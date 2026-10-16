using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Xunit;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Area 1: user registration and login (success and failure).</summary>
public class AuthControllerTests
{
    private static AuthController Make(AppDbContext db, FakeSessionService session) =>
        new(db, new PasswordService(), session, Config());

    private static RegisterDto Registration(string email, UserRole role, string? code = null) => new()
    {
        Name = "Pat",
        Surname = "Tester",
        Email = email,
        Password = "Password123",
        Role = role,
        OrganiserCode = code
    };

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Participant_ReturnsCreated_AndStoresHashedPassword()
    {
        using var db = NewDb();
        var controller = Make(db, NotLoggedIn());

        var result = await controller.Register(Registration("Pat@Test.com", UserRole.Participant));

        Assert.Equal(201, Status(result));
        var saved = db.Users.Single();
        Assert.Equal("pat@test.com", saved.Email);                  // email is normalised
        Assert.NotEqual("Password123", saved.PasswordHash);         // never stored in plain text
        Assert.True(new PasswordService().Verify(saved, "Password123"));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        using var db = NewDb();
        AddUser(db, UserRole.Participant, "pat@test.com");
        var controller = Make(db, NotLoggedIn());

        var result = await controller.Register(Registration("pat@test.com", UserRole.Participant));

        Assert.Equal(400, Status(result));
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_ReturnsBadRequest()
    {
        using var db = NewDb();
        var controller = Make(db, NotLoggedIn());

        var result = await controller.Register(Registration("olga@test.com", UserRole.Organiser));

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongCode_ReturnsBadRequest()
    {
        using var db = NewDb();
        var controller = Make(db, NotLoggedIn());

        var result = await controller.Register(Registration("olga@test.com", UserRole.Organiser, "WRONG"));

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_Organiser_WithCorrectCode_ReturnsCreated()
    {
        using var db = NewDb();
        var controller = Make(db, NotLoggedIn());

        var result = await controller.Register(Registration("olga@test.com", UserRole.Organiser, "TEST-CODE"));

        Assert.Equal(201, Status(result));
        Assert.Equal(UserRole.Organiser, db.Users.Single().Role);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk_AndStartsSession()
    {
        using var db = NewDb();
        var session = NotLoggedIn();
        var controller = Make(db, session);
        await controller.Register(Registration("pat@test.com", UserRole.Participant));
        var user = db.Users.Single();

        var result = await controller.Login(new LoginDto { Email = "pat@test.com", Password = "Password123" });

        Assert.Equal(200, Status(result));
        Assert.Equal(1, session.SignInCount);
        Assert.Equal(user.UserId, session.UserId);
        Assert.Equal(UserRole.Participant, session.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized_AndNoSession()
    {
        using var db = NewDb();
        var session = NotLoggedIn();
        var controller = Make(db, session);
        await controller.Register(Registration("pat@test.com", UserRole.Participant));

        var result = await controller.Login(new LoginDto { Email = "pat@test.com", Password = "WrongPassword" });

        Assert.Equal(401, Status(result));
        Assert.Equal(0, session.SignInCount);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        using var db = NewDb();
        var controller = Make(db, NotLoggedIn());

        var result = await controller.Login(new LoginDto { Email = "nobody@test.com", Password = "Password123" });

        Assert.Equal(401, Status(result));
    }

    // ---------- Logout ----------

    [Fact]
    public void Logout_ClearsSession_AndReturnsOk()
    {
        using var db = NewDb();
        var user = AddUser(db, UserRole.Participant);
        var session = SessionAs(user);
        var controller = Make(db, session);

        var result = controller.Logout();

        Assert.Equal(200, Status(result));
        Assert.Equal(1, session.SignOutCount);
    }
}