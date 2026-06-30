using KRD.AttendanceWeb.Database;
using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Services;

public class AttendanceService
{
    private readonly AppDbContext _db;
    public AttendanceService(AppDbContext db) => _db = db;

    public CompanySettings GetSettings() =>
        _db.CompanySettings.FirstOrDefault() ?? new CompanySettings();

    // ── Employees ──────────────────────────────────────────────────────
    public List<Employee> GetAllEmployees(bool activeOnly = true)
    {
        var q = _db.Employees.Include(e => e.Department).AsQueryable();
        if (activeOnly) q = q.Where(e => e.IsActive);
        return q.OrderBy(e => e.FullName).ToList();
    }

    public List<Department> GetDepartments() =>
        _db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();

    public bool AddEmployee(Employee emp)
    {
        if (_db.Employees.Any(e => e.EmployeeId == emp.EmployeeId)) return false;
        if (_db.Employees.Any(e => e.Email.ToLower() == emp.Email.ToLower())) return false;
        _db.Employees.Add(emp);
        _db.SaveChanges();
        return true;
    }

    public bool UpdateEmployee(Employee emp)
    {
        var ex = _db.Employees.Find(emp.Id);
        if (ex == null) return false;
        ex.FullName           = emp.FullName;
        ex.Email              = emp.Email;
        ex.Phone              = emp.Phone;
        ex.DepartmentId       = emp.DepartmentId;
        ex.Position           = emp.Position;
        ex.JoinDate           = emp.JoinDate;
        ex.IsActive           = emp.IsActive;
        ex.ShiftStartTicks    = emp.ShiftStartTicks;
        ex.ShiftEndTicks      = emp.ShiftEndTicks;
        ex.ShiftRequiredHours = emp.ShiftRequiredHours;
        ex.ShiftName          = emp.ShiftName;
        _db.SaveChanges();
        return true;
    }

    public bool DeactivateEmployee(int id)
    {
        var emp = _db.Employees.Find(id);
        if (emp == null) return false;
        emp.IsActive = false;
        _db.SaveChanges();
        return true;
    }

    // ── Attendance ─────────────────────────────────────────────────────
    public Attendance? GetTodayAttendance(int employeeId) =>
        _db.Attendances.FirstOrDefault(a =>
            a.EmployeeId == employeeId && a.Date.Date == DateTime.Today);

    public (TimeSpan start, TimeSpan end, double requiredHours) GetEffectiveShift(Employee emp)
    {
        var settings = GetSettings();
        if (emp.HasCustomShift)
            return (emp.ShiftStart!.Value, emp.ShiftEnd!.Value, emp.ShiftRequiredHours ?? settings.RequiredWorkingHours);
        return (settings.OfficeStartTime, settings.OfficeEndTime, settings.RequiredWorkingHours);
    }

    // ── Shift resolver ─────────────────────────────────────────────────
    private (TimeSpan start, TimeSpan end, double requiredHours, int graceMinutes) GetShift(int employeeId)
    {
        var settings = GetSettings();
        var emp = _db.Employees.Find(employeeId);
        if (emp != null && emp.HasCustomShift)
        {
            return (
                emp.ShiftStart!.Value,
                emp.ShiftEnd!.Value,
                emp.ShiftRequiredHours ?? settings.RequiredWorkingHours,
                settings.GraceTimeMinutes
            );
        }
        return (settings.OfficeStartTime, settings.OfficeEndTime, settings.RequiredWorkingHours, settings.GraceTimeMinutes);
    }

    public bool CheckIn(int employeeId)
    {
        if (_db.Attendances.Any(a => a.EmployeeId == employeeId && a.Date.Date == DateTime.Today))
            return false;
        var (start, _, requiredHours, graceMinutes) = GetShift(employeeId);
        var now   = DateTime.Now;
        var grace = start.Add(TimeSpan.FromMinutes(graceMinutes));
        bool isLate = now.TimeOfDay > grace;
        _db.Attendances.Add(new Attendance
        {
            EmployeeId    = employeeId,
            Date          = DateTime.Today,
            CheckInTime   = now,
            Status        = isLate ? AttendanceStatus.Late : AttendanceStatus.Present,
            IsLate        = isLate,
            LateMinutes   = isLate ? (int)(now.TimeOfDay - start).TotalMinutes : 0,
            RequiredHours = requiredHours
        });
        _db.SaveChanges();
        return true;
    }

