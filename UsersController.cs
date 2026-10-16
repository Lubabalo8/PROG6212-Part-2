using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuthorize]   // every endpoint here needs a logged-in user (either role)
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISessionService _session;
    private readonly IPasswordService _passwords;

    public UsersController(AppDbContext db, ISessionService session, IPasswordService passwords)
    {
        _db = db;
        _session = session;
        _passwords = passwords;
    }

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">User no longer exists.</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _session.GetUserId()!.Value;
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        return Ok(new ProfileDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>Updates the logged-in user's own profile (and optionally their password).</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Invalid information, email already used, or wrong current password.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = _session.GetUserId()!.Value;
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var email = dto.Email.Trim().ToLowerInvariant();

        // Email must not belong to a different user
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            return BadRequest(new { message = "Email is already in use." });

        // Optional password change
        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword) || !_passwords.Verify(user, dto.CurrentPassword))
                return BadRequest(new { message = "Current password is incorrect." });

            user.PasswordHash = _passwords.Hash(user, dto.NewPassword);
        }

        user.Name = dto.Name.Trim();
        user.Surname = dto.Surname.Trim();
        user.Email = email;

        await _db.SaveChangesAsync();

        return Ok(new ProfileDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        });
    }
}