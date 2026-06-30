using ClosedXML.Excel;
using KRD.AttendanceWeb.Database;
using KRD.AttendanceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace KRD.AttendanceWeb.Services;

public class ExcelExportService
{
    private readonly AppDbContext _db;
    private readonly AttendanceService _svc;

    public ExcelExportService(AppDbContext db, AttendanceService svc)
    {
        _db  = db;
        _svc = svc;
    }

    // ── Palette ────────────────────────────────────────────────────────
    private static readonly XLColor BgHeader    = XLColor.FromHtml("#2D3561");
    private static readonly XLColor BgSubHeader = XLColor.FromHtml("#1A237E");
    private static readonly XLColor BgAlt       = XLColor.FromHtml("#F5F6FA");
    private static readonly XLColor AccentBlue  = XLColor.FromHtml("#4361EE");
    private static readonly XLColor AccentGreen = XLColor.FromHtml("#22C55E");
    private static readonly XLColor AccentRed   = XLColor.FromHtml("#EF4444");
    private static readonly XLColor AccentAmber = XLColor.FromHtml("#F59E0B");
    private static readonly XLColor AccentPurple= XLColor.FromHtml("#7209B7");
    private static readonly XLColor White       = XLColor.White;

    // ══════════════════════════════════════════════════════════════════
    // 1.  SHIFT SCHEDULE  (like the Excel in your screenshot)
    // ══════════════════════════════════════════════════════════════════
    public byte[] ExportShiftSchedule()
    {
        var employees = _db.Employees.Include(e => e.Department)
                           .Where(e => e.IsActive).OrderBy(e => e.FullName).ToList();
        var settings  = _svc.GetSettings();

        using var wb  = new XLWorkbook();
        var ws = wb.Worksheets.Add("Shift Schedule");

        // ── Title ──────────────────────────────────────────────────────
        ws.Cell("A1").Value = "KRD ATTENDANCE SYSTEM — SHIFT SCHEDULE";
        var titleRange = ws.Range("A1:J1");
        titleRange.Merge();
        titleRange.Style
            .Font.SetFontSize(16).Font.SetBold(true).Font.SetFontColor(White)
            .Fill.SetBackgroundColor(AccentPurple)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(1).Height = 32;

        ws.Cell("A2").Value = $"Generated: {DateTime.Now:dd MMM yyyy  hh:mm tt}";
        ws.Range("A2:J2").Merge().Style
            .Font.SetFontColor(XLColor.Gray).Font.SetItalic(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Company Default box ────────────────────────────────────────
        ws.Cell("A4").Value = "COMPANY DEFAULT SHIFT";
        ws.Range("A4:D4").Merge().Style.Font.SetBold(true).Font.SetFontColor(White)
            .Fill.SetBackgroundColor(BgHeader)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var defVals = new (string label, string val)[]
        {
            ("Start Time",     DateTime.Today.Add(settings.OfficeStartTime).ToString("h:mm tt")),
            ("End Time",       DateTime.Today.Add(settings.OfficeEndTime).ToString("h:mm tt")),
            ("Required Hours", settings.RequiredWorkingHours.ToString()),
            ("Grace (min)",    settings.GraceTimeMinutes.ToString())
        };
        for (int i = 0; i < 4; i++)
        {
            ws.Cell(5, i + 1).Value = defVals[i].label;
            ws.Cell(5, i + 1).Style.Font.SetBold(true).Fill.SetBackgroundColor(BgSubHeader)
                .Font.SetFontColor(White);
            ws.Cell(6, i + 1).Value = defVals[i].val;
            ws.Cell(6, i + 1).Style.Font.SetFontColor(AccentBlue);
        }

        // ── Headers ────────────────────────────────────────────────────
        int row = 8;
        string[] headers = { "#", "Employee ID", "Employee Name", "Department", "Position",
                              "Shift Name", "Start Time", "End Time", "Req. Hours", "Type" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold(true).Font.SetFontColor(White)
                .Fill.SetBackgroundColor(BgHeader)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        ws.Row(row).Height = 22;

        // ── Data ───────────────────────────────────────────────────────
        int idx = 1;
        foreach (var emp in employees)
        {
            row++;
            var (start, end, reqH) = _svc.GetEffectiveShift(emp);
            bool isCustom = emp.HasCustomShift;
            bool alt = idx % 2 == 0;

            var rowRange = ws.Range(row, 1, row, 10);
            if (alt) rowRange.Style.Fill.SetBackgroundColor(BgAlt);

            ws.Cell(row, 1).Value  = idx++;
            ws.Cell(row, 2).Value  = emp.EmployeeId;
            ws.Cell(row, 3).Value  = emp.FullName;
            ws.Cell(row, 4).Value  = emp.Department?.Name ?? "—";
            ws.Cell(row, 5).Value  = emp.Position;
            ws.Cell(row, 6).Value  = string.IsNullOrEmpty(emp.ShiftName) ? "—" : emp.ShiftName;
            ws.Cell(row, 7).Value  = DateTime.Today.Add(start).ToString("h:mm tt");
            ws.Cell(row, 8).Value  = DateTime.Today.Add(end).ToString("h:mm tt");
            ws.Cell(row, 9).Value  = reqH;
            ws.Cell(row, 10).Value = isCustom ? "Custom" : "Default";

            ws.Cell(row, 7).Style.Font.SetFontColor(AccentGreen).Font.SetBold(true);
            ws.Cell(row, 8).Style.Font.SetFontColor(AccentRed).Font.SetBold(true);
            ws.Cell(row, 9).Style.Font.SetFontColor(AccentBlue).Font.SetBold(true);
            ws.Cell(row, 10).Style.Font.SetFontColor(isCustom ? AccentPurple : XLColor.Gray);
        }

        // ── Auto-fit & borders ─────────────────────────────────────────
        ws.Columns().AdjustToContents();
        ws.Range(8, 1, row, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Hair);
        ws.Column(1).Width = 5;

        return ToBytes(wb);
    }

    // ══════════════════════════════════════════════════════════════════
    // 2.  ATTENDANCE REPORT  (date range, per employee)
    // ══════════════════════════════════════════════════════════════════
    public byte[] ExportAttendanceReport(DateTime from, DateTime to, int empId = 0)
    {
        var employees = _db.Employees.Include(e => e.Department)
                           .Where(e => empId == 0 || e.Id == empId)
                           .OrderBy(e => e.FullName).ToList();

        var allAtt = _db.Attendances.Include(a => a.Employee)
                        .Where(a => a.Date.Date >= from.Date && a.Date.Date <= to.Date &&
                               (empId == 0 || a.EmployeeId == empId))
                        .OrderBy(a => a.Employee!.FullName).ThenBy(a => a.Date)
                        .ToList();

        var settings = _svc.GetSettings();

        using var wb = new XLWorkbook();

        // ── Sheet 1: Summary ───────────────────────────────────────────
        var wsSummary = wb.Worksheets.Add("Summary");
        WriteTitle(wsSummary, "EMPLOYEE ATTENDANCE SUMMARY", from, to, 9);

        string[] sumHdr = { "#", "Employee", "Department", "Present", "Late", "Early Exit",
                            "Absent", "Total Hours", "Overtime", "Attendance %" };
        WriteHeaderRow(wsSummary, 4, sumHdr);

        int workDays = WorkingDays(from, to);
        int sumRow = 5;
        int empIdx = 1;

        foreach (var emp in employees)
        {
            var recs = allAtt.Where(a => a.EmployeeId == emp.Id).ToList();
            int present   = recs.Count(r => r.CheckInTime != null);
            int late      = recs.Count(r => r.IsLate);
            int earlyExit = recs.Count(r => r.IsEarlyExit);
            int absent    = Math.Max(0, workDays - present);
            double totalH = recs.Sum(r => r.WorkingHours);
            double overtime = recs.Sum(r => r.OvertimeHours);
            double pct    = workDays > 0 ? (double)present / workDays * 100 : 0;

            bool alt = empIdx % 2 == 0;
            if (alt) wsSummary.Range(sumRow, 1, sumRow, 10).Style.Fill.SetBackgroundColor(BgAlt);

            wsSummary.Cell(sumRow, 1).Value  = empIdx++;
            wsSummary.Cell(sumRow, 2).Value  = emp.FullName;
            wsSummary.Cell(sumRow, 3).Value  = emp.Department?.Name ?? "—";
            wsSummary.Cell(sumRow, 4).Value  = present;
            wsSummary.Cell(sumRow, 5).Value  = late;
            wsSummary.Cell(sumRow, 6).Value  = earlyExit;
            wsSummary.Cell(sumRow, 7).Value  = absent;
            wsSummary.Cell(sumRow, 8).Value  = Math.Round(totalH, 2);
            wsSummary.Cell(sumRow, 9).Value  = Math.Round(overtime, 2);
            wsSummary.Cell(sumRow, 10).Value = Math.Round(pct, 1);

            wsSummary.Cell(sumRow, 4).Style.Font.SetFontColor(AccentGreen);
            wsSummary.Cell(sumRow, 5).Style.Font.SetFontColor(AccentAmber);
            wsSummary.Cell(sumRow, 7).Style.Font.SetFontColor(AccentRed);
            wsSummary.Cell(sumRow, 8).Style.Font.SetFontColor(AccentBlue);
            wsSummary.Cell(sumRow, 9).Style.Font.SetFontColor(AccentAmber);
            var pctCell = wsSummary.Cell(sumRow, 10);
            pctCell.Style.Font.SetFontColor(pct >= 80 ? AccentGreen : pct >= 60 ? AccentAmber : AccentRed)
                              .Font.SetBold(true);
            sumRow++;
        }

        // Totals row
        wsSummary.Cell(sumRow, 2).Value = "TOTALS";
        wsSummary.Cell(sumRow, 2).Style.Font.SetBold(true);
        for (int c = 4; c <= 9; c++)
        {
            wsSummary.Cell(sumRow, c).FormulaA1 = $"SUM({wsSummary.Column(c).ColumnLetter}5:{wsSummary.Column(c).ColumnLetter}{sumRow - 1})";
            wsSummary.Cell(sumRow, c).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));
        }

        wsSummary.Columns().AdjustToContents();
        wsSummary.Range(3, 1, sumRow, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Hair);

        // ── Sheet 2: Detailed Records ──────────────────────────────────
        var wsDetail = wb.Worksheets.Add("Detailed Records");
        WriteTitle(wsDetail, "DETAILED ATTENDANCE RECORDS", from, to, 9);

        string[] detHdr = { "#", "Employee", "Employee ID", "Date", "Day",
                            "Check In", "Check Out", "Working Hrs", "Overtime", "Status" };
        WriteHeaderRow(wsDetail, 4, detHdr);

        int detRow = 5;
        int detIdx = 1;
        foreach (var a in allAtt)
        {
            bool alt = detIdx % 2 == 0;
            if (alt) wsDetail.Range(detRow, 1, detRow, 10).Style.Fill.SetBackgroundColor(BgAlt);

            wsDetail.Cell(detRow, 1).Value  = detIdx++;
            wsDetail.Cell(detRow, 2).Value  = a.Employee?.FullName ?? "—";
            wsDetail.Cell(detRow, 3).Value  = a.Employee?.EmployeeId ?? "—";
            wsDetail.Cell(detRow, 4).Value  = a.Date.ToString("dd MMM yyyy");
            wsDetail.Cell(detRow, 5).Value  = a.Date.DayOfWeek.ToString();
            wsDetail.Cell(detRow, 6).Value  = a.CheckInTime?.ToString("hh:mm tt") ?? "—";
            wsDetail.Cell(detRow, 7).Value  = a.CheckOutTime?.ToString("hh:mm tt") ?? "—";
            wsDetail.Cell(detRow, 8).Value  = a.WorkingHours > 0 ? Math.Round(a.WorkingHours, 2) : 0;
            wsDetail.Cell(detRow, 9).Value  = a.OvertimeHours > 0 ? Math.Round(a.OvertimeHours, 2) : 0;
            wsDetail.Cell(detRow, 10).Value = a.Status.ToString();

            wsDetail.Cell(detRow, 6).Style.Font.SetFontColor(AccentGreen);
            wsDetail.Cell(detRow, 7).Style.Font.SetFontColor(AccentBlue);
            var statusCell = wsDetail.Cell(detRow, 10);
            statusCell.Style.Font.SetFontColor(a.Status switch
            {
                AttendanceStatus.Present  => AccentGreen,
                AttendanceStatus.Late     => AccentAmber,
                AttendanceStatus.Absent   => AccentRed,
                AttendanceStatus.EarlyExit=> XLColor.FromHtml("#06B6D4"),
                _ => AccentBlue
            });
            if (a.IsLate)
            {
                wsDetail.Cell(detRow, 6).Style.Font.SetFontColor(AccentAmber);
                wsDetail.Cell(detRow, 6).Value = $"{a.CheckInTime?.ToString("hh:mm tt")} (+{a.LateMinutes}m late)";
            }
            detRow++;
        }

        wsDetail.Columns().AdjustToContents();
        wsDetail.Range(3, 1, detRow - 1, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Hair);

        // ── Sheet 3: Per-Employee individual sheets ────────────────────
        foreach (var emp in employees.Take(20)) // max 20 individual sheets
        {
            var empRecs = allAtt.Where(a => a.EmployeeId == emp.Id).ToList();
            if (!empRecs.Any()) continue;

            var safeName = new string(emp.FullName.Take(28).ToArray()).Trim();
            var wsEmp    = wb.Worksheets.Add(safeName);

            WriteTitle(wsEmp, $"{emp.FullName.ToUpper()} — TIME SHEET", from, to, 9);

            // Shift info
            var (s, e, h) = _svc.GetEffectiveShift(emp);
            wsEmp.Cell("A3").Value = $"Shift: {DateTime.Today.Add(s):h:mm tt} → {DateTime.Today.Add(e):h:mm tt}  |  Required: {h}h/day  |  Dept: {emp.Department?.Name}";
            wsEmp.Range("A3:I3").Merge().Style.Font.SetItalic(true).Font.SetFontColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            string[] empHdr = { "Date", "Day", "Check In", "Check Out",
                                "Working Hrs", "Required Hrs", "Overtime", "Short Hrs", "Status" };
            WriteHeaderRow(wsEmp, 4, empHdr);

            int empRow = 5;
            double totalWork = 0, totalOt = 0, totalShort = 0;
            foreach (var a in empRecs)
            {
                bool alt = (empRow % 2 == 0);
                if (alt) wsEmp.Range(empRow, 1, empRow, 9).Style.Fill.SetBackgroundColor(BgAlt);

                wsEmp.Cell(empRow, 1).Value = a.Date.ToString("dd MMM yyyy");
                wsEmp.Cell(empRow, 2).Value = a.Date.DayOfWeek.ToString()[..3];
                wsEmp.Cell(empRow, 3).Value = a.CheckInTime?.ToString("hh:mm tt") ?? "—";
                wsEmp.Cell(empRow, 4).Value = a.CheckOutTime?.ToString("hh:mm tt") ?? "—";
                wsEmp.Cell(empRow, 5).Value = a.WorkingHours > 0 ? Math.Round(a.WorkingHours, 2) : 0;
                wsEmp.Cell(empRow, 6).Value = Math.Round(a.RequiredHours > 0 ? a.RequiredHours : h, 2);
                wsEmp.Cell(empRow, 7).Value = a.OvertimeHours > 0 ? Math.Round(a.OvertimeHours, 2) : 0;
                wsEmp.Cell(empRow, 8).Value = a.ShortHours > 0 ? Math.Round(a.ShortHours, 2) : 0;
                wsEmp.Cell(empRow, 9).Value = a.Status.ToString();

                wsEmp.Cell(empRow, 3).Style.Font.SetFontColor(a.IsLate ? AccentAmber : AccentGreen);
                wsEmp.Cell(empRow, 5).Style.Font.SetFontColor(AccentBlue).Font.SetBold(true);
                if (a.OvertimeHours > 0) wsEmp.Cell(empRow, 7).Style.Font.SetFontColor(AccentAmber);
                if (a.ShortHours > 0)    wsEmp.Cell(empRow, 8).Style.Font.SetFontColor(AccentRed);

                totalWork  += a.WorkingHours;
                totalOt    += a.OvertimeHours;
                totalShort += a.ShortHours;
                empRow++;
            }

            // Totals row
            wsEmp.Cell(empRow, 1).Value = "TOTAL";
            wsEmp.Cell(empRow, 1).Style.Font.SetBold(true);
            wsEmp.Cell(empRow, 5).Value = Math.Round(totalWork,  2);
            wsEmp.Cell(empRow, 7).Value = Math.Round(totalOt,    2);
            wsEmp.Cell(empRow, 8).Value = Math.Round(totalShort, 2);
            wsEmp.Range(empRow, 1, empRow, 9).Style.Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));

