namespace CallCenter.Shared.Helpers;

/// <summary>
/// Telefon numarasi temizleme ve normalizasyon.
/// Bosluk, tire, parantez gibi karakterleri siler.
/// </summary>
public static class PhoneHelper
{
    /// <summary>
    /// Numarayi temizler: bosluk, tire, parantez, nokta kaldirilir.
    /// Sadece rakamlar, +, * ve # kalir.
    /// </summary>
    public static string Sanitize(string? number)
    {
        if (string.IsNullOrEmpty(number)) return string.Empty;

        // Sadece gecerli SIP/PSTN karakterlerini birak
        var span = number.AsSpan();
        var result = new char[span.Length];
        int pos = 0;

        for (int i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c is (>= '0' and <= '9') or '+' or '*' or '#')
            {
                result[pos++] = c;
            }
        }

        return new string(result, 0, pos);
    }

    /// <summary>
    /// Telefon numarasini normalize et: "+905XXXXXXXXX" formatina cevirir.
    /// "+90 532 123 45 67" -> "+905321234567"
    /// "0532 123 45 67"    -> "+905321234567"
    /// "532 123 4567"      -> "+905321234567"
    /// </summary>
    public static string? Normalize(string? phone, string defaultCountryCode = "+90")
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var cleaned = Sanitize(phone);
        if (string.IsNullOrEmpty(cleaned)) return null;

        // + ile basliyorsa ulke kodu var — zaten normalize
        if (cleaned.StartsWith('+'))
            return cleaned;

        // 00 ile basliyorsa uluslararasi format
        if (cleaned.StartsWith("00"))
            return "+" + cleaned[2..];

        // 0 ile basliyorsa yerel format — default ulke kodu ekle
        if (cleaned.StartsWith('0'))
            return defaultCountryCode + cleaned[1..];

        // Hicbir prefix yok — default ulke kodu ekle
        return defaultCountryCode + cleaned;
    }

    /// <summary>Telefon numarasi gecerli mi? (en az 7 rakam)</summary>
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var sanitized = Sanitize(phone);
        var digitCount = sanitized.Count(c => c is >= '0' and <= '9');
        return digitCount >= 7;
    }
}
