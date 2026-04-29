namespace CallCenter.Shared.Auth;

/// <summary>
/// JWT oturum çerezinin adı. Aynı host (localhost) altında farklı portlarda çalışan uygulamalar
/// <b>farklı</b> <see cref="CookieName"/> kullanmalı; aksi halde tarayıcı tek çerez tutar ve girişler birbirini ezer.
/// </summary>
public class JwtAuthCookieOptions
{
    /// <summary>Eski paylaşımlı ad; geriye dönük okuma için.</summary>
    public const string FallbackSharedCookieName = "AuthToken";

    /// <summary>Management: CorpLynk.Mgmt.Auth — Salon: CorpLynk.Salon.Auth</summary>
    public string CookieName { get; set; } = FallbackSharedCookieName;
}
