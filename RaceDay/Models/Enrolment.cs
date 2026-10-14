using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Enrolments")]
public class Enrolment
{
    [Key, Column("Enrollment_ID")] public int EnrolmentId { get; set; }
    [Column("User_ID")] public int UserId { get; set; }
    [Column("Category_ID")] public int CategoryId { get; set; }
    [Column("Enrollment_Date")] public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    [Column("Bib_Number")] public int BibNumber { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Confirmed";

    public User? User { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}