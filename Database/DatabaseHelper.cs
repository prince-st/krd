using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Database;

public static class DatabaseHelper
{
    private static string? _dbPath;

    public static string GetDatabasePath()
    {
        if (_dbPath != null) return _dbPath;
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KRD", "AttendanceWeb");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "KRDAttendanceWeb.db");
        return _dbPath;
    }

    public static void InitializeDatabase(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        ctx.Database.EnsureCreated();
        SeedDefaults(ctx);
    }

    private static void SeedDefaults(AppDbContext ctx)
    {
        if (!ctx.Admins.Any())
        {
            ctx.Admins.Add(new Admin
            {
                FullName = "System Administrator",
                Email = "admin@krd.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                IsActive = true,
                CreatedAt = DateTime.Now
            });
        }
        if (!ctx.CompanySettings.Any())
        {
            ctx.CompanySettings.Add(new CompanySettings
            {
                CompanyName = "KRD Company",
                OfficeStartTimeTicks = new TimeSpan(9, 0, 0).Ticks,
                OfficeEndTimeTicks = new TimeSpan(18, 0, 0).Ticks,
                RequiredWorkingHours = 8.0,
                GraceTimeMinutes = 15,
                LateEntryMinutes = 30,
                WeeklyHolidayDays = "0,6",
                Theme = "Dark"
            });
        }
        if (!ctx.Departments.Any())
        {
            ctx.Departments.AddRange(
                new Department { Name = "Administration",        Description = "Administrative Department" },
                new Department { Name = "Human Resources",       Description = "HR Department" },
                new Department { Name = "Information Technology",Description = "IT Department" },
                new Department { Name = "Finance",               Description = "Finance Department" },
                new Department { Name = "Operations",            Description = "Operations Department" }
            );
        }
        ctx.SaveChanges();
    }
}
