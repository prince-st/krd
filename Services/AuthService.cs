using KRD.AttendanceWeb.Models;
using KRD.AttendanceWeb.Database;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public Admin?    CurrentAdmin    { get; private set; }
    public Employee? CurrentEmployee { get; private set; }

    public bool IsAdminLoggedIn    => CurrentAdmin    != null;
    public bool IsEmployeeLoggedIn => CurrentEmployee != null;
    public bool IsLoggedIn         => IsAdminLoggedIn || IsEmployeeLoggedIn;
    public string Role             => IsAdminLoggedIn ? "Admin" : IsEmployeeLoggedIn ? "Employee" : "";

    public AuthService(AppDbContext db) => _db = db;

    public bool LoginAdmin(string email, string password)
    {
        var admin = _db.Admins.FirstOrDefault(a =>
            a.Email.ToLower() == email.ToLower() && a.IsActive);
        if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            return false;
        admin.LastLogin = DateTime.Now;
        _db.SaveChanges();
        CurrentAdmin = admin;
        CurrentEmployee = null;
        return true;
    }

    public bool LoginEmployee(string employeeId, string password)
    {
        var emp = _db.Employees
            .Include(e => e.Department)
            .FirstOrDefault(e => (e.EmployeeId == employeeId || e.Email.ToLower() == employeeId.ToLower())
                              && e.IsActive);
        if (emp == null || !BCrypt.Net.BCrypt.Verify(password, emp.PasswordHash))
            return false;
        emp.LastLogin = DateTime.Now;
        _db.SaveChanges();
        CurrentEmployee = emp;
        CurrentAdmin = null;
        return true;
    }

    public void Logout()
    {
        CurrentAdmin    = null;
        CurrentEmployee = null;
    }
}
