using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KRD.AttendanceWeb.Models;

public enum AttendanceStatus
{
    Present,
    Absent,
    Holiday,
    Leave,
    HalfDay,
    Late,
    EarlyExit
}

public class Attendance
{
    [Key]
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public double WorkingHours { get; set; }

    public double RequiredHours { get; set; }

    public double OvertimeHours { get; set; }

    public double ShortHours { get; set; }

    public bool IsLate { get; set; }

    public bool IsEarlyExit { get; set; }

    public int LateMinutes { get; set; }

    public int EarlyExitMinutes { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }
}

