using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Categories")]
public class Category
{
    [Key, Column("Category_ID")] public int CategoryId { get; set; }
    [Column("Event_ID")] public int EventId { get; set; }
    [Required, MaxLength(100), Column("Category_Name")] public string CategoryName { get; set; } = string.Empty;
    [Column("Distance_km", TypeName = "decimal(6,2)")] public decimal DistanceKm { get; set; }
    [Column("Entry_Fee", TypeName = "decimal(10,2)")] public decimal EntryFee { get; set; }
    [Column("Max_Participants")] public int MaxParticipants { get; set; }
    [Column("Start_Time")] public TimeSpan StartTime { get; set; }

    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}