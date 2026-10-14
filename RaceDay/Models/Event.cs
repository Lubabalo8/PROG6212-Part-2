using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Events")]
public class Event
{
    [Key, Column("Event_ID")] public int EventId { get; set; }
    [Column("Organiser_ID")] public int OrganiserId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
    [Column("Event_Type")] public EventType EventType { get; set; }
    [Column("Event_Date")] public DateTime EventDate { get; set; }
    [Column("Registration_Deadline")] public DateTime RegistrationDeadline { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Open";

    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
}