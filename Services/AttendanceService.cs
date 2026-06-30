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

    // ── Shift resolver ─────────────────────────────────────────────────
    public (TimeSpan start, TimeSpan end, double requiredHours) GetEffectiveShift(Employee emp)
    {
        var s = GetSettings();
        if (emp.HasCustomShift)
            return (emp.ShiftStart!.Value, emp.ShiftEnd!.Value, emp.ShiftRequiredHours ?? s.RequiredWorkingHours);
        return (s.OfficeStartTime, s.OfficeEndTime, s.RequiredWorkingHours);
    }

    private (TimeSpan start, TimeSpan end, double reqH, int graceMin) GetShift(int employeeId)
    {
        var s   = GetSettings();
        var emp = _db.Employees.Find(employeeId);
        if (emp != null && emp.HasCustomShift)
            return (emp.ShiftStart!.Value, emp.ShiftEnd!.Value,
                    emp.ShiftRequiredHours ?? s.RequiredWorkingHours, s.GraceTimeMinutes);
        return (s.OfficeStartTime, s.OfficeEndTime, s.RequiredWorkingHours, s.GraceTimeMinutes);
    }

    // ── Multi-Punch Attendance ──────────────────────────────────────────
    public Attendance? GetTodayAttendance(int employeeId) =>
        _db.Attendances.FirstOrDefault(a =>
            a.EmployeeId == employeeId && a.Date.Date == DateTime.Today);

    public List<PunchLog> GetTodayPunches(int employeeId) =>
        _db.PunchLogs
           .Where(p => p.EmployeeId == employeeId && p.PunchIn.Date == DateTime.Today)
           .OrderBy(p => p.PunchIn).ToList();

    public PunchLog? GetOpenPunch(int employeeId) =>
        _db.PunchLogs.FirstOrDefault(p =>
            p.EmployeeId == employeeId &&
            p.PunchIn.Date == DateTime.Today &&
            p.PunchOut == null);

    /// <summary>Punch IN — can be called multiple times per day after a PunchOut.</summary>
    public (bool ok, string message) PunchIn(int employeeId)
    {
        if (GetOpenPunch(employeeId) != null)
            return (false, "Already punched in. Please punch out first.");

        var (start, _, reqH, graceMin) = GetShift(employeeId);
        var now   = DateTime.Now;
        bool isLate = now.TimeOfDay > start.Add(TimeSpan.FromMinutes(graceMin));

        // Get or create today's Attendance (daily summary record)
        var att = _db.Attendances.FirstOrDefault(a =>
            a.EmployeeId == employeeId && a.Date.Date == DateTime.Today);

        if (att == null)
        {
            att = new Attendance
            {
                EmployeeId    = employeeId,
                Date          = DateTime.Today,
                CheckInTime   = now,
                IsLate        = isLate,
                LateMinutes   = isLate ? (int)(now.TimeOfDay - start).TotalMinutes : 0,
                RequiredHours = reqH,
                Status        = isLate ? AttendanceStatus.Late : AttendanceStatus.Present
            };
            _db.Attendances.Add(att);
            _db.SaveChanges();
        }

        _db.PunchLogs.Add(new PunchLog
        {
            EmployeeId   = employeeId,
            AttendanceId = att.Id,
            PunchIn      = now
        });
        _db.SaveChanges();

        string lateNote = isLate ? $" — Late by {(int)(now.TimeOfDay - start).TotalMinutes} min" : "";
        return (true, $"Punched in at {now:hh:mm tt}{lateNote}");
    }

    /// <summary>Punch OUT — close the open punch, recalculate daily total.</summary>
    public (bool ok, string message) PunchOut(int employeeId)
    {
        var open = GetOpenPunch(employeeId);
        if (open == null) return (false, "No active punch found. Please punch in first.");

        var now = DateTime.Now;
        open.PunchOut     = now;
        open.SessionHours = (now - open.PunchIn).TotalHours;

        var att = _db.Attendances.Find(open.AttendanceId);
        if (att != null)
        {
            // Sum all closed punches for today (including this one)
            var closed = _db.PunchLogs
                .Where(p => p.AttendanceId == att.Id && p.PunchOut != null && p.Id != open.Id)
                .ToList();
            double total = closed.Sum(p => p.SessionHours) + open.SessionHours;

            var (_, end, reqH, _) = GetShift(employeeId);
            att.CheckOutTime     = now;
            att.WorkingHours     = total;
            att.RequiredHours    = reqH;
            att.OvertimeHours    = Math.Max(0, total - reqH);
            att.ShortHours       = Math.Max(0, reqH - total);
            att.IsEarlyExit      = now.TimeOfDay < end;
            att.EarlyExitMinutes = att.IsEarlyExit ? (int)(end - now.TimeOfDay).TotalMinutes : 0;
            if (att.Status != AttendanceStatus.Late)
                att.Status = att.IsEarlyExit ? AttendanceStatus.EarlyExit : AttendanceStatus.Present;
        }
        _db.SaveChanges();

        return (true, $"Punched out at {now:hh:mm tt} — Session: {open.SessionHours:F1}h · Total today: {att?.WorkingHours:F1}h");
    }

    // Backward-compat for admin manual records
    public bool CheckIn(int employeeId)  => PunchIn(employeeId).ok;
    public bool CheckOut(int employeeId) => PunchOut(employeeId).ok;

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
        _db.Leaves.Include(l => l.Employee).OrderByDescending(l => l.AppliedDate).ToList();

    public List<Leave> GetLeavesByEmployee(int empId) =>
        _db.Leaves.Where(l => l.EmployeeId == empId).OrderByDescending(l => l.AppliedDate).ToList();

    public void ApplyLeave(Leave leave) { _db.Leaves.Add(leave); _db.SaveChanges(); }

    public bool UpdateLeaveStatus(int id, LeaveStatus status, int adminId)
    {
        var l = _db.Leaves.Find(id); if (l == null) return false;
        l.Status = status; l.ReviewedDate = DateTime.Now; l.ReviewedByAdminId = adminId;
        _db.SaveChanges(); return true;
    }

    public bool UpdateLeaveStatusWithRemarks(int id, LeaveStatus status, int adminId, string remarks)
    {
        var l = _db.Leaves.Find(id); if (l == null) return false;
        l.Status = status; l.ReviewedDate = DateTime.Now;
        l.ReviewedByAdminId = adminId; l.AdminRemarks = remarks ?? string.Empty;
        _db.SaveChanges(); return true;
    }

    // ── Holidays ───────────────────────────────────────────────────────
    public List<Holiday> GetHolidays() => _db.Holidays.OrderBy(h => h.Date).ToList();
    public void AddHoliday(Holiday h)  { _db.Holidays.Add(h); _db.SaveChanges(); }
    public bool UpdateHoliday(Holiday u)
    {
        var ex = _db.Holidays.Find(u.Id); if (ex == null) return false;
        ex.Name = u.Name; ex.Date = u.Date; ex.Description = u.Description;
        _db.SaveChanges(); return true;
    }
    public bool DeleteHoliday(int id)
    {
        var h = _db.Holidays.Find(id); if (h == null) return false;
        _db.Holidays.Remove(h); _db.SaveChanges(); return true;
    }

    // ── Attendance CRUD (Admin manual) ─────────────────────────────────
    public void AddAttendance(Attendance att) { _db.Attendances.Add(att); _db.SaveChanges(); }

    public bool UpdateAttendance(Attendance u)
    {
        var ex = _db.Attendances.Find(u.Id); if (ex == null) return false;
        ex.EmployeeId = u.EmployeeId; ex.Date = u.Date;
        ex.CheckInTime = u.CheckInTime; ex.CheckOutTime = u.CheckOutTime;
        ex.WorkingHours = u.WorkingHours; ex.OvertimeHours = u.OvertimeHours;
        ex.ShortHours = u.ShortHours; ex.RequiredHours = u.RequiredHours;
        ex.IsLate = u.IsLate; ex.LateMinutes = u.LateMinutes;
        ex.IsEarlyExit = u.IsEarlyExit; ex.EarlyExitMinutes = u.EarlyExitMinutes;
        ex.Status = u.Status; ex.Notes = u.Notes;
        _db.SaveChanges(); return true;
    }

    public bool DeleteAttendance(int id)
    {
        var a = _db.Attendances.Find(id); if (a == null) return false;
        _db.Attendances.Remove(a); _db.SaveChanges(); return true;
    }

    // ── Stats ──────────────────────────────────────────────────────────
    public int PresentToday() => _db.Attendances.Count(a => a.Date.Date == DateTime.Today &&
        (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));
    public int LateToday()    => _db.Attendances.Count(a => a.Date.Date == DateTime.Today && a.IsLate);
    public int TotalActive()  => _db.Employees.Count(e => e.IsActive);
    public int PendingLeaves()=> _db.Leaves.Count(l => l.Status == LeaveStatus.Pending);

    // ── Settings ───────────────────────────────────────────────────────
    public void SaveSettings(CompanySettings u)
    {
        var ex = _db.CompanySettings.FirstOrDefault();
        if (ex == null) { _db.CompanySettings.Add(u); }
        else
        {
            ex.CompanyName = u.CompanyName; ex.CompanyEmail = u.CompanyEmail;
            ex.CompanyPhone = u.CompanyPhone; ex.CompanyAddress = u.CompanyAddress;
            ex.OfficeStartTimeTicks = u.OfficeStartTimeTicks;
            ex.OfficeEndTimeTicks   = u.OfficeEndTimeTicks;
            ex.RequiredWorkingHours = u.RequiredWorkingHours;
            ex.GraceTimeMinutes     = u.GraceTimeMinutes;
            ex.LateEntryMinutes     = u.LateEntryMinutes;
            ex.WeeklyHolidayDays    = u.WeeklyHolidayDays;
            ex.Theme = u.Theme; ex.UpdatedAt = DateTime.Now;
        }
        _db.SaveChanges();
    }

    // ── Profile Update ─────────────────────────────────────────────────
    public bool UpdateProfile(int empId, bool isAdmin, string name, string email, string phone,
                              string? newPassword, string currentPassword)
    {
        if (isAdmin)
        {
            var admin = _db.Admins.FirstOrDefault(); if (admin == null) return false;
            if (!string.IsNullOrEmpty(name))  admin.FullName = name;
            if (!string.IsNullOrEmpty(email)) admin.Email    = email;
            if (!string.IsNullOrEmpty(newPassword))
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges(); return true;
        }
        else
        {
            var emp = _db.Employees.Find(empId); if (emp == null) return false;
            if (!string.IsNullOrEmpty(name))  emp.FullName = name;
            if (!string.IsNullOrEmpty(email)) emp.Email    = email;
            emp.Phone = phone;
            if (!string.IsNullOrEmpty(newPassword))
                emp.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges(); return true;
        }
    }
}
