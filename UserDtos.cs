using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ProfileDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Surname { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Only fill in these two if you want to change your password.</summary>
    public string? CurrentPassword { get; set; }

    [MinLength(8)]
    public string? NewPassword { get; set; }
}