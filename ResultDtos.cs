using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateResultDto
{
    /// <summary>The user ID of the participant (must be enrolled in this event).</summary>
    [Range(1, int.MaxValue)]
    public int ParticipantId { get; set; }

    /// <summary>Finish time in the format HH:mm:ss, e.g. 01:45:30.</summary>
    public TimeSpan FinishTime { get; set; }

    /// <summary>Finishing position overall. Use 0 for "Not finished".</summary>
    [Range(0, 1000000)]
    public int PositionOverall { get; set; }

    /// <summary>Finishing position within the participant's category (optional).</summary>
    [Range(1, 1000000)]
    public int? PositionCategory { get; set; }

    /// <summary>Finished or Not finished.</summary>
    public string Status { get; set; } = "Finished";
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int BibNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int PositionOverall { get; set; }
    public int? PositionCategory { get; set; }
    public string Status { get; set; } = string.Empty;
}