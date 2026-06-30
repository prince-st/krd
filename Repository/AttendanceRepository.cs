using KRD.AttendanceWeb.Database;
using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Repository;

public class AttendanceRepository
{
    public Attendance? GetByEmployeeAndDate(int employeeId, DateTime date)
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances
            .Include(a => a.Employee)
            .FirstOrDefault(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);
    }

    public Attendance? GetById(int id)
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances.Include(a => a.Employee).FirstOrDefault(a => a.Id == id);
    }

    public List<Attendance> GetByEmployee(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var ctx = new AppDbContext();
        var query = ctx.Attendances
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId)
            .AsQueryable();

        if (startDate.HasValue) query = query.Where(a => a.Date.Date >= startDate.Value.Date);
        if (endDate.HasValue) query = query.Where(a => a.Date.Date <= endDate.Value.Date);

        return query.OrderByDescending(a => a.Date).ToList();
    }

    public List<Attendance> GetByDateRange(DateTime startDate, DateTime endDate)
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e!.Department)
            .Where(a => a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date)
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Employee!.FullName)
            .ToList();
    }

    public List<Attendance> GetByDate(DateTime date)
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e!.Department)
            .Where(a => a.Date.Date == date.Date)
            .OrderBy(a => a.Employee!.FullName)
            .ToList();
    }

    public List<Attendance> GetByDepartmentAndDateRange(int departmentId, DateTime startDate, DateTime endDate)
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e!.Department)
            .Where(a => a.Employee!.DepartmentId == departmentId
                     && a.Date.Date >= startDate.Date
                     && a.Date.Date <= endDate.Date)
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Employee!.FullName)
            .ToList();
    }

    public bool CheckIn(Attendance attendance)
    {
        using var ctx = new AppDbContext();
        // Prevent duplicate check-in
        if (ctx.Attendances.Any(a => a.EmployeeId == attendance.EmployeeId && a.Date.Date == attendance.Date.Date))
            return false;
        ctx.Attendances.Add(attendance);
        ctx.SaveChanges();
        return true;
    }

    public bool CheckOut(int attendanceId, DateTime checkOutTime, double workingHours, double requiredHours,
        double overtimeHours, double shortHours, bool isEarlyExit, int earlyExitMinutes, AttendanceStatus status)
    {
        using var ctx = new AppDbContext();
        var record = ctx.Attendances.Find(attendanceId);
        if (record == null || record.CheckOutTime.HasValue) return false;

        record.CheckOutTime = checkOutTime;
        record.WorkingHours = workingHours;
        record.RequiredHours = requiredHours;
        record.OvertimeHours = overtimeHours;
        record.ShortHours = shortHours;
        record.IsEarlyExit = isEarlyExit;
        record.EarlyExitMinutes = earlyExitMinutes;
        record.Status = status;

        ctx.SaveChanges();
        return true;
    }

    public bool Update(Attendance attendance)
    {
        using var ctx = new AppDbContext();
        var existing = ctx.Attendances.Find(attendance.Id);
        if (existing == null) return false;

        existing.CheckInTime = attendance.CheckInTime;
        existing.CheckOutTime = attendance.CheckOutTime;
        existing.WorkingHours = attendance.WorkingHours;
        existing.RequiredHours = attendance.RequiredHours;
        existing.OvertimeHours = attendance.OvertimeHours;
        existing.ShortHours = attendance.ShortHours;
        existing.IsLate = attendance.IsLate;
        existing.IsEarlyExit = attendance.IsEarlyExit;
        existing.LateMinutes = attendance.LateMinutes;
        existing.EarlyExitMinutes = attendance.EarlyExitMinutes;
        existing.Status = attendance.Status;
        existing.Notes = attendance.Notes;

        ctx.SaveChanges();
        return true;
    }

    public bool Delete(int id)
    {
        using var ctx = new AppDbContext();
        var record = ctx.Attendances.Find(id);
        if (record == null) return false;
        ctx.Attendances.Remove(record);
        ctx.SaveChanges();
        return true;
    }

    public int GetPresentCountToday()
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances.Count(a => a.Date.Date == DateTime.Today && a.Status == AttendanceStatus.Present);
    }

    public int GetAbsentCountToday(int totalEmployees)
    {
        using var ctx = new AppDbContext();
        int present = ctx.Attendances.Count(a => a.Date.Date == DateTime.Today
            && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.HalfDay));
        return totalEmployees - present;
    }

    public int GetLateCountToday()
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances.Count(a => a.Date.Date == DateTime.Today && a.IsLate);
    }

    public List<Attendance> GetMonthlyByEmployee(int employeeId, int year, int month)
    {
        using var ctx = new AppDbContext();
        return ctx.Attendances
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId
                     && a.Date.Year == year
                     && a.Date.Month == month)
            .OrderBy(a => a.Date)
            .ToList();
    }
}

