using EmpPortal.Application.Hr;

namespace EmpPortal.Application.UnitTests;

public sealed class IranianLicensePlateTests
{
    [Theory]
    [InlineData("12ب345-22", "12", "ب", "345", "22")]
    [InlineData("۱۲ب۳۴۵۲۲", "12", "ب", "345", "22")]
    [InlineData("12 الف 345 22", "12", "الف", "345", "22")]
    [InlineData("IRAN 21ی811-99", "21", "ی", "811", "99")]
    public void ParsesCanonicalAndLooseFormats(
        string raw,
        string series,
        string letter,
        string number,
        string province)
    {
        Assert.True(IranianLicensePlate.TryParse(raw, out IranianLicensePlateParts parts));
        Assert.Equal(series, parts.Series);
        Assert.Equal(letter, parts.Letter);
        Assert.Equal(number, parts.Number);
        Assert.Equal(province, parts.Province);
        Assert.Equal($"{series}{letter}{number}-{province}", IranianLicensePlate.Normalize(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("12x345-22")]
    [InlineData("12چ345-22")]
    public void RejectsInvalidPlates(string raw)
    {
        Assert.False(IranianLicensePlate.TryParse(raw, out _));
        Assert.Throws<ArgumentException>(() => IranianLicensePlate.Normalize(raw));
    }

    [Fact]
    public void FormatsDisplayWithPersianDigits()
    {
        string display = IranianLicensePlate.FormatDisplay("12ب345-22");

        Assert.Equal("۱۲ ب ۳۴۵ — ۲۲", display);
    }
}
