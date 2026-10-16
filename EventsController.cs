using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] AllowedStatuses = { "Scheduled", "Ongoing", "Completed", "Cancelled" };

    private readonly AppDbContext _db;
    private readonly ISessionService _session;

    public EventsController(AppDbContext db, ISessionService session)
    {
        _db = db;
        _session = session;
    }

    private static EventDto ToDto(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        Title = e.Title,
        Description = e.Description,
        City = e.City,
        EventType = e.EventType.ToString(),
        EventDate = e.EventDate,
        RegistrationDeadline = e.RegistrationDeadline,
        Status = e.Status,
        Categories = e.Categories.Select(c => new CategorySummaryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants,
            StartTime = c.StartTime
        }).ToList()
    };

    /// <summary>Creates a new event (Organisers only).</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid event details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpPost]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto)
    {
        if (dto.RegistrationDeadline.Date > dto.EventDate.Date)
            return BadRequest(new { message = "Registration deadline must be on or before the event date." });

        if (dto.EventDate.Date < DateTime.UtcNow.Date)
            return BadRequest(new { message = "Event date cannot be in the past." });

        var ev = new Event
        {
            OrganiserId = _session.GetUserId()!.Value,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            City = dto.City.Trim(),
            EventType = dto.EventType,
            EventDate = dto.EventDate.Date,
            RegistrationDeadline = dto.RegistrationDeadline.Date,
            Status = "Scheduled"
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { eventId = ev.EventId }, ToDto(ev));
    }

    /// <summary>Returns all events (any logged-in user).</summary>
    /// <response code="200">Events returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [SessionAuthorize]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToDto).ToList());
    }

    /// <summary>Returns one event with its categories (any logged-in user).</summary>
    /// <response code="200">Event returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{eventId:int}")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int eventId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (ev == null) return NotFound(new { message = "Event not found." });

        return Ok(ToDto(ev));
    }

    // PUT and DELETE are added in Part C
}