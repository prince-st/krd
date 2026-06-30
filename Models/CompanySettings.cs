using System.ComponentModel.DataAnnotations;

namespace KRD.AttendanceWeb.Models;

public class CompanySettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = "KRD Company";

    [MaxLength(500)]
    public string CompanyLogoPath { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CompanyAddress { get; set; } = string.Empty;

    [MaxLength(20)]
    public string CompanyPhone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string CompanyEmail { get; set; } = string.Empty;

    // Office hours stored as TimeSpan ticks for SQLite compatibility
    public long OfficeStartTimeTicks { get; set; } = new TimeSpan(9, 0, 0).Ticks;

    public long OfficeEndTimeTicks { get; set; } = new TimeSpan(18, 0, 0).Ticks;

    public double RequiredWorkingHours { get; set; } = 8.0;

    public int GraceTimeMinutes { get; set; } = 15;

    public int LateEntryMinutes { get; set; } = 30;

    // Weekly holidays: 0=Sunday,1=Monday,...,6=Saturday (comma-separated e.g. "0,6")
    [MaxLength(20)]
    public string WeeklyHolidayDays { get; set; } = "0,6";

    // Leave rules as JSON
    [MaxLength(2000)]
    public string LeaveRulesJson { get; set; } = "{\"annual\":15,\"sick\":10,\"casual\":7,\"maternity\":90,\"paternity\":15,\"emergency\":3,\"unpaid\":0}";

    // Dark/Light theme preference
    [MaxLength(10)]
    public string Theme { get; set; } = "Dark";

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Computed helpers (not persisted)
    public TimeSpan OfficeStartTime
    {
        get => TimeSpan.FromTicks(OfficeStartTimeTicks);
        set => OfficeStartTimeTicks = value.Ticks;
    }

    public TimeSpan OfficeEndTime
    {
        get => TimeSpan.FromTicks(OfficeEndTimeTicks);
        set => OfficeEndTimeTicks = value.Ticks;
    }

    public List<DayOfWeek> WeeklyHolidays
    {
        get
        {
            var result = new List<DayOfWeek>();
            foreach (var part in WeeklyHolidayDays.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out int day))
                    result.Add((DayOfWeek)day);
            }
            return result;
        }
    }
}

