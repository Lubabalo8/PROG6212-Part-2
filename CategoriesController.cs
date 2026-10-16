using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISessionService _session;

    public CategoriesController(AppDbContext db, ISessionService session)
    {
        _db = db;
        _session = session;
    }

    private static CategorySummaryDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants,
        StartTime = c.StartTime
    };

    /// <summary>Lists the categories of an event (any logged-in user).</summary>
    /// <response code="200">Categories returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(List<CategorySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories.Select(ToDto).ToList());
    }

    /// <summary>Creates a category for one of your own events (Organisers only).</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid category, or the name already exists for this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(CategorySummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequestDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });

        var name = dto.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name))
            return BadRequest(new { message = "This event already has a category with that name." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants,
            StartTime = dto.StartTime
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(category));
    }

    /// <summary>Updates a category of one of your own events (Organisers only).</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Invalid category, duplicate name, or capacity below current enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("api/categories/{categoryId:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(typeof(CategorySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int categoryId, CategoryRequestDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change categories of your own events." });

        var name = dto.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId
                                               && c.CategoryName == name
                                               && c.CategoryId != categoryId))
            return BadRequest(new { message = "This event already has a category with that name." });

        var enrolled = await _db.Enrolments.CountAsync(e => e.CategoryId == categoryId);
        if (dto.MaxParticipants < enrolled)
            return BadRequest(new { message = $"{enrolled} participants are already enrolled, so the maximum cannot be lower." });

        category.CategoryName = name;
        category.DistanceKm = dto.DistanceKm;
        category.EntryFee = dto.EntryFee;
        category.MaxParticipants = dto.MaxParticipants;
        category.StartTime = dto.StartTime;

        await _db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>Deletes a category of one of your own events (Organisers only).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Insufficient role, or not the organiser of this event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has enrolments, so it cannot be deleted.</response>
    [HttpDelete("api/categories/{categoryId:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != _session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of your own events." });

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == categoryId))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}