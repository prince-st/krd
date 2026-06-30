using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace KRD.AttendanceWeb.Database;

public class AppDbContext : DbContext
{
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<PunchLog> PunchLogs { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<Leave> Leaves { get; set; }
    public DbSet<CompanySettings> CompanySettings { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string dbPath = DatabaseHelper.GetDatabasePath();
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore computed properties for CompanySettings
        modelBuilder.Entity<CompanySettings>()
            .Ignore(c => c.OfficeStartTime)
            .Ignore(c => c.OfficeEndTime)
            .Ignore(c => c.WeeklyHolidays);

        // Indexes
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeId)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Email)
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        // PunchLog relationships
        modelBuilder.Entity<PunchLog>()
            .HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PunchLog>()
            .HasOne(p => p.Attendance)
            .WithMany()
            .HasForeignKey(p => p.AttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationships
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Leave>()
            .HasOne(l => l.Employee)
            .WithMany(e => e.Leaves)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

