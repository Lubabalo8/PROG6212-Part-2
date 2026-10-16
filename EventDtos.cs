using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class CreateEventDto
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Location of the event.</summary>
    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>Run, Walk or Cycle.</summary>
    [Required]
    public EventType EventType { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public DateTime RegistrationDeadline { get; set; }
}

public class UpdateEventDto : CreateEventDto
{
    /// <summary>Scheduled, Ongoing, Completed or Cancelled. Leave empty to keep the current status.</summary>
    public string? Status { get; set; }
}

public class CategorySummaryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int DistanceKm { get; set; }
    public int EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public TimeSpan StartTime { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Distances are stored per category, so they are listed here.</summary>
    public List<CategorySummaryDto> Categories { get; set; } = new();
}