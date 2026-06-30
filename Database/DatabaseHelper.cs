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
            ctx.SaveChanges(); // save departments first so we can reference their IDs
        }

        if (!ctx.Employees.Any())
        {
            // Get department IDs
            int hrId   = ctx.Departments.First(d => d.Name == "Human Resources").Id;
            int itId   = ctx.Departments.First(d => d.Name == "Information Technology").Id;
            int finId  = ctx.Departments.First(d => d.Name == "Finance").Id;
            int opsId  = ctx.Departments.First(d => d.Name == "Operations").Id;
            int admId  = ctx.Departments.First(d => d.Name == "Administration").Id;

            ctx.Employees.AddRange(
                new Employee
                {
                    EmployeeId   = "EMP001",
                    FullName     = "Ali Hassan",
                    Email        = "ali.hassan@krd.com",
                    Phone        = "0300-1111111",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp@123"),
                    DepartmentId = itId,
                    Position     = "Software Engineer",
                    JoinDate     = new DateTime(2023, 1, 15),
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                },
                new Employee
                {
                    EmployeeId   = "EMP002",
                    FullName     = "Sara Khan",
                    Email        = "sara.khan@krd.com",
                    Phone        = "0300-2222222",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp@123"),
                    DepartmentId = hrId,
                    Position     = "HR Manager",
                    JoinDate     = new DateTime(2022, 6, 1),
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                },
                new Employee
                {
                    EmployeeId   = "EMP003",
                    FullName     = "Bilal Ahmed",
                    Email        = "bilal.ahmed@krd.com",
                    Phone        = "0300-3333333",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp@123"),
                    DepartmentId = finId,
                    Position     = "Accountant",
                    JoinDate     = new DateTime(2023, 3, 10),
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                },
                new Employee
                {
                    EmployeeId   = "EMP004",
                    FullName     = "Fatima Noor",
                    Email        = "fatima.noor@krd.com",
                    Phone        = "0300-4444444",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp@123"),
                    DepartmentId = opsId,
                    Position     = "Operations Coordinator",
                    JoinDate     = new DateTime(2024, 1, 5),
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                },
                new Employee
                {
                    EmployeeId   = "EMP005",
                    FullName     = "Usman Tariq",
                    Email        = "usman.tariq@krd.com",
                    Phone        = "0300-5555555",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp@123"),
                    DepartmentId = admId,
                    Position     = "Admin Officer",
                    JoinDate     = new DateTime(2022, 9, 20),
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                }
            );
        }
        ctx.SaveChanges();
    }
}
