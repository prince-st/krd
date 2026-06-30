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

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLogin { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Leave> Leaves { get; set; } = new List<Leave>();
}

