using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KRD.AttendanceWeb.Models;

public class Employee
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    [MaxLength(100)]
    public string Position { get; set; } = string.Empty;

    public DateTime JoinDate { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    // ── Custom Shift (null = use company default) ──────────────────────
    /// <summary>Stored as ticks; null means "use company default"</summary>
    public long? ShiftStartTicks { get; set; }
    public long? ShiftEndTicks   { get; set; }
    public double? ShiftRequiredHours { get; set; }

    [MaxLength(100)]
    public string ShiftName { get; set; } = string.Empty;  // e.g. "Morning", "Night"

    [NotMapped]
    public TimeSpan? ShiftStart
    {
        get => ShiftStartTicks.HasValue ? TimeSpan.FromTicks(ShiftStartTicks.Value) : null;
        set => ShiftStartTicks = value?.Ticks;
    }
    [NotMapped]
    public TimeSpan? ShiftEnd
    {
        get => ShiftEndTicks.HasValue ? TimeSpan.FromTicks(ShiftEndTicks.Value) : null;
        set => ShiftEndTicks = value?.Ticks;
    }

    public bool HasCustomShift => ShiftStartTicks.HasValue && ShiftEndTicks.HasValue;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLogin { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Leave> Leaves { get; set; } = new List<Leave>();
}

