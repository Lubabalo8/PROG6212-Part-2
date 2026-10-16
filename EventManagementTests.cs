using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Area 2: event management - Organisers can create, update and delete their own events.</summary>
public class EventManagementTests
{
    private static EventsController Make(AppDbContext db, User loggedInUser) =>
        new(db, SessionAs(loggedInUser));

    private static CreateEventDto NewEventDto() => new()
    {
        Title = "Soweto Run",
        Description = "10km fun run",
        City = "Johannesburg",
        EventType = EventType.Run,
        EventDate = DateTime.UtcNow.Date.AddDays(30),
        RegistrationDeadline = DateTime.UtcNow.Date.AddDays(15)
    };

    private static UpdateEventDto UpdateDto(string title, string? status = null) => new()
    {
        Title = title,
        Description = "Updated description",
        City = "Cape Town",
        EventType = EventType.Cycle,
        EventDate = DateTime.UtcNow.Date.AddDays(40),
        RegistrationDeadline = DateTime.UtcNow.Date.AddDays(20),
        Status = status
    };

    // ---------- Create ----------

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated_AndSavesWithOrganiserId()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);

        var result = await Make(db, organiser).CreateEvent(NewEventDto());

        Assert.Equal(201, Status(result));
        var saved = db.Events.Single();
        Assert.Equal(organiser.UserId, saved.OrganiserId);
        Assert.Equal("Scheduled", saved.Status);
        Assert.Equal(EventType.Run, saved.EventType);
    }

    [Fact]
    public async Task CreateEvent_DeadlineAfterEventDate_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var dto = NewEventDto();
        dto.RegistrationDeadline = dto.EventDate.AddDays(5);

        var result = await Make(db, organiser).CreateEvent(dto);

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task CreateEvent_DateInThePast_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var dto = NewEventDto();
        dto.EventDate = DateTime.UtcNow.Date.AddDays(-5);
        dto.RegistrationDeadline = DateTime.UtcNow.Date.AddDays(-10);

        var result = await Make(db, organiser).CreateEvent(dto);

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Events);
    }

    // ---------- View ----------

    [Fact]
    public async Task GetEvent_Existing_ReturnsOk()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, organiser.UserId);

        var result = await Make(db, organiser).GetEvent(ev.EventId);

        Assert.Equal(200, Status(result));
    }

    [Fact]
    public async Task GetEvent_Missing_ReturnsNotFound()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);

        var result = await Make(db, organiser).GetEvent(9999);

        Assert.Equal(404, Status(result));
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateEvent_OwnEvent_ReturnsOk_AndSavesChanges()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, organiser.UserId);

        var result = await Make(db, organiser).UpdateEvent(ev.EventId, UpdateDto("New Title", "Ongoing"));

        Assert.Equal(200, Status(result));
        var saved = db.Events.Single();
        Assert.Equal("New Title", saved.Title);
        Assert.Equal("Ongoing", saved.Status);
        Assert.Equal(EventType.Cycle, saved.EventType);
    }

    [Fact]
    public async Task UpdateEvent_AnotherOrganisersEvent_ReturnsForbidden_AndChangesNothing()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Organiser);
        var otherOrganiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, owner.UserId);

        var result = await Make(db, otherOrganiser).UpdateEvent(ev.EventId, UpdateDto("Hacked"));

        Assert.Equal(403, Status(result));
        Assert.Equal("Test Run", db.Events.Single().Title);
    }

    [Fact]
    public async Task UpdateEvent_InvalidStatus_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, organiser.UserId);

        var result = await Make(db, organiser).UpdateEvent(ev.EventId, UpdateDto("Title", "Finished"));

        Assert.Equal(400, Status(result));
    }

    [Fact]
    public async Task UpdateEvent_Missing_ReturnsNotFound()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);

        var result = await Make(db, organiser).UpdateEvent(9999, UpdateDto("Title"));

        Assert.Equal(404, Status(result));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteEvent_OwnEvent_ReturnsNoContent_AndRemovesIt()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, organiser.UserId);

        var result = await Make(db, organiser).DeleteEvent(ev.EventId);

        Assert.Equal(204, Status(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task DeleteEvent_AnotherOrganisersEvent_ReturnsForbidden_AndKeepsIt()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Organiser);
        var otherOrganiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, owner.UserId);

        var result = await Make(db, otherOrganiser).DeleteEvent(ev.EventId);

        Assert.Equal(403, Status(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_ReturnsConflict_AndKeepsIt()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId);
        var category = AddCategory(db, ev.EventId);
        AddEnrolment(db, participant.UserId, category.CategoryId, 1);

        var result = await Make(db, organiser).DeleteEvent(ev.EventId);

        Assert.Equal(409, Status(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task DeleteEvent_Missing_ReturnsNotFound()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);

        var result = await Make(db, organiser).DeleteEvent(9999);

        Assert.Equal(404, Status(result));
    }
}