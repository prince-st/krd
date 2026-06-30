using System.ComponentModel.DataAnnotations;

namespace KRD.AttendanceWeb.Models;

public class ActivityLog
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string UserType { get; set; } = string.Empty; // Admin / Employee

    public int UserId { get; set; }

    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string IpAddress { get; set; } = "localhost";
}

