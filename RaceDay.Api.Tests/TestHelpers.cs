using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public static class TestHelpers
{
    /// <summary>A brand-new in-memory database for each test, so tests never affect each other.</summary>
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>A fake session where this user is logged in.</summary>
    public static FakeSessionService SessionAs(User user) => new(user);

    /// <summary>A fake session where nobody is logged in.</summary>
    public static FakeSessionService NotLoggedIn() => new();
    public static IConfiguration Config(string organiserCode = "TEST-CODE") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:OrganiserCode"] = organiserCode
            })
            .Build();

    /// <summary>Returns the HTTP status code of a controller result (200, 201, 400, ...).</summary>
    public static int? Status(IActionResult? result) => (result as IStatusCodeActionResult)?.StatusCode;

    // ---------- Seed helpers ----------

    public static User AddUser(AppDbContext db, UserRole role, string? email = null)
    {
        var user = new User
        {
            Name = "Test",
            Surname = role.ToString(),
            Email = email ?? $"{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            Role = role
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(AppDbContext db, int organiserId, string status = "Scheduled", int deadlineDays = 10)
    {
        var ev = new Event
        {
            OrganiserId = organiserId,
            Title = "Test Run",
            Description = "Test event",
            City = "Johannesburg",
            EventType = EventType.Run,
            EventDate = DateTime.UtcNow.Date.AddDays(30),
            RegistrationDeadline = DateTime.UtcNow.Date.AddDays(deadlineDays),
            Status = status
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(AppDbContext db, int eventId, int maxParticipants = 10, string name = "10km")
    {
        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            DistanceKm = 10,
            EntryFee = 100,
            MaxParticipants = maxParticipants,
            StartTime = new TimeSpan(8, 0, 0)
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(AppDbContext db, int userId, int categoryId, int bib)
    {
        var enrolment = new Enrolment
        {
            UserId = userId,
            CategoryId = categoryId,
            EnrolmentDate = DateTime.UtcNow.Date,
            BibNumber = bib,
            Status = "Partaking"
        };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }

    /// <summary>A simple stand-in for the real session, which records what the controller did.</summary>
    public class FakeSessionService : ISessionService
    {
        public int? UserId { get; private set; }
        public UserRole? Role { get; private set; }
        public int SignInCount { get; private set; }
        public int SignOutCount { get; private set; }

        public FakeSessionService() { }

        public FakeSessionService(User user)
        {
            UserId = user.UserId;
            Role = user.Role;
        }

        public int? GetUserId() => UserId;
        public UserRole? GetRole() => Role;

        public void SignIn(int userId, UserRole role)
        {
            UserId = userId;
            Role = role;
            SignInCount++;
        }

        public void SignOut()
        {
            UserId = null;
            Role = null;
            SignOutCount++;
        }
    }
}