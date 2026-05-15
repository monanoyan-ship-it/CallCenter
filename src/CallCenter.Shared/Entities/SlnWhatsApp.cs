namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon bazli WhatsApp Business API ayarlari
/// </summary>
public class SlnWhatsAppConfig
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Meta Business hesap ID</summary>
    public string? BusinessAccountId { get; set; }
    /// <summary>Telefon numarasi ID (WABA icinde)</summary>
    public string? PhoneNumberId { get; set; }
    /// <summary>Access Token (kalici)</summary>
    public string? AccessToken { get; set; }
    /// <summary>Webhook verify token</summary>
    public string? WebhookVerifyToken { get; set; }

    public bool IsActive { get; set; }
    public bool SendAppointmentReminder { get; set; } = true;
    public bool SendAppointmentConfirmation { get; set; } = true;
    public bool SendBirthdayGreeting { get; set; } = true;

    /// <summary>Hatilatma kac saat once gonderilsin</summary>
    public int ReminderHoursBefore { get; set; } = 24;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// WhatsApp mesaj loglari
/// </summary>
public class SlnWhatsAppMessage
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    /// <summary>Gonderim yonu: 1=Giden, 2=Gelen</summary>
    public int DirectionId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public string? MessageBody { get; set; }

    /// <summary>1=Gonderildi, 2=Teslim Edildi, 3=Okundu, 4=Basarisiz</summary>
    public int StatusId { get; set; } = 1;
    public string? WhatsAppMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public int? SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
