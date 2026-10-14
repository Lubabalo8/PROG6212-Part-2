using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Users")]
public class User
{
    [Key, Column("User_ID")] public int UserId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Surname { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string Email { get; set; } = string.Empty;

    // Stores the HASH, never the plain password. Column is still named "Password" as in the ERD.
    [Required, Column("Password")] public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
    [Column("Created_At")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}