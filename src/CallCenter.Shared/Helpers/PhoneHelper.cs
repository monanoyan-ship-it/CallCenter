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
}