            wsEmp.Columns().AdjustToContents();
            wsEmp.Range(4, 1, empRow, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair);
        }

        return ToBytes(wb);
    }

    // ══════════════════════════════════════════════════════════════════
    // 3.  EMPLOYEE MASTER LIST
    // ══════════════════════════════════════════════════════════════════
    public byte[] ExportEmployeeList()
    {
        var employees = _db.Employees.Include(e => e.Department)
                           .OrderBy(e => e.FullName).ToList();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Employees");

        WriteTitle(ws, "EMPLOYEE MASTER LIST", DateTime.Today, DateTime.Today, 9);

        string[] headers = { "#", "Employee ID", "Full Name", "Email", "Phone",
                             "Department", "Position", "Join Date", "Status" };
        WriteHeaderRow(ws, 4, headers);

        int row = 5;
        int idx = 1;
        foreach (var emp in employees)
        {
            bool alt = idx % 2 == 0;
            if (alt) ws.Range(row, 1, row, 9).Style.Fill.SetBackgroundColor(BgAlt);

            ws.Cell(row, 1).Value = idx++;
            ws.Cell(row, 2).Value = emp.EmployeeId;
            ws.Cell(row, 3).Value = emp.FullName;
            ws.Cell(row, 4).Value = emp.Email;
            ws.Cell(row, 5).Value = emp.Phone;
            ws.Cell(row, 6).Value = emp.Department?.Name ?? "—";
            ws.Cell(row, 7).Value = emp.Position;
            ws.Cell(row, 8).Value = emp.JoinDate.ToString("dd MMM yyyy");
            ws.Cell(row, 9).Value = emp.IsActive ? "Active" : "Inactive";

            ws.Cell(row, 2).Style.Font.SetFontColor(AccentBlue).Font.SetBold(true);
            ws.Cell(row, 9).Style.Font.SetFontColor(emp.IsActive ? AccentGreen : AccentRed)
                                .Font.SetBold(true);
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Range(3, 1, row - 1, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Hair);

        return ToBytes(wb);
    }

    // ── Helpers ────────────────────────────────────────────────────────
    private static void WriteTitle(IXLWorksheet ws, string title, DateTime from, DateTime to, int cols)
    {
        ws.Cell("A1").Value = title;
        ws.Range(1, 1, 1, cols).Merge().Style
            .Font.SetFontSize(14).Font.SetBold(true).Font.SetFontColor(White)
            .Fill.SetBackgroundColor(AccentPurple)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(1).Height = 30;

        ws.Cell("A2").Value = $"Period: {from:dd MMM yyyy} – {to:dd MMM yyyy}   |   Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}";
        ws.Range(2, 1, 2, cols).Merge().Style
            .Font.SetItalic(true).Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void WriteHeaderRow(IXLWorksheet ws, int row, string[] headers)
    {
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold(true).Font.SetFontColor(White)
                .Fill.SetBackgroundColor(BgHeader)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        ws.Row(row).Height = 20;
    }

    private static int WorkingDays(DateTime start, DateTime end)
    {
        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        return count;
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
