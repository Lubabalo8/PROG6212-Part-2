using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Controllers;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Xunit;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>
/// Area 3: different roles are rejected when attempting role-based functionality.
/// Part 1 tests the filter itself; part 2 checks each endpoint is protected with the correct role.
/// </summary>
public class RoleAccessTests
{
    private static AuthorizationFilterContext MakeContext(ISessionService session)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISessionService>(session);

        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    // ---------- Part 1: the filter ----------

    [Fact]
    public void NotLoggedIn_IsRejectedWith401()
    {
        var ctx = MakeContext(NotLoggedIn());

        new SessionAuthorizeAttribute(UserRole.Organiser).OnAuthorization(ctx);

        Assert.Equal(401, Status(ctx.Result));
    }

    [Fact]
    public void NotLoggedIn_IsRejectedWith401_EvenWhenAnyRoleIsAllowed()
    {
        var ctx = MakeContext(NotLoggedIn());

        new SessionAuthorizeAttribute().OnAuthorization(ctx);

        Assert.Equal(401, Status(ctx.Result));
    }

    [Fact]
    public void Participant_OnOrganiserEndpoint_IsRejectedWith403()
    {
        using var db = NewDb();
        var participant = AddUser(db, UserRole.Participant);
        var ctx = MakeContext(SessionAs(participant));

        new SessionAuthorizeAttribute(UserRole.Organiser).OnAuthorization(ctx);

        Assert.Equal(403, Status(ctx.Result));
    }

    [Fact]
    public void Organiser_OnParticipantEndpoint_IsRejectedWith403()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var ctx = MakeContext(SessionAs(organiser));

        new SessionAuthorizeAttribute(UserRole.Participant).OnAuthorization(ctx);

        Assert.Equal(403, Status(ctx.Result));
    }

    [Fact]
    public void Organiser_OnOrganiserEndpoint_IsAllowed()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var ctx = MakeContext(SessionAs(organiser));

        new SessionAuthorizeAttribute(UserRole.Organiser).OnAuthorization(ctx);

        Assert.Null(ctx.Result);   // no result set = the request continues to the endpoint
    }

    [Fact]
    public void Participant_OnParticipantEndpoint_IsAllowed()
    {
        using var db = NewDb();
        var participant = AddUser(db, UserRole.Participant);
        var ctx = MakeContext(SessionAs(participant));

        new SessionAuthorizeAttribute(UserRole.Participant).OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }

    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public void AnyLoggedInUser_IsAllowed_OnOpenEndpoint(UserRole role)
    {
        using var db = NewDb();
        var user = AddUser(db, role);
        var ctx = MakeContext(SessionAs(user));

        new SessionAuthorizeAttribute().OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }

    // ---------- Part 2: every endpoint carries the right protection ----------
    // expectedRole = null means "any logged-in user"

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Logout), null)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetProfile), null)]
    [InlineData(typeof(UsersController), nameof(UsersController.UpdateProfile), null)]
    [InlineData(typeof(EventsController), nameof(EventsController.CreateEvent), UserRole.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.GetEvents), null)]
    [InlineData(typeof(EventsController), nameof(EventsController.GetEvent), null)]
    [InlineData(typeof(EventsController), nameof(EventsController.UpdateEvent), UserRole.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.DeleteEvent), UserRole.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.GetCategories), null)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.CreateCategory), UserRole.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.UpdateCategory), UserRole.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.DeleteCategory), UserRole.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), UserRole.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetEnrolments), UserRole.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.RecordResult), UserRole.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetEventResults), UserRole.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMyResults), UserRole.Participant)]
    public void Endpoint_IsProtectedWithExpectedRole(Type controller, string methodName, UserRole? expectedRole)
    {
        var method = controller.GetMethod(methodName);
        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<SessionAuthorizeAttribute>()
                        ?? controller.GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRole, attribute!.Role);
    }

    [Theory]
    [InlineData(nameof(AuthController.Register))]
    [InlineData(nameof(AuthController.Login))]
    public void PublicAuthEndpoints_DoNotRequireLogin(string methodName)
    {
        var method = typeof(AuthController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Null(method!.GetCustomAttribute<SessionAuthorizeAttribute>());
    }
}