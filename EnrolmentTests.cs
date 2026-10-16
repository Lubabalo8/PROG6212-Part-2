using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

/// <summary>Area 4: event enrolments - Participants can enrol and the enrolment is correctly recorded.</summary>
public class EnrolmentTests
{
    private static EnrolmentsController Make(AppDbContext db, User loggedInUser) =>
        new(db, SessionAs(loggedInUser));

    private static CreateEnrolmentDto For(Category category) => new() { CategoryId = category.CategoryId };

    // ---------- Participant enrols ----------

    [Fact]
    public async Task Enrol_ValidCategory_ReturnsCreated_AndRecordsParticipantEventCategoryLink()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId);
        var category = AddCategory(db, ev.EventId);

        var result = await Make(db, participant).Enrol(ev.EventId, For(category));

        Assert.Equal(201, Status(result));

        // The enrolment links participant -> category (-> event) in the database
        var saved = db.Enrolments.Single();
        Assert.Equal(participant.UserId, saved.UserId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal("Partaking", saved.Status);
        Assert.Equal(1, saved.BibNumber);

        // ...and the response describes the same link
        var body = Assert.IsType<EnrolmentDto>(Assert.IsType<ObjectResult>(result).Value);
        Assert.Equal(ev.EventId, body.EventId);
        Assert.Equal(category.CategoryName, body.CategoryName);
        Assert.Equal(participant.UserId, body.ParticipantId);
    }

    [Fact]
    public async Task Enrol_TwoParticipants_GetDifferentBibNumbers()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var first = AddUser(db, UserRole.Participant);
        var second = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId);
        var category = AddCategory(db, ev.EventId);

        await Make(db, first).Enrol(ev.EventId, For(category));
        await Make(db, second).Enrol(ev.EventId, For(category));

        var bibs = db.Enrolments.Select(e => e.BibNumber).ToList();
        Assert.Equal(2, bibs.Count);
        Assert.Equal(2, bibs.Distinct().Count());
    }

    [Fact]
    public async Task Enrol_SameEventTwice_ReturnsBadRequest_AndCreatesOnlyOne()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId);
        var category = AddCategory(db, ev.EventId);
        var controller = Make(db, participant);

        await controller.Enrol(ev.EventId, For(category));
        var second = await controller.Enrol(ev.EventId, For(category));

        Assert.Equal(400, Status(second));
        Assert.Single(db.Enrolments);
    }

    // ---------- Failure cases ----------

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var eventA = AddEvent(db, organiser.UserId);
        var eventB = AddEvent(db, organiser.UserId);
        var categoryOfB = AddCategory(db, eventB.EventId);

        var result = await Make(db, participant).Enrol(eventA.EventId, For(categoryOfB));

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_EventDoesNotExist_ReturnsNotFound()
    {
        using var db = NewDb();
        var participant = AddUser(db, UserRole.Participant);

        var result = await Make(db, participant).Enrol(9999, new CreateEnrolmentDto { CategoryId = 1 });

        Assert.Equal(404, Status(result));
    }

    [Fact]
    public async Task Enrol_FullCategory_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var alreadyIn = AddUser(db, UserRole.Participant);
        var latecomer = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId);
        var category = AddCategory(db, ev.EventId, maxParticipants: 1);
        AddEnrolment(db, alreadyIn.UserId, category.CategoryId, 1);

        var result = await Make(db, latecomer).Enrol(ev.EventId, For(category));

        Assert.Equal(400, Status(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_CancelledEvent_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId, status: "Cancelled");
        var category = AddCategory(db, ev.EventId);

        var result = await Make(db, participant).Enrol(ev.EventId, For(category));

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_AfterRegistrationDeadline_ReturnsBadRequest()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId, deadlineDays: -1);   // deadline was yesterday
        var category = AddCategory(db, ev.EventId);

        var result = await Make(db, participant).Enrol(ev.EventId, For(category));

        Assert.Equal(400, Status(result));
        Assert.Empty(db.Enrolments);
    }

    // ---------- Organiser views enrolments ----------

    [Fact]
    public async Task GetEnrolments_ByEventOwner_ReturnsOk_WithTheEnrolments()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);
        var participant = AddUser(db, UserRole.Participant);
        var ev = AddEvent(db, organiser.UserId);
        var category = AddCategory(db, ev.EventId);
        AddEnrolment(db, participant.UserId, category.CategoryId, 1);

        var result = await Make(db, organiser).GetEnrolments(ev.EventId);

        Assert.Equal(200, Status(result));
        var list = Assert.IsAssignableFrom<List<EnrolmentDto>>(Assert.IsType<OkObjectResult>(result).Value);
        var item = Assert.Single(list);
        Assert.Equal(participant.UserId, item.ParticipantId);
        Assert.Equal(category.CategoryId, item.CategoryId);
    }

    [Fact]
    public async Task GetEnrolments_ByDifferentOrganiser_ReturnsForbidden()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Organiser);
        var otherOrganiser = AddUser(db, UserRole.Organiser);
        var ev = AddEvent(db, owner.UserId);

        var result = await Make(db, otherOrganiser).GetEnrolments(ev.EventId);

        Assert.Equal(403, Status(result));
    }

    [Fact]
    public async Task GetEnrolments_EventDoesNotExist_ReturnsNotFound()
    {
        using var db = NewDb();
        var organiser = AddUser(db, UserRole.Organiser);

        var result = await Make(db, organiser).GetEnrolments(9999);

        Assert.Equal(404, Status(result));
    }
}