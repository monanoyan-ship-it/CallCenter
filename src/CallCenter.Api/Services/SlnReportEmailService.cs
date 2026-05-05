using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services.Email;
using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services;

public class SlnReportEmailJob
{
    public Guid Uid { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }
    public int? BranchId { get; set; }
    public int? IntegrationId { get; set; }
    public string Report { get; set; } = "sales";
    public string Format { get; set; } = "pdf";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public DateTime ScheduledAtUtc { get; set; } = DateTime.UtcNow;
    public List<string> ToAddresses { get; set; } = new();
    public string? Subject { get; set; }
    public string? Message { get; set; }
}

public class SlnReportEmailQueue
{
    private readonly List<SlnReportEmailJob> _jobs = new();
    private readonly object _lock = new();

    public void Enqueue(SlnReportEmailJob job)
    {
        lock (_lock)
            _jobs.Add(job);
    }

    public List<SlnReportEmailJob> TakeDue(DateTime utcNow)
    {
        lock (_lock)
        {
            var due = _jobs.Where(j => j.ScheduledAtUtc <= utcNow).OrderBy(j => j.ScheduledAtUtc).ToList();
            foreach (var job in due)
                _jobs.Remove(job);
            return due;
        }
    }
}

public class SlnReportEmailService
{
    private readonly ISlnReportFactory _reportFactory;
    private readonly IEmailSendService _emailSend;

    public SlnReportEmailService(ISlnReportFactory reportFactory, IEmailSendService emailSend)
    {
        _reportFactory = reportFactory;
        _emailSend = emailSend;
    }

    public async Task<(int Sent, List<string> Errors)> SendAsync(SlnReportEmailJob job, CancellationToken ct = default)
    {
        var (bytes, fileName, contentType) = await BuildReportAttachmentAsync(job);
        var errors = new List<string>();
        var subject = string.IsNullOrWhiteSpace(job.Subject)
            ? $"CorpLynk Salon {ReportDisplayName(job.Report)} raporu"
            : job.Subject.Trim();
        var html = BuildMailHtml(job);
        var sent = 0;

        foreach (var address in job.ToAddresses.Select(a => a.Trim()).Where(a => a.Contains('@')).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var result = await _emailSend.SendAsync(new EmailSendRequest
            {
                CustomerId = job.CustomerId,
                IntegrationId = job.IntegrationId,
                ToAddress = address,
                Subject = subject,
                HtmlBody = html,
                PlainTextBody = $"{ReportDisplayName(job.Report)} raporu ekte yer alir.",
                Attachments =
                [
                    new EmailAttachmentDto
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        Content = bytes
                    }
                ]
            }, ct);

            if (result.Success) sent++;
            else errors.Add($"{address}: {result.Error ?? "Gonderilemedi"}");
        }

        return (sent, errors);
    }

    private async Task<(byte[] Bytes, string FileName, string ContentType)> BuildReportAttachmentAsync(SlnReportEmailJob job)
    {
        var report = NormalizeReportKey(job.Report);
        var format = NormalizeFormat(job.Format);
        var bytes = format switch
        {
            "csv" => await _reportFactory.ExportSalonReportCsvAsync(job.CustomerId, report, job.From, job.To, job.BranchId),
            "xlsx" => await _reportFactory.ExportSalonReportExcelAsync(job.CustomerId, report, job.From, job.To, job.BranchId),
            _ => await _reportFactory.ExportSalonReportPdfAsync(job.CustomerId, report, job.From, job.To, job.BranchId)
        };

        var contentType = format switch
        {
            "csv" => "text/csv; charset=utf-8",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/pdf"
        };
        var fileName = $"salon-{report}-raporu-{DateTime.UtcNow:yyyyMMdd}.{format}";
        return (bytes, fileName, contentType);
    }

    private static string BuildMailHtml(SlnReportEmailJob job)
    {
        var message = string.IsNullOrWhiteSpace(job.Message)
            ? "Istediginiz salon raporu ekte yer alir."
            : System.Net.WebUtility.HtmlEncode(job.Message.Trim());

        return $@"<p>Merhaba,</p>
<p>{message}</p>
<p><strong>Rapor:</strong> {System.Net.WebUtility.HtmlEncode(ReportDisplayName(job.Report))}<br/>
<strong>Donem:</strong> {job.From:dd.MM.yyyy} - {job.To:dd.MM.yyyy}<br/>
<strong>Format:</strong> {System.Net.WebUtility.HtmlEncode(NormalizeFormat(job.Format).ToUpperInvariant())}</p>
<p>CorpLynk Salon</p>";
    }

    private static string NormalizeReportKey(string report)
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

    private static string NormalizeFormat(string format)
    {
        var value = (format ?? "pdf").Trim().ToLowerInvariant();
        return value is "xlsx" or "excel" ? "xlsx" : value is "csv" ? "csv" : "pdf";
    }

    private static string ReportDisplayName(string reportKey)
        => NormalizeReportKey(reportKey) switch
        {
            "kpis" => "KPI",
            "sales" => "Satis",
            "staff" => "Personel",
            "stock" => "Stok",
            "finance" => "Finans",
            "clients" => "Musteri",
            "branches" => "Sube Karsilastirma",
            _ => reportKey
        };
}

public class SlnReportEmailSchedulerService : BackgroundService
{
    private readonly SlnReportEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlnReportEmailSchedulerService> _logger;

    public SlnReportEmailSchedulerService(
        SlnReportEmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<SlnReportEmailSchedulerService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var job in _queue.TakeDue(DateTime.UtcNow))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<SlnReportEmailService>();
                    var result = await sender.SendAsync(job, stoppingToken);
                    if (result.Errors.Count > 0)
                        _logger.LogWarning("Zamanlanmis rapor e-postasi kismi basarisiz. Job={JobUid}, Sent={Sent}, Errors={Errors}",
                            job.Uid, result.Sent, string.Join("; ", result.Errors));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Zamanlanmis rapor e-postasi gonderilemedi. Job={JobUid}", job.Uid);
                }
            }
        }
    }
}
