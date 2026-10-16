using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateEnrolmentDto
{
    /// <summary>The category (of this event) the participant wants to enter.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public int BibNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}