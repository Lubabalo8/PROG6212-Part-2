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
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISessionService _session;

    public ResultsController(AppDbContext db, ISessionService session)
    {
        _db = db;
        _session = session;
    }

    private static readonly Expression<Func<Result, ResultDto>> ToDto = r => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment!.Category!.EventId,
        EventTitle = r.Enrolment.Category.Event!.Title,
        ParticipantId = r.Enrolment.UserId,
        ParticipantName = r.Enrolment.User!.Name + " " + r.Enrolment.User.Surname,
        BibNumber = r.Enrolment.BibNumber,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        PositionOverall = r.PositionOverall,
        PositionCategory = r.PositionCategory,
        Status = r.Status
    };

    /// <summary>Records a participant's finish time and position for one of your own events (Organisers only).</summary>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid result, participant not partaking, or result/position already recorded.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Event not found, or the participant is not enrolled in it.</response>
    [HttpPost("api/events/{eventId:int}/results")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordResult(int eventId, CreateResultDto dto)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only record results for your own events." });

        if (ev.Status == "Cancelled")
            return BadRequest(new { message = "Cannot record results for a cancelled event." });

        var status = dto.Status.Trim().Equals("Finished", StringComparison.OrdinalIgnoreCase) ? "Finished"
                   : dto.Status.Trim().Equals("Not finished", StringComparison.OrdinalIgnoreCase) ? "Not finished"
                   : null;
        if (status == null)
            return BadRequest(new { message = "Status must be 'Finished' or 'Not finished'." });

        if (status == "Finished" && (dto.FinishTime <= TimeSpan.Zero || dto.PositionOverall < 1))
            return BadRequest(new { message = "A finished result needs a finish time and a position of 1 or more." });

        var enrolment = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.UserId == dto.ParticipantId && e.Category!.EventId == eventId);
        if (enrolment == null)
            return NotFound(new { message = "That participant is not enrolled in this event." });

        if (enrolment.Status != "Partaking")
            return BadRequest(new { message = "That participant is not partaking in this event." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolment.EnrolmentId))
            return BadRequest(new { message = "A result is already recorded for this participant." });

        if (status == "Finished" && await _db.Results.AnyAsync(r =>
                r.Enrolment!.Category!.EventId == eventId &&
                r.Status == "Finished" &&
                r.PositionOverall == dto.PositionOverall))
            return BadRequest(new { message = "Another participant already has that overall position." });

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = dto.FinishTime,
            PositionOverall = status == "Finished" ? dto.PositionOverall : 0,
            PositionCategory = dto.PositionCategory,
            Status = status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var created = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == result.ResultId)
            .Select(ToDto)
            .FirstAsync();

        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Lists all results for one of your own events (Organisers only).</summary>
    /// <response code="200">Results returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view results for your own events." });

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.Status == "Finished" ? 0 : 1)
            .ThenBy(r => r.PositionOverall)
            .Select(ToDto)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Returns the logged-in Participant's own results.</summary>
    /// <response code="200">Results returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role (Participants only).</response>
    [HttpGet("api/results/my")]
    [SessionAuthorize(UserRole.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = _session.GetUserId()!.Value;

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment!.UserId == userId)
            .OrderBy(r => r.ResultId)
            .Select(ToDto)
            .ToListAsync();

        return Ok(list);
    }
}