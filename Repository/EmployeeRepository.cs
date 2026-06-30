using KRD.AttendanceWeb.Database;
using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Repository;

public class EmployeeRepository
{
    public Employee? GetByEmployeeId(string employeeId)
    {
        using var ctx = new AppDbContext();
        return ctx.Employees
            .Include(e => e.Department)
            .FirstOrDefault(e => e.EmployeeId == employeeId && e.IsActive);
    }

    public Employee? GetById(int id)
    {
        using var ctx = new AppDbContext();
        return ctx.Employees
            .Include(e => e.Department)
            .FirstOrDefault(e => e.Id == id);
    }

    public List<Employee> GetAll(bool activeOnly = true)
    {
        using var ctx = new AppDbContext();
        var query = ctx.Employees.Include(e => e.Department).AsQueryable();
        if (activeOnly) query = query.Where(e => e.IsActive);
        return query.OrderBy(e => e.FullName).ToList();
    }

    public List<Employee> GetByDepartment(int departmentId)
    {
        using var ctx = new AppDbContext();
        return ctx.Employees
            .Include(e => e.Department)
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .OrderBy(e => e.FullName)
            .ToList();
    }

    public bool Add(Employee employee)
    {
        using var ctx = new AppDbContext();
        if (ctx.Employees.Any(e => e.EmployeeId == employee.EmployeeId))
            return false;
        if (ctx.Employees.Any(e => e.Email.ToLower() == employee.Email.ToLower()))
            return false;
        ctx.Employees.Add(employee);
        ctx.SaveChanges();
        return true;
    }

    public bool Update(Employee employee)
    {
        using var ctx = new AppDbContext();
        var existing = ctx.Employees.Find(employee.Id);
        if (existing == null) return false;

        existing.FullName = employee.FullName;
        existing.Email = employee.Email;
        existing.Phone = employee.Phone;
        existing.DepartmentId = employee.DepartmentId;
        existing.Position = employee.Position;
        existing.JoinDate = employee.JoinDate;
        existing.IsActive = employee.IsActive;

        ctx.SaveChanges();
        return true;
    }

    public bool Delete(int id)
    {
        using var ctx = new AppDbContext();
        var emp = ctx.Employees.Find(id);
        if (emp == null) return false;
        emp.IsActive = false;
        ctx.SaveChanges();
        return true;
    }

    public bool UpdateLastLogin(int employeeId)
    {
        using var ctx = new AppDbContext();
        var emp = ctx.Employees.Find(employeeId);
        if (emp == null) return false;
        emp.LastLogin = DateTime.Now;
        ctx.SaveChanges();
        return true;
    }

    public bool UpdatePassword(int employeeId, string newPasswordHash)
    {
        using var ctx = new AppDbContext();
        var emp = ctx.Employees.Find(employeeId);
        if (emp == null) return false;
        emp.PasswordHash = newPasswordHash;
        ctx.SaveChanges();
        return true;
    }

    public bool EmployeeIdExists(string employeeId, int excludeId = 0)
    {
        using var ctx = new AppDbContext();
        return ctx.Employees.Any(e => e.EmployeeId == employeeId && e.Id != excludeId);
    }

    public bool EmailExists(string email, int excludeId = 0)
    {
        using var ctx = new AppDbContext();
        return ctx.Employees.Any(e => e.Email.ToLower() == email.ToLower() && e.Id != excludeId);
    }

    public int GetTotalCount(bool activeOnly = true)
    {
        using var ctx = new AppDbContext();
        var query = ctx.Employees.AsQueryable();
        if (activeOnly) query = query.Where(e => e.IsActive);
        return query.Count();
    }
}

