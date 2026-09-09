using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>
/// Push-уведомления: нативные устройства (FCM/APNs token) и Web Push (VAPID) для PWA.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PushController : ControllerBase
{
    private readonly PushService _push;
    private readonly WebPushService _webPush;
    private readonly AuthService _auth;

    public PushController(PushService push, WebPushService webPush, AuthService auth)
    {
        _push = push;
        _webPush = webPush;
        _auth = auth;
    }

    /// <summary>Зарегистрировать устройство для push (iOS/Android).</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request)
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();

        var platform = string.IsNullOrWhiteSpace(request.Platform)
            ? "unknown"
            : request.Platform.ToLowerInvariant();
        await _push.RegisterDeviceAsync(userId.Value, request.Token, platform);
        return Ok();
    }

    /// <summary>Отменить регистрацию device token.</summary>
    [HttpPost("unregister")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Unregister([FromBody] RegisterDeviceRequest request)
    {
        await _push.UnregisterDeviceAsync(request.Token);
        return Ok();
    }

    /// <summary>Публичный VAPID-ключ для Web Push (PWA / iOS home screen).</summary>
    [AllowAnonymous]
    [HttpGet("vapid-public-key")]
    public ActionResult<object> VapidPublicKey() => Ok(new { publicKey = _webPush.PublicKey });

    /// <summary>Подписать браузер/PWA на Web Push.</summary>
    [HttpPost("web/subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WebSubscribe([FromBody] WebPushSubscriptionDto sub)
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();
        await _webPush.SaveSubscriptionAsync(userId.Value, sub);
        return Ok();
    }

    /// <summary>Отписаться от Web Push.</summary>
    [HttpPost("web/unsubscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WebUnsubscribe([FromBody] WebPushSubscriptionDto sub)
    {
        await _webPush.RemoveSubscriptionAsync(sub.Endpoint);
        return Ok();
    }
}
