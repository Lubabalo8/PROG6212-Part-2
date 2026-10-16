using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ISessionService _session;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IPasswordService passwords,
                          ISessionService session, IConfiguration config)
    {
        _db = db;
        _passwords = passwords;
        _session = session;
        _config = config;
    }

    /// <summary>Registers a new Organiser or Participant.</summary>
    /// <response code="201">Registration successful.</response>
    /// <response code="400">Invalid details, duplicate email, or wrong organiser code.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { message = "Email is already registered." });

        // Only people with the secret code can become an Organiser
        var requiredCode = _config["Registration:OrganiserCode"];
        if (dto.Role == UserRole.Organiser &&
            (string.IsNullOrEmpty(requiredCode) || dto.OrganiserCode != requiredCode))
            return BadRequest(new { message = "Invalid organiser code." });

        var user = new User
        {
            Name = dto.Name.Trim(),
            Surname = dto.Surname.Trim(),
            Email = email,
            Role = dto.Role
        };
        user.PasswordHash = _passwords.Hash(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            user.UserId,
            user.Name,
            user.Surname,
            user.Email,
            Role = user.Role.ToString()
        });
    }

    /// <summary>Logs in and starts a session that remembers the user's role.</summary>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !_passwords.Verify(user, dto.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        _session.SignIn(user.UserId, user.Role);

        return Ok(new { message = "Login successful.", user.UserId, Role = user.Role.ToString() });
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="200">Logged out.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        _session.SignOut();
        return Ok(new { message = "Logged out." });
    }
}