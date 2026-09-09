using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>Shared helpers for authenticated API controllers (Template Method style).</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected IAuthService Auth { get; }

    protected ApiControllerBase(IAuthService auth) => Auth = auth;

    protected Guid? CurrentUserId => Auth.GetUserIdFromPrincipal(User);

    /// <summary>Resolves current user id or sets <paramref name="unauthorized"/> to Unauthorized.</summary>
    protected bool TryGetUserId(out Guid userId, out ActionResult? unauthorized)
    {
        var id = CurrentUserId;
        if (id is null)
        {
            userId = default;
            unauthorized = Unauthorized();
            return false;
        }

        userId = id.Value;
        unauthorized = null;
        return true;
    }
}
