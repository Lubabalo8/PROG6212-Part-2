using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Routes")]
public class RaceRoute
{
    [Key, Column("Route_ID")] public int RouteId { get; set; }
    [Column("Event_ID")] public int EventId { get; set; }
    [Column("Elevation_Gain", TypeName = "decimal(8,2)")] public decimal ElevationGain { get; set; }

    public Event? Event { get; set; }
}