    public bool CheckOut(int employeeId)
    {
        var att = GetTodayAttendance(employeeId);
        if (att == null || att.CheckInTime == null || att.CheckOutTime != null) return false;
        var (_, end, requiredHours, _) = GetShift(employeeId);
        var now  = DateTime.Now;
        double hrs = (now - att.CheckInTime.Value).TotalHours;
        bool earlyExit = now.TimeOfDay < end;
        att.CheckOutTime     = now;
        att.WorkingHours     = hrs;
        att.RequiredHours    = requiredHours;
        att.OvertimeHours    = Math.Max(0, hrs - requiredHours);
        att.ShortHours       = Math.Max(0, requiredHours - hrs);
        att.IsEarlyExit      = earlyExit;
        att.EarlyExitMinutes = earlyExit ? (int)(end - now.TimeOfDay).TotalMinutes : 0;
        att.Status = earlyExit ? AttendanceStatus.EarlyExit :
                     att.IsLate ? AttendanceStatus.Late : AttendanceStatus.Present;
        _db.SaveChanges();
        return true;
    }

    public List<Attendance> GetAttendanceByDate(DateTime date) =>
        _db.Attendances.Include(a => a.Employee).ThenInclude(e => e!.Department)
           .Where(a => a.Date.Date == date.Date)
           .OrderBy(a => a.Employee!.FullName).ToList();

    public List<Attendance> GetAttendanceByEmployee(int empId, DateTime from, DateTime to) =>
        _db.Attendances.Where(a => a.EmployeeId == empId &&
            a.Date.Date >= from.Date && a.Date.Date <= to.Date)
           .OrderByDescending(a => a.Date).ToList();

    public List<Attendance> GetAttendanceRange(DateTime from, DateTime to) =>
        _db.Attendances.Include(a => a.Employee).ThenInclude(e => e!.Department)
           .Where(a => a.Date.Date >= from.Date && a.Date.Date <= to.Date)
           .OrderByDescending(a => a.Date).ThenBy(a => a.Employee!.FullName).ToList();

    // ── Leave ──────────────────────────────────────────────────────────
    public List<Leave> GetAllLeaves() =>
        _db.Leaves.Include(l => l.Employee)
           .OrderByDescending(l => l.AppliedDate).ToList();

    public List<Leave> GetLeavesByEmployee(int empId) =>
        _db.Leaves.Where(l => l.EmployeeId == empId)
           .OrderByDescending(l => l.AppliedDate).ToList();

    public void ApplyLeave(Leave leave) { _db.Leaves.Add(leave); _db.SaveChanges(); }

    public bool UpdateLeaveStatus(int id, LeaveStatus status, int adminId)
    {
        var l = _db.Leaves.Find(id);
        if (l == null) return false;
        l.Status = status; l.ReviewedDate = DateTime.Now; l.ReviewedByAdminId = adminId;
        _db.SaveChanges();
        return true;
    }

    // ── Holidays ───────────────────────────────────────────────────────
    public List<Holiday> GetHolidays() =>
        _db.Holidays.OrderBy(h => h.Date).ToList();

    public void AddHoliday(Holiday h) { _db.Holidays.Add(h); _db.SaveChanges(); }

    public bool DeleteHoliday(int id)
    {
        var h = _db.Holidays.Find(id);
        if (h == null) return false;
        _db.Holidays.Remove(h); _db.SaveChanges();
        return true;
    }

    // ── Attendance CRUD ────────────────────────────────────────────────
    public void AddAttendance(Attendance att)
    {
        _db.Attendances.Add(att);
        _db.SaveChanges();
    }

