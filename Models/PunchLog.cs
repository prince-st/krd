using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KRD.AttendanceWeb.Models;

/// <summary>
/// Every individual check-in / check-out tap.
/// One employee can have many punches per day.
/// </summary>
public class PunchLog
{
    [Key]
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>The parent daily Attendance record</summary>
    public int AttendanceId { get; set; }

    public DateTime PunchIn  { get; set; }

    public DateTime? PunchOut { get; set; }

    /// <summary>Hours for this single session (filled on PunchOut)</summary>
    public double SessionHours { get; set; }

    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }

    [ForeignKey("AttendanceId")]
    public virtual Attendance? Attendance { get; set; }
}
