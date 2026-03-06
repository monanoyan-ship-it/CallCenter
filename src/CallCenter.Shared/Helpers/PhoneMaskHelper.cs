namespace CallCenter.Shared.Helpers;

public static class PhoneMaskHelper
{
    /// <summary>Telefon numarasini maskeler: 0532***4567</summary>
    public static string Mask(string? number)
    {
        if (string.IsNullOrEmpty(number) || number.Length < 7) return "***";
        return number[..3] + new string('*', number.Length - 6) + number[^3..];
    }
}
