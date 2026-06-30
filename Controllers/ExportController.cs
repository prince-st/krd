using KRD.AttendanceWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace KRD.AttendanceWeb.Controllers;

[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly ExcelExportService _excel;
    public ExportController(ExcelExportService excel) => _excel = excel;

    [HttpGet("shifts")]
    public IActionResult ShiftSchedule()
    {
        var bytes = _excel.ExportShiftSchedule();
        var name  = $"KRD_Shift_Schedule_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet("attendance")]
    public IActionResult AttendanceReport(
        [FromQuery] string from  = "",
        [FromQuery] string to    = "",
        [FromQuery] int    empId = 0)
    {
        var fromDate = DateTime.TryParse(from, out var f) ? f : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var toDate   = DateTime.TryParse(to,   out var t) ? t : DateTime.Today;
        var bytes    = _excel.ExportAttendanceReport(fromDate, toDate, empId);
        var name     = $"KRD_Attendance_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet("employees")]
    public IActionResult EmployeeList()
    {
        var bytes = _excel.ExportEmployeeList();
        var name  = $"KRD_Employees_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }
}