    public bool UpdateAttendance(Attendance updated)
    {
        var ex = _db.Attendances.Find(updated.Id);
        if (ex == null) return false;
        ex.EmployeeId      = updated.EmployeeId;
        ex.Date            = updated.Date;
        ex.CheckInTime     = updated.CheckInTime;
        ex.CheckOutTime    = updated.CheckOutTime;
        ex.WorkingHours    = updated.WorkingHours;
        ex.OvertimeHours   = updated.OvertimeHours;
        ex.ShortHours      = updated.ShortHours;
        ex.RequiredHours   = updated.RequiredHours;
        ex.IsLate          = updated.IsLate;
        ex.LateMinutes     = updated.LateMinutes;
        ex.IsEarlyExit     = updated.IsEarlyExit;
        ex.EarlyExitMinutes= updated.EarlyExitMinutes;
        ex.Status          = updated.Status;
        ex.Notes           = updated.Notes;
        _db.SaveChanges();
        return true;
    }

    public bool DeleteAttendance(int id)
    {
        var a = _db.Attendances.Find(id);
        if (a == null) return false;
        _db.Attendances.Remove(a);
        _db.SaveChanges();
        return true;
    }

    // ── Holiday Update ─────────────────────────────────────────────────
    public bool UpdateHoliday(Holiday updated)
    {
        var ex = _db.Holidays.Find(updated.Id);
        if (ex == null) return false;
        ex.Name        = updated.Name;
        ex.Date        = updated.Date;
        ex.Description = updated.Description;
        _db.SaveChanges();
        return true;
    }

    // ── Leave with Remarks ─────────────────────────────────────────────
    public bool UpdateLeaveStatusWithRemarks(int id, LeaveStatus status, int adminId, string remarks)
    {
        var l = _db.Leaves.Find(id);
        if (l == null) return false;
        l.Status           = status;
        l.ReviewedDate     = DateTime.Now;
        l.ReviewedByAdminId= adminId;
        l.AdminRemarks     = remarks ?? string.Empty;
        _db.SaveChanges();
        return true;
    }

    // ── Stats ──────────────────────────────────────────────────────────
    public int PresentToday() =>
        _db.Attendances.Count(a => a.Date.Date == DateTime.Today &&
            (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));
    public int LateToday() =>
        _db.Attendances.Count(a => a.Date.Date == DateTime.Today && a.IsLate);
    public int TotalActive() =>
        _db.Employees.Count(e => e.IsActive);
    public int PendingLeaves() =>
        _db.Leaves.Count(l => l.Status == LeaveStatus.Pending);

    // ── Settings ───────────────────────────────────────────────────────
    public void SaveSettings(CompanySettings updated)
    {
        var existing = _db.CompanySettings.FirstOrDefault();
        if (existing == null) { _db.CompanySettings.Add(updated); }
        else
        {
            existing.CompanyName          = updated.CompanyName;
            existing.CompanyEmail         = updated.CompanyEmail;
            existing.CompanyPhone         = updated.CompanyPhone;
            existing.CompanyAddress       = updated.CompanyAddress;
            existing.OfficeStartTimeTicks = updated.OfficeStartTimeTicks;
            existing.OfficeEndTimeTicks   = updated.OfficeEndTimeTicks;
            existing.RequiredWorkingHours = updated.RequiredWorkingHours;
            existing.GraceTimeMinutes     = updated.GraceTimeMinutes;
            existing.LateEntryMinutes     = updated.LateEntryMinutes;
            existing.WeeklyHolidayDays    = updated.WeeklyHolidayDays;
            existing.Theme                = updated.Theme;
            existing.UpdatedAt            = DateTime.Now;
        }
        _db.SaveChanges();
    }

    // ── Profile Update ─────────────────────────────────────────────────
    public bool UpdateProfile(int empId, bool isAdmin, string name, string email, string phone,
                              string? newPassword, string currentPassword)
    {
        if (isAdmin)
        {
            var admin = _db.Admins.FirstOrDefault();
            if (admin == null) return false;
            if (!string.IsNullOrEmpty(name))  admin.FullName = name;
            if (!string.IsNullOrEmpty(email)) admin.Email    = email;
            if (!string.IsNullOrEmpty(newPassword))
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            return true;
        }
        else
        {
            var emp = _db.Employees.Find(empId);
            if (emp == null) return false;
            if (!string.IsNullOrEmpty(name))  emp.FullName = name;
            if (!string.IsNullOrEmpty(email)) emp.Email    = email;
            emp.Phone = phone;
            if (!string.IsNullOrEmpty(newPassword))
                emp.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            return true;
        }
    }
}
