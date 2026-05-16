using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-reports")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnReports)]
public class SlnReportController : ControllerBase
{
    private readonly ISlnReportFactory _reportFactory;
    private readonly SlnReportEmailService _reportEmailService;
    private readonly SlnReportEmailQueue _reportEmailQueue;

    public SlnReportController(
        ISlnReportFactory reportFactory,
        SlnReportEmailService reportEmailService,
        SlnReportEmailQueue reportEmailQueue)
    {
        _reportFactory = reportFactory;
        _reportEmailService = reportEmailService;
        _reportEmailQueue = reportEmailQueue;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<SlnKpiReportDto>> GetKpiReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetKpiReportAsync(customerId, from, to, access.BranchId);
        return Ok(report);
    }

    [HttpGet("branch-comparison")]
    public async Task<ActionResult<SlnBranchComparisonReportDto>> GetBranchComparisonReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetBranchComparisonReportAsync(customerId, from, to, access.BranchId);
        return Ok(report);
    }

    [HttpGet("sales")]
    public async Task<ActionResult<SlnSalesReportDto>> GetSalesReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetSalesReportAsync(customerId, from, to, access.BranchId);
        return Ok(report);
    }

    [HttpGet("staff")]
    public async Task<ActionResult<SlnStaffReportDto>> GetStaffReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetStaffReportAsync(customerId, from, to, access.BranchId);
        return Ok(report);
    }

    [HttpGet("stock")]
    public async Task<ActionResult<SlnStockReportDto>> GetStockReport()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetStockReportAsync(customerId, access.BranchId);
        return Ok(report);
    }

    [HttpGet("finance")]
    public async Task<ActionResult<SlnFinanceReportDto>> GetFinanceReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetFinanceReportAsync(customerId, from, to, access.BranchId);
        return Ok(report);
    }

    [HttpGet("clients")]
    public async Task<ActionResult<SlnClientReportDto>> GetClientReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var report = await _reportFactory.GetClientReportAsync(customerId, from, to, access.BranchId);
        return Ok(report);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string report,
        [FromQuery] string format,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var reportKey = NormalizeReportKey(report);
        if (!AllowedReportKeys.Contains(reportKey))
            return BadRequest(new { message = "Desteklenmeyen rapor turu." });

        var requestedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        var isExcel = requestedFormat is "xlsx" or "excel";
        var isPdf = requestedFormat == "pdf";
        if (!isExcel && !isPdf && requestedFormat != "csv")
            return BadRequest(new { message = "Bu rapor icin CSV, Excel ve PDF formatlari destekleniyor." });

        var bytes = isPdf
            ? await _reportFactory.ExportSalonReportPdfAsync(customerId, reportKey, from, to, access.BranchId)
            : isExcel
                ? await _reportFactory.ExportSalonReportExcelAsync(customerId, reportKey, from, to, access.BranchId)
                : await _reportFactory.ExportSalonReportCsvAsync(customerId, reportKey, from, to, access.BranchId);

        var extension = isPdf ? "pdf" : isExcel ? "xlsx" : "csv";
        var contentType = isExcel
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : isPdf
                ? "application/pdf"
            : "text/csv; charset=utf-8";
        var fileName = $"salon-{reportKey}-raporu-{DateTime.UtcNow:yyyyMMdd}.{extension}";
        return File(bytes, contentType, fileName);
    }

    [HttpPost("email")]
    public async Task<ActionResult> EmailReport([FromBody] SlnReportEmailRequest request, CancellationToken ct)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveReportAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var reportKey = NormalizeReportKey(request.Report);
        if (!AllowedReportKeys.Contains(reportKey))
            return BadRequest(new { message = "Desteklenmeyen rapor turu." });

        var recipients = (request.ToAddresses ?? [])
            .SelectMany(x => x.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim())
            .Where(x => x.Contains('@'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (recipients.Count == 0)
            return BadRequest(new { message = "En az bir gecerli e-posta adresi girin." });

        var job = new SlnReportEmailJob
        {
            CustomerId = customerId,
            BranchId = access.BranchId,
            IntegrationId = request.IntegrationId,
            Report = reportKey,
            Format = request.Format,
            From = request.From,
            To = request.To,
            ToAddresses = recipients,
            Subject = request.Subject,
            Message = request.Message,
            ScheduledAtUtc = NormalizeScheduledAtUtc(request.ScheduledAt)
        };

        if (job.ScheduledAtUtc > DateTime.UtcNow.AddSeconds(15))
        {
            _reportEmailQueue.Enqueue(job);
            return Accepted(new { jobId = job.Uid, scheduledAt = job.ScheduledAtUtc, message = "Rapor e-postasi zamanlandi." });
        }

        var result = await _reportEmailService.SendAsync(job, ct);
        if (result.Sent == 0)
            return BadRequest(new { message = "Rapor e-postasi gonderilemedi.", errors = result.Errors });
        return Ok(new { sent = result.Sent, errors = result.Errors, message = "Rapor e-postasi gonderildi." });
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private int GetCustomerRoleId()
    {
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : SalonRoles.Ids.SalonOwner;
    }

    private ReportAccess ResolveReportAccess()
    {
        var roleId = GetCustomerRoleId();
        if (!SalonRolePermissions.CanAccess(roleId, "Reports"))
            return new ReportAccess(false, null, Forbid());

        var branchId = GetBranchId() ?? GetRequestedBranchId();
        if (roleId == SalonRoles.Ids.BranchManager && !branchId.HasValue)
            return new ReportAccess(false, null, Forbid());

        return new ReportAccess(true, branchId, null);
    }

    private int? GetRequestedBranchId()
    {
        var raw = Request.Query["branchId"].FirstOrDefault();
        return int.TryParse(raw, out var id) && id > 0 ? id : null;
    }

    private readonly record struct ReportAccess(bool IsAllowed, int? BranchId, ActionResult? ErrorResult);

    private static readonly HashSet<string> AllowedReportKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "kpis",
        "sales",
        "staff",
        "stock",
        "finance",
        "clients",
        "branches"
    };

    private static string NormalizeReportKey(string? report)
    {
        var key = (report ?? "").Trim().ToLowerInvariant();
        return key switch
        {
            "kpi" or "overview" => "kpis",
            "sale" => "sales",
            "personnel" => "staff",
            "client" or "customers" => "clients",
            "branch" or "branch-comparison" => "branches",
            _ => key
        };
    }

    private static DateTime NormalizeScheduledAtUtc(DateTime? scheduledAt)
    {
        if (!scheduledAt.HasValue) return DateTime.UtcNow;
        var value = scheduledAt.Value;
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}

public class SlnReportEmailRequest
{
    public string Report { get; set; } = "sales";
    public string Format { get; set; } = "pdf";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<string>? ToAddresses { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public int? IntegrationId { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
}
