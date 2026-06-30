using KRD.AttendanceWeb.Models;
using KRD.AttendanceWeb.Database;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Services;

/// <summary>
/// Singleton session store — keyed by a per-browser session token stored in a cookie/localStorage.
/// In Blazor Server the service lifetime must outlive a single circuit so we keep a static
/// dictionary and look up the current user by SessionId (set once per browser tab).
/// </summary>
public class AuthService
{
    private readonly AppDbContext _db;

    // Per-instance state (Scoped = one per Blazor circuit, which is what we want)
    public Admin?    CurrentAdmin    { get; private set; }
    public Employee? CurrentEmployee { get; private set; }

    public bool IsAdminLoggedIn    => CurrentAdmin    != null;
    public bool IsEmployeeLoggedIn => CurrentEmployee != null;
    public bool IsLoggedIn         => IsAdminLoggedIn || IsEmployeeLoggedIn;
    public string Role             => IsAdminLoggedIn ? "Admin" : IsEmployeeLoggedIn ? "Employee" : "";

    public AuthService(AppDbContext db) => _db = db;

    public bool LoginAdmin(string email, string password)
    {
        // Create a fresh context query (avoid stale tracked entities)
        var admin = _db.Admins.AsNoTracking().FirstOrDefault(a =>
            a.Email.ToLower() == email.ToLower() && a.IsActive);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            return false;

        // Update LastLogin on tracked entity
        var tracked = _db.Admins.Find(admin.Id);
        if (tracked != null)
        {
            tracked.LastLogin = DateTime.Now;
            _db.SaveChanges();
        }

        CurrentAdmin    = admin;
        CurrentEmployee = null;
        return true;
    }

    public bool LoginEmployee(string employeeId, string password)
    {
        var emp = _db.Employees
            .Include(e => e.Department)
            .AsNoTracking()
            .FirstOrDefault(e =>
                (e.EmployeeId == employeeId || e.Email.ToLower() == employeeId.ToLower())
                && e.IsActive);

        if (emp == null || !BCrypt.Net.BCrypt.Verify(password, emp.PasswordHash))
            return false;

        // Update LastLogin
        var tracked = _db.Employees.Find(emp.Id);
        if (tracked != null)
        {
            tracked.LastLogin = DateTime.Now;
            _db.SaveChanges();
        }

        CurrentEmployee = emp;
        CurrentAdmin    = null;
        return true;
    }

    public void Logout()
    {
        CurrentAdmin    = null;
        CurrentEmployee = null;
    }
}
