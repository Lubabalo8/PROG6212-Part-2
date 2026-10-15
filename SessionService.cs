using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public interface ISessionService
{
    int? GetUserId();
    UserRole? GetRole();
    void SignIn(int userId, UserRole role);
    void SignOut();
}

public class SessionService : ISessionService
{
    private readonly IHttpContextAccessor _accessor;

    public SessionService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public int? GetUserId() => Session.GetInt32("UserId");

    public UserRole? GetRole() =>
        Enum.TryParse<UserRole>(Session.GetString("Role"), out var role) ? role : null;

    public void SignIn(int userId, UserRole role)
    {
        Session.SetInt32("UserId", userId);
        Session.SetString("Role", role.ToString());
    }

    public void SignOut() => Session.Clear();
}