using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISessionService _session;

    public EnrolmentsController(AppDbContext db, ISessionService session)
    {
        _db = db;
        _session = session;
    }

    private static readonly Expression<Func<Enrolment, EnrolmentDto>> ToDto = e => new EnrolmentDto
    {
        EnrolmentId = e.EnrolmentId,
        EventId = e.Category!.EventId,
        EventTitle = e.Category.Event!.Title,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        ParticipantId = e.UserId,
        ParticipantName = e.User!.Name + " " + e.User.Surname,
        EnrolmentDate = e.EnrolmentDate,
        BibNumber = e.BibNumber,
        Status = e.Status
    };

    /// <summary>Enrols the logged-in Participant in an event by selecting a category.</summary>
    /// <response code="201">Enrolment created. A unique bib number is assigned.</response>
    /// <response code="400">Invalid category, event closed, deadline passed, already enrolled, or category full.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role (Participants only).</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Could not allocate a bib number. Try again.</response>
    [HttpPost]
    [SessionAuthorize(UserRole.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, CreateEnrolmentDto dto)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);
        if (category == null)
            return BadRequest(new { message = "Invalid category for this event." });

        if (ev.Status != "Scheduled")
            return BadRequest(new { message = "This event is not open for enrolment." });

        if (DateTime.UtcNow.Date > ev.RegistrationDeadline.Date)
            return BadRequest(new { message = "The registration deadline has passed." });

        var userId = _session.GetUserId()!.Value;

        if (await _db.Enrolments.AnyAsync(e => e.UserId == userId && e.Category!.EventId == eventId))
            return BadRequest(new { message = "You are already enrolled in this event." });

        var taken = await _db.Enrolments
            .CountAsync(e => e.CategoryId == category.CategoryId && e.Status == "Partaking");
        if (taken >= category.MaxParticipants)
            return BadRequest(new { message = "This category is full." });

        // Bib numbers must be unique across all enrolments
        var nextBib = (await _db.Enrolments.MaxAsync(e => (int?)e.BibNumber) ?? 0) + 1;

        var enrolment = new Enrolment
        {
            UserId = userId,
            CategoryId = category.CategoryId,
            EnrolmentDate = DateTime.UtcNow.Date,
            BibNumber = nextBib,
            Status = "Partaking"
        };

        _db.Enrolments.Add(enrolment);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Could not complete the enrolment. Please try again." });
        }

        var created = await _db.Enrolments.AsNoTracking()
            .Where(e => e.EnrolmentId == enrolment.EnrolmentId)
            .Select(ToDto)
            .FirstAsync();

        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Lists all enrolments for one of your own events (Organisers only).</summary>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolments(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });

        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.BibNumber)
            .Select(ToDto)
            .ToListAsync();

        return Ok(list);
    }
}