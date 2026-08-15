using System.Globalization;

namespace EmpPortal.Application.Localization;

public static class PersianDateTimeFormatter
{
    private static readonly char[] EnglishDigits = "0123456789".ToCharArray();
    private static readonly char[] PersianDigits = "۰۱۲۳۴۵۶۷۸۹".ToCharArray();

    public static readonly string[] MonthNames =
    [
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    ];

    public static string FormatDate(DateTimeOffset? value, bool usePersianDigits = true) =>
        value.HasValue ? FormatDate(value.Value.LocalDateTime, usePersianDigits) : "—";

    public static string FormatDate(DateTime value, bool usePersianDigits = true)
    {
        PersianCalendar calendar = new();
        string formatted = string.Create(
            CultureInfo.InvariantCulture,
            $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00}");
        return usePersianDigits ? ToPersianDigits(formatted) : formatted;
    }

    public static string FormatDateTime(DateTimeOffset? value, bool usePersianDigits = true) =>
        value.HasValue ? FormatDateTime(value.Value, usePersianDigits) : "—";

    public static string FormatDateTime(DateTimeOffset value, bool usePersianDigits = true)
    {
        DateTime local = value.LocalDateTime;
        string formatted = $"{FormatDate(local, usePersianDigits: false)} {local:HH:mm}";
        return usePersianDigits ? ToPersianDigits(formatted) : formatted;
    }

    public static string FormatTime(TimeOnly value, bool usePersianDigits = true)
    {
        string formatted = value.ToString("HH:mm", CultureInfo.InvariantCulture);
        return usePersianDigits ? ToPersianDigits(formatted) : formatted;
    }

    public static string FormatClock(DateTimeOffset? value, bool usePersianDigits = true) =>
        value.HasValue ? FormatClock(value.Value.LocalDateTime, usePersianDigits) : "—";

    public static string FormatClock(DateTime value, bool usePersianDigits = true)
    {
        string formatted = value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return usePersianDigits ? ToPersianDigits(formatted) : formatted;
    }

    public static PersianDateParts GetParts(DateTime dateTime)
    {
        PersianCalendar calendar = new();
        return new PersianDateParts(
            calendar.GetYear(dateTime),
            calendar.GetMonth(dateTime),
            calendar.GetDayOfMonth(dateTime),
            dateTime.Hour,
            dateTime.Minute);
    }

    public static int GetCurrentYear() => new PersianCalendar().GetYear(DateTime.Now);

    public static int GetDaysInMonth(int year, int month) =>
        new PersianCalendar().GetDaysInMonth(year, month);

    public static bool TryCreateLocalDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        out DateTimeOffset value)
    {
        try
        {
            DateTime localDateTime = DateTime.SpecifyKind(
                new PersianCalendar().ToDateTime(year, month, day, hour, minute, 0, 0),
                DateTimeKind.Unspecified);
            value = new DateTimeOffset(
                localDateTime,
                TimeZoneInfo.Local.GetUtcOffset(localDateTime));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = default;
            return false;
        }
    }

    public static bool TryParseStoredDateTime(string? value, out DateTimeOffset parsed)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out parsed))
        {
            return true;
        }

        if (DateOnly.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            DateTime localDateTime = DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Unspecified);
            parsed = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
            return true;
        }

        return false;
    }

    public static bool TryParseStoredTime(string? value, out TimeOnly parsed) =>
        TimeOnly.TryParseExact(
            NormalizeDigits(value),
            ["H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsed);

    public static string ToPersianDigits(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Create(value.Length, value, static (span, source) =>
        {
            for (int index = 0; index < source.Length; index++)
            {
                int digitIndex = Array.IndexOf(EnglishDigits, source[index]);
                span[index] = digitIndex >= 0 ? PersianDigits[digitIndex] : source[index];
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
            int persianIndex = Array.IndexOf(PersianDigits, normalized[index]);
            if (persianIndex >= 0)
            {
                normalized[index] = EnglishDigits[persianIndex];
                continue;
            }

            if (normalized[index] is >= '٠' and <= '٩')
            {
                normalized[index] = (char)('0' + normalized[index] - '٠');
            }
        }

        return new string(normalized);
    }
}

public readonly record struct PersianDateParts(
    int Year,
    int Month,
    int Day,
    int Hour,
    int Minute);
