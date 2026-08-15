using System.Text;

namespace EmpPortal.Application.Hr;

public readonly record struct IranianLicensePlateParts(
    string Series,
    string Letter,
    string Number,
    string Province);

public static class IranianLicensePlate
{
    public static readonly IReadOnlyList<string> Letters =
    [
        "الف", "ب", "پ", "ت", "ث", "ج", "د", "ز", "ژ",
        "س", "ش", "ص", "ط", "ع", "ف", "ق", "ک", "گ",
        "ل", "م", "ن", "و", "ه", "ی"
    ];

    public static string Canonical(string series, string letter, string number, string province) =>
        $"{series}{letter}{number}-{province}";

    public static string Normalize(string? raw)
    {
        if (!TryParse(raw, out IranianLicensePlateParts parts))
        {
            throw new ArgumentException("پلاک خودرو را به‌صورت کامل و معتبر وارد کنید.", nameof(raw));
        }

        return Canonical(parts.Series, parts.Letter, parts.Number, parts.Province);
    }

    public static bool TryParse(string? raw, out IranianLicensePlateParts parts)
    {
        parts = default;
        string text = Compact(raw);
        if (text.Length < 8)
        {
            return false;
        }

        string province = text[^2..];
        string number = text[^5..^2];
        string seriesAndLetter = text[..^5];
        if (seriesAndLetter.Length < 3 ||
            !IsDigits(province) ||
            !IsDigits(number))
        {
            return false;
        }

        string series = seriesAndLetter[..2];
        string letter = seriesAndLetter[2..];
        if (!IsDigits(series) || !IsKnownLetter(letter))
        {
            return false;
        }

        parts = new IranianLicensePlateParts(series, letter, number, province);
        return true;
    }

    public static string FormatDisplay(string? raw, bool usePersianDigits = true)
    {
        if (!TryParse(raw, out IranianLicensePlateParts parts))
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim();
        }

        string formatted = $"{parts.Series} {parts.Letter} {parts.Number} — {parts.Province}";
        return usePersianDigits ? ToPersianDigits(formatted) : formatted;
    }

    public static string ToPersianDigits(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Create(value.Length, value, static (span, source) =>
        {
            for (int index = 0; index < source.Length; index++)
            {
                char character = source[index];
                span[index] = character is >= '0' and <= '9'
                    ? (char)('۰' + (character - '0'))
                    : character;
            }
        });
    }

    public static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        char[] normalized = value.ToCharArray();
        for (int index = 0; index < normalized.Length; index++)
        {
            char character = normalized[index];
            if (character is >= '۰' and <= '۹')
            {
                normalized[index] = (char)('0' + character - '۰');
            }
            else if (character is >= '٠' and <= '٩')
            {
                normalized[index] = (char)('0' + character - '٠');
            }
        }

        return new string(normalized);
    }

    public static string KeepDigits(string? value, int maxLength)
    {
        string digits = new(NormalizeDigits(value).Where(character => character is >= '0' and <= '9').ToArray());
        return digits.Length <= maxLength ? digits : digits[..maxLength];
    }

    private static bool IsKnownLetter(string letter) =>
        Letters.Any(candidate => string.Equals(candidate, letter, StringComparison.Ordinal));

    private static bool IsDigits(string value) =>
        value.Length > 0 && value.All(character => character is >= '0' and <= '9');

    private static string Compact(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        string normalized = NormalizeDigits(raw)
            .Replace("هـ", "ه", StringComparison.Ordinal)
            .Replace("ایران", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IRAN", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("I.R.", string.Empty, StringComparison.OrdinalIgnoreCase);

        StringBuilder builder = new(normalized.Length);
        foreach (char character in normalized)
        {
            if (character is '-' or ' ' or '|' or '_' or '.' or '\u200c' or '\u200f' or '\u200e')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
