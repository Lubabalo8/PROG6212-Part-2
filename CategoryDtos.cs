using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryRequestDto
{
    /// <summary>For example: Under 20, Senior, 10km, 21km.</summary>
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int DistanceKm { get; set; }

    [Range(0, 100000)]
    public int EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    /// <summary>Time of day in the format HH:mm:ss, e.g. 08:00:00.</summary>
    public TimeSpan StartTime { get; set; }
}