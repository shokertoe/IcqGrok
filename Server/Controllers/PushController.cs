using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ICQ.Server.Models;
using ICQ.Server.Services;

namespace ICQ.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly PushService _pushService;
    private readonly WebPushService _webPush;

    public PushController(PushService pushService, WebPushService webPush)
    {
        _pushService = pushService;
        _webPush = webPush;
    }

    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
    {
        var key = _webPush.GetPublicKey();
        if (string.IsNullOrEmpty(key)) return NotFound(new { error = "VAPID not configured" });
        return Ok(new { publicKey = key });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] WebPushSubscribeRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _pushService.SubscribeAsync(userId.Value, req);
        return Ok(new { ok = true });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] WebPushSubscribeRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _pushService.UnsubscribeAsync(userId.Value, req.Endpoint);
        return Ok(new { ok = true });
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(claim, out var id) ? id : null;
    }
}
