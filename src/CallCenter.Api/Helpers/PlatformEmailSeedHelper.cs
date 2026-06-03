using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Helpers;

/// <summary>
/// Platform email event ve sablonlarini idempotent olarak ekler.
/// Startup'ta cagrilir (Development veya AUTO_MIGRATE).
/// </summary>
public static class PlatformEmailSeedHelper
{
    public const string EventUserEmailVerify = "user_email_verify";
    public const string EventUserPasswordReset = "user_password_reset";
    public const string EventPlatformUserEmailVerify = "platform_user_email_verify";
    public const string EventPlatformUserPasswordReset = "platform_user_password_reset";

    public static async Task SeedAsync(AppDbContext db)
    {
        await EnsureEventAsync(db, EventUserEmailVerify,
            description: "Kullanıcı e-posta doğrulama maili",
            placeholders: "[\"FullName\",\"VerifyUrl\"]",
            templates: new[]
            {
                ("tr", "CorpLynk hesabını doğrula", BuildVerifyHtmlTr()),
                ("en", "Verify your CorpLynk account", BuildVerifyHtmlEn())
            });

        await EnsureEventAsync(db, EventUserPasswordReset,
            description: "Şifre sıfırlama maili",
            placeholders: "[\"FullName\",\"ResetUrl\"]",
            templates: new[]
            {
                ("tr", "CorpLynk şifre sıfırlama", BuildResetHtmlTr()),
                ("en", "Reset your CorpLynk password", BuildResetHtmlEn())
            });

        await EnsureEventAsync(db, EventPlatformUserEmailVerify,
            description: "Salon müşterisi (PlatformUser) e-posta doğrulama maili",
            placeholders: "[\"FullName\",\"VerifyUrl\"]",
            templates: new[]
            {
                ("tr", "CorpLynk hesabını doğrula", BuildVerifyHtmlTr()),
                ("en", "Verify your CorpLynk account", BuildVerifyHtmlEn())
            });

        await EnsureEventAsync(db, EventPlatformUserPasswordReset,
            description: "Salon müşterisi (PlatformUser) şifre sıfırlama maili",
            placeholders: "[\"FullName\",\"ResetUrl\"]",
            templates: new[]
            {
                ("tr", "CorpLynk şifre sıfırlama", BuildResetHtmlTr()),
                ("en", "Reset your CorpLynk password", BuildResetHtmlEn())
            });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureEventAsync(
        AppDbContext db,
        string eventKey,
        string description,
        string placeholders,
        (string Lang, string Subject, string Html)[] templates)
    {
        var evt = await db.PlatformEmailEvents
            .Include(e => e.Templates)
            .FirstOrDefaultAsync(e => e.EventKey == eventKey);

        if (evt == null)
        {
            evt = new PlatformEmailEvent
            {
                EventKey = eventKey,
                Description = description,
                AvailablePlaceholders = placeholders,
                IsActive = true
            };
            db.PlatformEmailEvents.Add(evt);
            await db.SaveChangesAsync();
        }
        else if (ShouldRefreshSeedText(eventKey, evt.Description, description))
        {
            evt.Description = description;
            evt.AvailablePlaceholders = placeholders;
            evt.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var (lang, subject, html) in templates)
        {
            var existing = evt.Templates.FirstOrDefault(t => t.Language == lang);
            if (existing != null) continue;

            db.PlatformEmailTemplates.Add(new PlatformEmailTemplate
            {
                EventId = evt.Id,
                Language = lang,
                Subject = subject,
                HtmlBody = html,
                IsActive = true
            });
        }
    }

    private static bool ShouldRefreshSeedText(string eventKey, string? currentDescription, string newDescription)
    {
        if (currentDescription == newDescription) return false;
        if (string.IsNullOrWhiteSpace(currentDescription)) return true;

        var legacyDescriptions = eventKey switch
        {
            EventUserEmailVerify => new[] { "Kullanici email dogrulama maili" },
            EventUserPasswordReset => new[] { "Sifre sifirlama maili" },
            EventPlatformUserEmailVerify => new[] { "Salon musteri (PlatformUser) email dogrulama maili" },
            EventPlatformUserPasswordReset => new[] { "Salon musteri (PlatformUser) sifre sifirlama maili" },
            _ => Array.Empty<string>()
        };

        return legacyDescriptions.Contains(currentDescription);
    }

    private static string BuildVerifyHtmlTr() => """
        <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
            <h2 style="color:#7b1fa2;">Hoş geldin, {{FullName}}</h2>
            <p>CorpLynk hesabını aktif etmek için email adresini doğrula.</p>
            <p style="margin:24px 0;">
                <a href="{{VerifyUrl}}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Email'i doğrula</a>
            </p>
            <p style="color:#6c757d;font-size:13px;">Düğme çalışmıyorsa bu bağlantıyı kopyala:<br/><span style="word-break:break-all;">{{VerifyUrl}}</span></p>
        </div>
        """;

    private static string BuildVerifyHtmlEn() => """
        <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
            <h2 style="color:#7b1fa2;">Welcome, {{FullName}}</h2>
            <p>Please verify your email address to activate your CorpLynk account.</p>
            <p style="margin:24px 0;">
                <a href="{{VerifyUrl}}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Verify email</a>
            </p>
            <p style="color:#6c757d;font-size:13px;">If the button does not work, copy this link:<br/><span style="word-break:break-all;">{{VerifyUrl}}</span></p>
        </div>
        """;

    private static string BuildResetHtmlTr() => """
        <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
            <h2 style="color:#7b1fa2;">Şifre sıfırlama</h2>
            <p>Merhaba {{FullName}}, yeni şifre belirlemek için butona tıkla. Bağlantı 1 saat geçerli.</p>
            <p style="margin:24px 0;">
                <a href="{{ResetUrl}}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Şifreyi sıfırla</a>
            </p>
            <p style="color:#6c757d;font-size:13px;">Bu işlemi sen başlatmadıysan emaili görmezden gelebilirsin.</p>
        </div>
        """;

    private static string BuildResetHtmlEn() => """
        <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
            <h2 style="color:#7b1fa2;">Password reset</h2>
            <p>Hello {{FullName}}, click the button below to set a new password. The link is valid for 1 hour.</p>
            <p style="margin:24px 0;">
                <a href="{{ResetUrl}}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Reset password</a>
            </p>
            <p style="color:#6c757d;font-size:13px;">If you did not request this, you can ignore this email.</p>
        </div>
        """;
}
