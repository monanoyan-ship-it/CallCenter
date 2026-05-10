namespace CallCenter.Shared.Helpers;

/// <summary>
/// Telefon numarasi temizleme ve normalizasyon.
/// Bosluk, tire, parantez gibi karakterleri siler.
/// </summary>
public static class PhoneHelper
{
    /// <summary>
    /// Numarayi temizler. Sadece rakamlar, +, * ve # kalir.
    /// </summary>
    public static string Sanitize(string? number)
    {
        if (string.IsNullOrEmpty(number)) return string.Empty;

        var span = number.AsSpan();
        var result = new char[span.Length];
        var pos = 0;

        for (var i = 0; i < span.Length; i++)
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
    /// Telefon numarasini normalize eder.
    /// "+90 532 123 45 67" -> "+905321234567"
    /// "0532 123 45 67"    -> "+905321234567"
    /// "532 123 4567"      -> "+905321234567"
    /// "905321234567"      -> "+905321234567"
    /// </summary>
    public static string? Normalize(string? phone, string defaultCountryCode = "+90")
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var cleaned = Sanitize(phone);
        if (string.IsNullOrEmpty(cleaned)) return null;

        var digits = DigitsOnly(cleaned);
        if (string.IsNullOrEmpty(digits)) return null;

        if (cleaned.StartsWith('+'))
            return "+" + digits;

        if (cleaned.StartsWith("00"))
            return digits.Length > 2 ? "+" + digits[2..] : null;

        var countryDigits = DigitsOnly(defaultCountryCode);
        if (string.IsNullOrEmpty(countryDigits)) countryDigits = "90";

        if (cleaned.StartsWith('0'))
            return digits.Length > 1 ? "+" + countryDigits + digits[1..] : null;

        if (digits.StartsWith(countryDigits, StringComparison.Ordinal) && digits.Length > countryDigits.Length)
            return "+" + digits;

        return "+" + countryDigits + digits;
    }

    /// <summary>
    /// Eski ve yeni kayitlari ayni sorguda yakalamak icin telefon varyantlarini dondurur.
    /// </summary>
    public static IReadOnlyList<string> GetLookupVariants(string? phone, string defaultCountryCode = "+90")
    {
        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var cleaned = Sanitize(phone);
        if (!string.IsNullOrWhiteSpace(cleaned))
            variants.Add(cleaned);

        var digits = DigitsOnly(cleaned);
        if (!string.IsNullOrWhiteSpace(digits))
            variants.Add(digits);

        var normalized = Normalize(phone, defaultCountryCode);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            variants.Add(normalized);
            variants.Add(normalized.TrimStart('+'));

            var countryDigits = DigitsOnly(defaultCountryCode);
            if (!string.IsNullOrEmpty(countryDigits) && normalized.StartsWith("+" + countryDigits, StringComparison.Ordinal))
            {
                var national = normalized[("+" + countryDigits).Length..];
                if (!string.IsNullOrEmpty(national))
                {
                    variants.Add(national);
                    variants.Add("0" + national);
                }
            }
        }

        return variants.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
    }

    /// <summary>Telefon numarasi gecerli mi? (en az 7 rakam)</summary>
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var sanitized = Sanitize(phone);
        var digitCount = sanitized.Count(c => c is >= '0' and <= '9');
        return digitCount >= 7;
    }

    private static string DigitsOnly(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var result = new char[value.Length];
        var pos = 0;
        foreach (var c in value)
        {
            if (c is >= '0' and <= '9')
                result[pos++] = c;
        }

        return new string(result, 0, pos);
    }
}
