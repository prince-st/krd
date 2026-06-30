using System.ComponentModel.DataAnnotations;

namespace KRD.AttendanceWeb.Models;

public class Holiday
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsRecurringYearly { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

