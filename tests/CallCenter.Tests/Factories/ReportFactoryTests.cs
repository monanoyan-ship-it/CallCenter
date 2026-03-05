using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Tests.Factories;

public class ReportFactoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReportFactory _sut;

    public ReportFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        var callEs = new CallRecordEntityService(_db);
        var personnelEs = new CustomerPersonnelEntityService(_db);
        var userEs = new UserEntityService(_db);
        var queueEs = new QueueEntityService(_db);

        _sut = new ReportFactory(callEs, personnelEs, userEs, queueEs);

        SeedBaseData();
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════════════════════════════
    // SEED DATA
    // ═══════════════════════════════════════════════════════════════

    private void SeedBaseData()
    {
        var customer = new Customer
        {
            Id = 1, Uid = Guid.NewGuid(), Name = "Test Firma",
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);

        var agent1 = new User
        {
            Id = 10, Uid = Guid.NewGuid(), UserName = "agent1",
            FullName = "Ali Yilmaz", Email = "ali@test.local",
            PasswordHash = "$2a$11$test", Extension = "1001",
            RoleId = UserRoles.Ids.Agent, StatusId = AgentStatuses.Ids.Available,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        var agent2 = new User
        {
            Id = 11, Uid = Guid.NewGuid(), UserName = "agent2",
            FullName = "Veli Demir", Email = "veli@test.local",
            PasswordHash = "$2a$11$test", Extension = "1002",
            RoleId = UserRoles.Ids.Agent, StatusId = AgentStatuses.Ids.Available,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.AddRange(agent1, agent2);

        _db.CustomerPersonnel.AddRange(
            new CustomerPersonnel { Id = 1, CustomerId = 1, UserId = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new CustomerPersonnel { Id = 2, CustomerId = 1, UserId = 11, IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        var queue = new Queue
        {
            Id = 1, Uid = Guid.NewGuid(), Name = "Destek",
            CustomerId = 1, MaxWaitTimeSeconds = 60, IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Queues.Add(queue);

        _db.SaveChanges();
    }

    private void SeedCallRecords(params CallRecord[] calls)
    {
        _db.CallRecords.AddRange(calls);
        _db.SaveChanges();
    }

    private static DateTime Utc(int year, int month, int day, int hour = 10, int min = 0)
        => new(year, month, day, hour, min, 0, DateTimeKind.Utc);

    private static CallRecord MakeCall(int agentId, int statusId, DateTime startedAt,
        DateTime? answeredAt = null, DateTime? endedAt = null,
        int duration = 0, int? queueId = null, int directionId = 1)
    {
        return new CallRecord
        {
            Uid = Guid.NewGuid(),
            CallerNumber = "05551112233",
            CalleeNumber = "02121234567",
            DirectionId = directionId,
            StatusId = statusId,
            StartedAt = startedAt,
            AnsweredAt = answeredAt,
            EndedAt = endedAt,
            DurationSeconds = duration,
            AgentId = agentId,
            QueueId = queueId
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // GetCallReportAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCallReport_EmptyDb_ReturnsZeros()
    {
        var result = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 20);

        result.TotalCalls.Should().Be(0);
        result.AnsweredCalls.Should().Be(0);
        result.MissedCalls.Should().Be(0);
        result.AvgDurationSeconds.Should().Be(0);
        result.AbandonmentRate.Should().Be(0);
        result.AvgSpeedOfAnswerSeconds.Should().Be(0);
        result.AvgHandleTimeSeconds.Should().Be(0);
        result.SlaComplianceRate.Should().Be(0);
    }

    [Fact]
    public async Task GetCallReport_WithCalls_ReturnsCorrectCounts()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(10), baseTime.AddSeconds(130), 120, 1),
            MakeCall(10, CallStatuses.Ids.Completed, baseTime.AddHours(1), baseTime.AddHours(1).AddSeconds(5), baseTime.AddHours(1).AddSeconds(65), 60, 1),
            MakeCall(11, CallStatuses.Ids.Missed, baseTime.AddHours(2))
        );

        var result = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 20);

        result.TotalCalls.Should().Be(3);
        result.AnsweredCalls.Should().Be(2);
        result.MissedCalls.Should().Be(1);
        result.AvgDurationSeconds.Should().Be(90); // (120+60)/2
    }

    [Fact]
    public async Task GetCallReport_AbandonmentRate_CalculatedCorrectly()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60, 1),
            MakeCall(10, CallStatuses.Ids.Missed, baseTime.AddHours(1)),
            MakeCall(11, CallStatuses.Ids.Missed, baseTime.AddHours(2)),
            MakeCall(11, CallStatuses.Ids.Missed, baseTime.AddHours(3))
        );

        var result = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 20);

        // 3 missed / 4 total = 75%
        result.AbandonmentRate.Should().Be(75);
    }

    [Fact]
    public async Task GetCallReport_SlaMetrics_CalculatedCorrectly()
    {
        var baseTime = Utc(2026, 3, 1);
        // Queue MaxWaitTimeSeconds = 60
        SeedCallRecords(
            // Cevaplanma: 10sn (SLA icinde, <60sn)
            MakeCall(10, CallStatuses.Ids.Completed, baseTime,
                baseTime.AddSeconds(10), baseTime.AddSeconds(130), 120, queueId: 1),
            // Cevaplanma: 30sn (SLA icinde, <60sn)
            MakeCall(10, CallStatuses.Ids.Completed, baseTime.AddHours(1),
                baseTime.AddHours(1).AddSeconds(30), baseTime.AddHours(1).AddSeconds(90), 60, queueId: 1),
            // Cevaplanma: 90sn (SLA DISI, >60sn)
            MakeCall(11, CallStatuses.Ids.Completed, baseTime.AddHours(2),
                baseTime.AddHours(2).AddSeconds(90), baseTime.AddHours(2).AddSeconds(210), 120, queueId: 1)
        );

        var result = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 20);

        // ASA: (10+30+90)/3 = 43.33 -> 43
        result.AvgSpeedOfAnswerSeconds.Should().BeInRange(43, 44);

        // AHT: (120+60+120)/3 = 100
        result.AvgHandleTimeSeconds.Should().Be(100);

        // SLA: 2/3 icinde = 66.7%
        result.SlaComplianceRate.Should().BeApproximately(66.7, 0.1);
    }

    [Fact]
    public async Task GetCallReport_FilterByCustomer_ReturnsOnlyCustomerCalls()
    {
        // Customer 1 icin agent10 ve agent11 var
        // Customer 2 icin agent yoktur -> 0 sonuc
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var result = await _sut.GetCallReportAsync(1, null, null, null, null, 1, 20);
        result.TotalCalls.Should().Be(1);

        // Olmayan musteri
        var result2 = await _sut.GetCallReportAsync(999, null, null, null, null, 1, 20);
        result2.TotalCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetCallReport_FilterByDateRange_ReturnsCorrectRange()
    {
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, Utc(2026, 2, 28), Utc(2026, 2, 28, 10, 5), Utc(2026, 2, 28, 10, 10), 300),
            MakeCall(10, CallStatuses.Ids.Completed, Utc(2026, 3, 1), Utc(2026, 3, 1, 10, 5), Utc(2026, 3, 1, 10, 10), 300),
            MakeCall(11, CallStatuses.Ids.Completed, Utc(2026, 3, 2), Utc(2026, 3, 2, 10, 5), Utc(2026, 3, 2, 10, 10), 300),
            MakeCall(11, CallStatuses.Ids.Completed, Utc(2026, 3, 3), Utc(2026, 3, 3, 10, 5), Utc(2026, 3, 3, 10, 10), 300)
        );

        // 1-2 Mart arasi
        var result = await _sut.GetCallReportAsync(null, new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), null, null, 1, 20);

        result.TotalCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetCallReport_FilterByDirection_ReturnsCorrectDirection()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60,
                directionId: CallDirections.Ids.Inbound),
            MakeCall(10, CallStatuses.Ids.Completed, baseTime.AddHours(1), baseTime.AddHours(1).AddSeconds(5), baseTime.AddHours(1).AddSeconds(65), 60,
                directionId: CallDirections.Ids.Outbound)
        );

        var inbound = await _sut.GetCallReportAsync(null, null, null, CallDirections.Ids.Inbound, null, 1, 20);
        inbound.TotalCalls.Should().Be(1);

        var outbound = await _sut.GetCallReportAsync(null, null, null, CallDirections.Ids.Outbound, null, 1, 20);
        outbound.TotalCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetCallReport_Pagination_WorksCorrectly()
    {
        var baseTime = Utc(2026, 3, 1);
        for (int i = 0; i < 5; i++)
            SeedCallRecords(MakeCall(10, CallStatuses.Ids.Completed,
                baseTime.AddHours(i), baseTime.AddHours(i).AddSeconds(5),
                baseTime.AddHours(i).AddSeconds(65), 60));

        var page1 = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 2);
        page1.Items.Items.Should().HaveCount(2);
        page1.Items.TotalCount.Should().Be(5);
        page1.Items.Page.Should().Be(1);
        page1.Items.PageSize.Should().Be(2);

        var page3 = await _sut.GetCallReportAsync(null, null, null, null, null, 3, 2);
        page3.Items.Items.Should().HaveCount(1); // son sayfa, 1 kayit
    }

    // ═══════════════════════════════════════════════════════════════
    // GetAgentReportAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAgentReport_WithCalls_ReturnsAgentStats()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60),
            MakeCall(10, CallStatuses.Ids.Completed, baseTime.AddHours(1), baseTime.AddHours(1).AddSeconds(5), baseTime.AddHours(1).AddSeconds(125), 120),
            MakeCall(10, CallStatuses.Ids.Missed, baseTime.AddHours(2)),
            MakeCall(11, CallStatuses.Ids.Completed, baseTime.AddHours(3), baseTime.AddHours(3).AddSeconds(5), baseTime.AddHours(3).AddSeconds(185), 180)
        );

        var result = await _sut.GetAgentReportAsync(null, null, null, 1, 20);

        result.TotalAgents.Should().Be(2);
        result.Items.Items.Should().HaveCount(2);

        var ali = result.Items.Items.First(a => a.FullName == "Ali Yilmaz");
        ali.TotalCalls.Should().Be(3);
        ali.AnsweredCalls.Should().Be(2);
        ali.MissedCalls.Should().Be(1);
        ali.AvgDurationSeconds.Should().Be(90); // (60+120)/2
        ali.AnswerRate.Should().BeApproximately(66.7, 0.1); // 2/3

        var veli = result.Items.Items.First(a => a.FullName == "Veli Demir");
        veli.TotalCalls.Should().Be(1);
        veli.AnsweredCalls.Should().Be(1);
        veli.AnswerRate.Should().Be(100);
    }

    [Fact]
    public async Task GetAgentReport_BestPerformer_IsCorrect()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60),
            MakeCall(10, CallStatuses.Ids.Missed, baseTime.AddHours(1)),
            MakeCall(11, CallStatuses.Ids.Completed, baseTime.AddHours(2), baseTime.AddHours(2).AddSeconds(5), baseTime.AddHours(2).AddSeconds(65), 60)
        );

        var result = await _sut.GetAgentReportAsync(null, null, null, 1, 20);

        // Veli %100 cevaplama, Ali %50 -> Veli best performer
        result.BestPerformerName.Should().Be("Veli Demir");
    }

    [Fact]
    public async Task GetAgentReport_FilterByCustomer_ReturnsOnlyCustomerAgents()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var result = await _sut.GetAgentReportAsync(1, null, null, 1, 20);
        result.TotalAgents.Should().Be(2); // customer 1 icin 2 agent

        var result2 = await _sut.GetAgentReportAsync(999, null, null, 1, 20);
        result2.TotalAgents.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════
    // GetQueueReportAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetQueueReport_WithQueueCalls_ReturnsCorrectStats()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            // SLA icinde (10sn < 60sn)
            MakeCall(10, CallStatuses.Ids.Completed, baseTime,
                baseTime.AddSeconds(10), baseTime.AddSeconds(130), 120, queueId: 1),
            // SLA icinde (20sn < 60sn)
            MakeCall(11, CallStatuses.Ids.Completed, baseTime.AddHours(1),
                baseTime.AddHours(1).AddSeconds(20), baseTime.AddHours(1).AddSeconds(80), 60, queueId: 1),
            // Cevapsiz
            MakeCall(10, CallStatuses.Ids.Missed, baseTime.AddHours(2), queueId: 1)
        );

        var result = await _sut.GetQueueReportAsync(null, null, null, 1, 20);

        result.TotalQueues.Should().Be(1);
        var queue = result.Items.Items.First();
        queue.QueueName.Should().Be("Destek");
        queue.TotalCalls.Should().Be(3);
        queue.AnsweredCalls.Should().Be(2);
        queue.MissedCalls.Should().Be(1);
        queue.SlaTargetSeconds.Should().Be(60);
        queue.SlaComplianceRate.Should().Be(100); // 2/2 icinde
        queue.AbandonmentRate.Should().BeApproximately(33.3, 0.1); // 1/3
        queue.AvgWaitSeconds.Should().Be(15); // (10+20)/2
    }

    [Fact]
    public async Task GetQueueReport_SlaViolation_CalculatedCorrectly()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            // SLA icinde (10sn < 60sn)
            MakeCall(10, CallStatuses.Ids.Completed, baseTime,
                baseTime.AddSeconds(10), baseTime.AddSeconds(70), 60, queueId: 1),
            // SLA DISI (120sn > 60sn)
            MakeCall(11, CallStatuses.Ids.Completed, baseTime.AddHours(1),
                baseTime.AddHours(1).AddSeconds(120), baseTime.AddHours(1).AddSeconds(240), 120, queueId: 1)
        );

        var result = await _sut.GetQueueReportAsync(null, null, null, 1, 20);

        var queue = result.Items.Items.First();
        queue.SlaComplianceRate.Should().Be(50); // 1/2 icinde
    }

    [Fact]
    public async Task GetQueueReport_OverallSla_IsAverageOfQueues()
    {
        // Ikinci kuyruk ekle
        _db.Queues.Add(new Queue
        {
            Id = 2, Uid = Guid.NewGuid(), Name = "Satis",
            CustomerId = 1, MaxWaitTimeSeconds = 30, IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            // Destek: 1/1 SLA icinde = %100
            MakeCall(10, CallStatuses.Ids.Completed, baseTime,
                baseTime.AddSeconds(10), baseTime.AddSeconds(70), 60, queueId: 1),
            // Satis: 0/1 SLA icinde = %0 (40sn > 30sn max)
            MakeCall(11, CallStatuses.Ids.Completed, baseTime.AddHours(1),
                baseTime.AddHours(1).AddSeconds(40), baseTime.AddHours(1).AddSeconds(100), 60, queueId: 2)
        );

        var result = await _sut.GetQueueReportAsync(null, null, null, 1, 20);

        result.TotalQueues.Should().Be(2);
        // Overall SLA: (100 + 0) / 2 = 50
        result.OverallSlaRate.Should().Be(50);
    }

    // ═══════════════════════════════════════════════════════════════
    // GetDailyTrendAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDailyTrend_GroupsByDate()
    {
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, Utc(2026, 3, 1, 9), Utc(2026, 3, 1, 9, 5), Utc(2026, 3, 1, 9, 10), 300),
            MakeCall(10, CallStatuses.Ids.Missed, Utc(2026, 3, 1, 14)),
            MakeCall(11, CallStatuses.Ids.Completed, Utc(2026, 3, 2, 10), Utc(2026, 3, 2, 10, 5), Utc(2026, 3, 2, 10, 10), 300),
            MakeCall(11, CallStatuses.Ids.Completed, Utc(2026, 3, 2, 15), Utc(2026, 3, 2, 15, 5), Utc(2026, 3, 2, 15, 10), 300),
            MakeCall(10, CallStatuses.Ids.Missed, Utc(2026, 3, 2, 16))
        );

        var result = await _sut.GetDailyTrendAsync(null, null, null);

        result.Should().HaveCount(2);

        var day1 = result.First(d => d.Date == new DateTime(2026, 3, 1));
        day1.TotalCalls.Should().Be(2);
        day1.AnsweredCalls.Should().Be(1);
        day1.MissedCalls.Should().Be(1);

        var day2 = result.First(d => d.Date == new DateTime(2026, 3, 2));
        day2.TotalCalls.Should().Be(3);
        day2.AnsweredCalls.Should().Be(2);
        day2.MissedCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetDailyTrend_OrderedByDate()
    {
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, Utc(2026, 3, 5), Utc(2026, 3, 5, 10, 5), Utc(2026, 3, 5, 10, 10), 300),
            MakeCall(10, CallStatuses.Ids.Completed, Utc(2026, 3, 1), Utc(2026, 3, 1, 10, 5), Utc(2026, 3, 1, 10, 10), 300),
            MakeCall(11, CallStatuses.Ids.Completed, Utc(2026, 3, 3), Utc(2026, 3, 3, 10, 5), Utc(2026, 3, 3, 10, 10), 300)
        );

        var result = await _sut.GetDailyTrendAsync(null, null, null);

        result.Should().BeInAscendingOrder(d => d.Date);
    }

    // ═══════════════════════════════════════════════════════════════
    // GetStatusDistributionAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStatusDistribution_GroupsByStatus()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60),
            MakeCall(10, CallStatuses.Ids.Completed, baseTime.AddHours(1), baseTime.AddHours(1).AddSeconds(5), baseTime.AddHours(1).AddSeconds(65), 60),
            MakeCall(11, CallStatuses.Ids.Missed, baseTime.AddHours(2)),
            MakeCall(11, CallStatuses.Ids.Failed, baseTime.AddHours(3))
        );

        var result = await _sut.GetStatusDistributionAsync(null, null, null);

        result.Should().HaveCount(3); // Completed, Missed, Failed

        var completed = result.First(d => d.StatusName == "Completed");
        completed.Count.Should().Be(2);

        var missed = result.First(d => d.StatusName == "Missed");
        missed.Count.Should().Be(1);

        var failed = result.First(d => d.StatusName == "Failed");
        failed.Count.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════
    // ExportCallReportCsvAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportCallReportCsv_ReturnsUtf8BomBytes()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var bytes = await _sut.ExportCallReportCsvAsync(null, null, null, null, null);

        bytes.Should().NotBeEmpty();
        // UTF-8 BOM: 0xEF, 0xBB, 0xBF
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);
    }

    [Fact]
    public async Task ExportCallReportCsv_ContainsHeaderAndData()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var bytes = await _sut.ExportCallReportCsvAsync(null, null, null, null, null);
        var content = System.Text.Encoding.UTF8.GetString(bytes);

        content.Should().Contain("Arayan;Aranan;Yon;Durum;Sure (sn);Temsilci;Kuyruk;Tarih");
        content.Should().Contain("05551112233");
        content.Should().Contain("02121234567");
    }

    [Fact]
    public async Task ExportCallReportCsv_EmptyDb_ReturnsOnlyHeader()
    {
        var bytes = await _sut.ExportCallReportCsvAsync(null, null, null, null, null);
        var content = System.Text.Encoding.UTF8.GetString(bytes);

        var lines = content.Trim().Split('\n');
        lines.Should().HaveCount(1); // sadece header
    }

    // ═══════════════════════════════════════════════════════════════
    // ExportCallReportExcelAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportCallReportExcel_ReturnsValidXlsxBytes()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var bytes = await _sut.ExportCallReportExcelAsync(null, null, null, null, null);

        bytes.Should().NotBeEmpty();
        // XLSX PK header (ZIP format)
        bytes[0].Should().Be(0x50); // P
        bytes[1].Should().Be(0x4B); // K
    }

    // ═══════════════════════════════════════════════════════════════
    // ExportAgentReportCsvAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportAgentReportCsv_ContainsAgentData()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var bytes = await _sut.ExportAgentReportCsvAsync(null, null, null);
        var content = System.Text.Encoding.UTF8.GetString(bytes);

        content.Should().Contain("Temsilci;Dahili;Toplam;Cevaplanan;Cevapsiz");
        content.Should().Contain("Ali Yilmaz");
        content.Should().Contain("1001");
    }

    // ═══════════════════════════════════════════════════════════════
    // ExportAgentReportExcelAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportAgentReportExcel_ReturnsValidXlsxBytes()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60)
        );

        var bytes = await _sut.ExportAgentReportExcelAsync(null, null, null);

        bytes.Should().NotBeEmpty();
        bytes[0].Should().Be(0x50); // P (XLSX = ZIP)
        bytes[1].Should().Be(0x4B); // K
    }

    // ═══════════════════════════════════════════════════════════════
    // Edge Cases
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCallReport_OnlyMissedCalls_SlaIsZero()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Missed, baseTime),
            MakeCall(11, CallStatuses.Ids.Missed, baseTime.AddHours(1))
        );

        var result = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 20);

        result.AbandonmentRate.Should().Be(100);
        result.AvgSpeedOfAnswerSeconds.Should().Be(0);
        result.AvgHandleTimeSeconds.Should().Be(0);
        result.SlaComplianceRate.Should().Be(0);
    }

    [Fact]
    public async Task GetCallReport_AnsweredWithoutEndedAt_AhtHandledGracefully()
    {
        var baseTime = Utc(2026, 3, 1);
        // EndedAt null - devam eden cagri
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime,
                answeredAt: baseTime.AddSeconds(10), endedAt: null, duration: 0, queueId: 1)
        );

        var result = await _sut.GetCallReportAsync(null, null, null, null, null, 1, 20);

        // AHT hesaplanamaz (EndedAt null), default 0
        result.AvgHandleTimeSeconds.Should().Be(0);
        // ASA hesaplanabilir
        result.AvgSpeedOfAnswerSeconds.Should().Be(10);
    }

    [Fact]
    public async Task GetQueueReport_NoCallsInQueue_AllZeros()
    {
        // Kuyruk var ama cagri yok
        var result = await _sut.GetQueueReportAsync(null, null, null, 1, 20);

        result.TotalQueues.Should().Be(1);
        var queue = result.Items.Items.First();
        queue.TotalCalls.Should().Be(0);
        queue.SlaComplianceRate.Should().Be(0);
        queue.AbandonmentRate.Should().Be(0);
    }

    [Fact]
    public async Task GetCallReport_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var baseTime = Utc(2026, 3, 1);
        SeedCallRecords(
            MakeCall(10, CallStatuses.Ids.Completed, baseTime, baseTime.AddSeconds(5), baseTime.AddSeconds(65), 60),
            MakeCall(10, CallStatuses.Ids.Missed, baseTime.AddHours(1)),
            MakeCall(11, CallStatuses.Ids.Failed, baseTime.AddHours(2))
        );

        var completed = await _sut.GetCallReportAsync(null, null, null, null, CallStatuses.Ids.Completed, 1, 20);
        completed.TotalCalls.Should().Be(1);

        var missed = await _sut.GetCallReportAsync(null, null, null, null, CallStatuses.Ids.Missed, 1, 20);
        missed.TotalCalls.Should().Be(1);
    }
}
