using KRD.AttendanceWeb.Database;
using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Repository;

public class AdminRepository
{
    public Admin? GetByEmail(string email)
    {
        using var ctx = new AppDbContext();
        return ctx.Admins.FirstOrDefault(a => a.Email.ToLower() == email.ToLower() && a.IsActive);
    }

    public Admin? GetById(int id)
    {
        using var ctx = new AppDbContext();
        return ctx.Admins.Find(id);
    }

    public List<Admin> GetAll()
    {
        using var ctx = new AppDbContext();
        return ctx.Admins.Where(a => a.IsActive).ToList();
    }

    public bool UpdateLastLogin(int adminId)
    {
        using var ctx = new AppDbContext();
        var admin = ctx.Admins.Find(adminId);
        if (admin == null) return false;
        admin.LastLogin = DateTime.Now;
        ctx.SaveChanges();
        return true;
    }

    public bool UpdatePassword(int adminId, string newPasswordHash)
    {
        using var ctx = new AppDbContext();
        var admin = ctx.Admins.Find(adminId);
        if (admin == null) return false;
        admin.PasswordHash = newPasswordHash;
        ctx.SaveChanges();
        return true;
    }
}

