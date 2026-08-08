using EmpPortal.Application.Localization;

namespace EmpPortal.Application.UnitTests;

public sealed class PersianDateTimeFormatterTests
{
    [Fact]
    public void FormatsNowruzAsPersianDate()
    {
        DateTimeOffset value = new(2026, 3, 21, 9, 5, 0, TimeSpan.Zero);

        string result = PersianDateTimeFormatter.FormatDate(value, usePersianDigits: false);

        Assert.Equal("1405/01/01", result);
    }

    [Fact]
    public void CreatesGregorianValueFromPersianParts()
    {
        bool succeeded = PersianDateTimeFormatter.TryCreateLocalDateTime(
            1405,
            1,
            1,
            12,
            30,
            out DateTimeOffset result);

        Assert.True(succeeded);
        Assert.Equal(new DateTime(2026, 3, 21), result.Date);
        Assert.Equal(12, result.Hour);
        Assert.Equal(30, result.Minute);
    }

    [Fact]
    public void NormalizesPersianAndArabicDigits()
    {
        Assert.Equal("1405/01/01 12:30", PersianDateTimeFormatter.NormalizeDigits("۱۴۰۵/۰۱/۰۱ ١٢:٣٠"));
    }
}
