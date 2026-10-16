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

    /// <summary>Updates one of your own events (Organisers only).</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid event details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{eventId:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int eventId, UpdateEventDto dto)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change your own events." });

        if (dto.RegistrationDeadline.Date > dto.EventDate.Date)
            return BadRequest(new { message = "Registration deadline must be on or before the event date." });

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = AllowedStatuses.FirstOrDefault(s => s.Equals(dto.Status.Trim(), StringComparison.OrdinalIgnoreCase));
            if (status == null)
                return BadRequest(new { message = "Status must be Scheduled, Ongoing, Completed or Cancelled." });
            ev.Status = status;
        }

        ev.Title = dto.Title.Trim();
        ev.Description = dto.Description.Trim();
        ev.City = dto.City.Trim();
        ev.EventType = dto.EventType;
        ev.EventDate = dto.EventDate.Date;
        ev.RegistrationDeadline = dto.RegistrationDeadline.Date;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Deletes one of your own events (Organisers only).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event has enrolments, so it cannot be deleted. Cancel it instead.</response>
    [HttpDelete("{eventId:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });

        // Enrolments block deletion (database uses Restrict), so check first
        var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.Category!.EventId == eventId);
        if (hasEnrolments)
            return Conflict(new { message = "This event has enrolments. Set its status to Cancelled instead of deleting it." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}