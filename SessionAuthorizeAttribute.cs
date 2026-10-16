using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Filters;

/// <summary>
/// [SessionAuthorize]                      = any logged-in user
/// [SessionAuthorize(UserRole.Organiser)]  = Organisers only
/// [SessionAuthorize(UserRole.Participant)] = Participants only
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole? _role;

    public SessionAuthorizeAttribute() { }

    public SessionAuthorizeAttribute(UserRole role)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();

        // Not logged in -> 401
        if (session.GetUserId() == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in." });
            return;
        }

        // Logged in but wrong role -> 403
        if (_role != null && session.GetRole() != _role)
        {
            context.Result = new ObjectResult(new { message = "Insufficient role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}