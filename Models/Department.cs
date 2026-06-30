using System.ComponentModel.DataAnnotations;

namespace KRD.AttendanceWeb.Models;

public class Department
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

