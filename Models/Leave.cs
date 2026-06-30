using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KRD.AttendanceWeb.Models;

public enum LeaveType
{
    Annual,
    Sick,
    Casual,
    Maternity,
    Paternity,
    Emergency,
    Unpaid
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public class Leave
{
    [Key]
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public LeaveType LeaveType { get; set; } = LeaveType.Casual;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalDays { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    [MaxLength(500)]
    public string AdminRemarks { get; set; } = string.Empty;

    public DateTime AppliedDate { get; set; } = DateTime.Now;

    public DateTime? ReviewedDate { get; set; }

    public int? ReviewedByAdminId { get; set; }

    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }
}

