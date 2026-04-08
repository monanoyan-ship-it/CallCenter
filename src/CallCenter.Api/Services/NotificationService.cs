using CallCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Bildirim servisi. SMS, Email ve Push bildirim gonderimleri.
/// Randevu hatirlatma, puan kazanim, kampanya bildirimleri.
/// </summary>
public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Randevu hatirlatma bildirimi gonder (1 gun once + 1 saat once)</summary>
    public async Task<int> SendAppointmentRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var oneDayLater = now.AddHours(24);
        var oneHourLater = now.AddHours(1);

        // 24 saat icindeki randevular
        var upcoming = await _db.SlnAppointments
            .Where(a => a.StatusId == 1 && a.StartTime >= now && a.StartTime <= oneDayLater)
            .Include(a => a.SlnClient)
            .ToListAsync();

        var sent = 0;
        foreach (var appt in upcoming)
        {
            var phone = appt.SlnClient?.Phone;
            if (string.IsNullOrEmpty(phone)) continue;

            var salon = await _db.Customers.FindAsync(appt.CustomerId);
            var timeStr = appt.StartTime.ToString("HH:mm");
            var dateStr = appt.StartTime.ToString("dd.MM.yyyy");

            // TODO: Gercek SMS/email gonderimi
            _logger.LogInformation("[Bildirim] Randevu hatirlatma: {Phone}, Salon={Salon}, Tarih={Date} {Time}",
                phone, salon?.Name, dateStr, timeStr);
            sent++;
        }

        return sent;
    }

    /// <summary>Puan kazanim bildirimi (adisyon sonrasi)</summary>
    public async Task SendLoyaltyPointsNotificationAsync(int slnClientId, int customerId, decimal pointsEarned)
    {
        var client = await _db.SlnClients.FindAsync(slnClientId);
        if (client == null || string.IsNullOrEmpty(client.Phone)) return;

        var salon = await _db.Customers.FindAsync(customerId);

        // TODO: Gercek SMS gonderimi
        _logger.LogInformation("[Bildirim] Puan kazanim: {Phone}, Salon={Salon}, Puan={Points}",
            client.Phone, salon?.Name, pointsEarned);
    }

    /// <summary>Kampanya bildirimi</summary>
    public async Task<int> SendCampaignNotificationAsync(int campaignId)
    {
        var campaign = await _db.SlnCampaigns.FindAsync(campaignId);
        if (campaign == null) return 0;

        // TODO: Kampanya hedef kitlesi filtrele + SMS/email gonder
        _logger.LogInformation("[Bildirim] Kampanya: Id={Id}, Baslik={Title}",
            campaignId, campaign.Name);

        return 0; // Gonderim sayisi
    }
}
