using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PushController : ApiControllerBase
{
    private readonly IPushService _push;
    private readonly IWebPushService _webPush;

    public PushController(IPushService push, IWebPushService webPush, IAuthService auth) : base(auth)
    {
        _push = push;
        _webPush = webPush;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;

        var platform = string.IsNullOrWhiteSpace(request.Platform)
            ? "unknown"
            : request.Platform.ToLowerInvariant();
        await _push.RegisterDeviceAsync(userId, request.Token, platform);
        return Ok();
    }

    [HttpPost("unregister")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Unregister([FromBody] RegisterDeviceRequest request)
    {
        await _push.UnregisterDeviceAsync(request.Token);
        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("vapid-public-key")]
    public ActionResult<object> VapidPublicKey() => Ok(new { publicKey = _webPush.PublicKey });

    [HttpPost("web/subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WebSubscribe([FromBody] WebPushSubscriptionDto sub)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        await _webPush.SaveSubscriptionAsync(userId, sub);
        return Ok();
    }

    [HttpPost("web/unsubscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WebUnsubscribe([FromBody] WebPushSubscriptionDto sub)
    {
        await _webPush.RemoveSubscriptionAsync(sub.Endpoint);
        return Ok();
    }
}
