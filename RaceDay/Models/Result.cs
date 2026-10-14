using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Results")]
public class Result
{
    [Key, Column("Result_ID")] public int ResultId { get; set; }
    [Column("Enrollment_ID")] public int EnrolmentId { get; set; }
    [Column("Finish_Time_Seconds")] public int FinishTimeSeconds { get; set; }
    [Column("Position_Overall")] public int PositionOverall { get; set; }
    [Column("Position_Category")] public int PositionCategory { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Finished";

    public Enrolment? Enrolment { get; set; }
}