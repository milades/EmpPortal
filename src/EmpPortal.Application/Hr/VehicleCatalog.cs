namespace EmpPortal.Application.Hr;

/// <summary>
/// Standard selectable catalogs for personnel vehicle forms (Iran market).
/// </summary>
public static class VehicleCatalog
{
    public const string CustomValue = "__custom__";
    public const string CustomLabel = "سایر / ثبت دستی…";

    public static IReadOnlyList<string> Types { get; } =
    [
        "سواری",
        "شاسی‌بلند",
        "کراس‌اوور",
        "وانت",
        "ون",
        "مینی‌بوس",
        "اتوبوس",
        "کامیونت",
        "موتورسیکلت"
    ];

    public static IReadOnlyList<string> Colors { get; } =
    [
        "سفید",
        "مشکی",
        "نقره‌ای",
        "خاکستری",
        "زغالی",
        "آبی",
        "آبی متالیک",
        "قرمز",
        "قهوه‌ای",
        "بژ",
        "کرم",
        "سبز",
        "طلایی",
        "برنز"
    ];

    public static IReadOnlyList<VehicleModelOption> Models { get; } =
    [
        new("پژو ۲۰۶", "سواری", ["تیپ ۲", "تیپ ۳", "تیپ ۵", "SD V8"]),
        new("پژو ۲۰۷", "سواری", ["دستی", "اتوماتیک", "پانوراما"]),
        new("پژو ۲۰۷i", "سواری", ["دستی", "اتوماتیک"]),
        new("پژو پارس", "سواری", ["سال", "ELX", "LX", "XU7P"]),
        new("پژو ۴۰۵", "سواری", ["GLX", "SLX"]),
        new("سمند", "سواری", ["LX", "EL", "SE", "سورن"]),
        new("سمند سورن", "سواری", ["ELX", "پلاس"]),
        new("دنا", "سواری", ["معمولی", "پلاس", "پلاس توربو"]),
        new("رانا", "سواری", ["EL", "پلاس"]),
        new("تارا", "سواری", ["دستی", "اتوماتیک", "V1", "V2"]),
        new("شاهین", "سواری", ["G", "GL", "اتوماتیک"]),
        new("کوییک", "سواری", ["دستی", "اتوماتیک", "R", "S"]),
        new("ساینا", "سواری", ["دستی", "اتوماتیک", "S"]),
        new("تیبا", "سواری", ["صندوق‌دار", "هاچ‌بک"]),
        new("پراید", "سواری", ["۱۱۱", "۱۳۱", "۱۳۲"]),
        new("ام‌وی‌ام X22", "کراس‌اوور", ["دستی", "اتوماتیک", "پرو"]),
        new("ام‌وی‌ام X33", "شاسی‌بلند", ["دستی", "اتوماتیک"]),
        new("ام‌وی‌ام X55", "شاسی‌بلند", ["اکسلنت"]),
        new("چری تیگو ۷", "شاسی‌بلند", ["پرو", "ماکس"]),
        new("چری تیگو ۸", "شاسی‌بلند", ["پرو", "ماکس"]),
        new("فیدلیتی", "شاسی‌بلند", ["پرایم", "پرایم ۵ نفره"]),
        new("دیگنیتی", "شاسی‌بلند", ["پرایم", "پرستیژ"]),
        new("هایما S5", "کراس‌اوور", ["۶ دنده", "اتوماتیک"]),
        new("هایما S7", "شاسی‌بلند", ["اتوماتیک", "پرمیوم"]),
        new("جک S3", "کراس‌اوور", ["دستی", "اتوماتیک"]),
        new("جک S5", "شاسی‌بلند", ["دستی", "اتوماتیک"]),
        new("تویوتا کرولا", "سواری", ["XLI", "GLI"]),
        new("تویوتا کمری", "سواری", ["GL", "GLX", "هایبرید"]),
        new("تویوتا RAV4", "شاسی‌بلند", ["استاندارد", "لیمیتد"]),
        new("تویوتا هایلوکس", "وانت", ["دو کابین", "تک کابین"]),
        new("هیوندای النترا", "سواری", ["پایه", "متوسط", "فول"]),
        new("هیوندای توسان", "شاسی‌بلند", ["پایه", "فول"]),
        new("هیوندای سانتافه", "شاسی‌بلند", ["پایه", "فول"]),
        new("کیا سراتو", "سواری", ["دستی", "اتوماتیک"]),
        new("کیا اسپورتیج", "شاسی‌بلند", ["پایه", "فول"]),
        new("کیا سورنتو", "شاسی‌بلند", ["پایه", "فول"]),
        new("نیسان جوک", "کراس‌اوور", ["پایه", "فول"]),
        new("نیسان قشقایی", "کراس‌اوور", ["پایه", "فول"]),
        new("رنو ساندرو", "سواری", ["دستی", "اتوماتیک", "استپ‌وی"]),
        new("رنو تندر ۹۰", "سواری", ["E0", "E1", "E2", "پارس"]),
        new("مزدا ۳", "سواری", ["تیپ ۱", "تیپ ۲", "تیپ ۳"]),
        new("بنز C کلاس", "سواری", ["C180", "C200", "C300"]),
        new("بی‌ام‌و سری ۳", "سواری", ["318i", "320i", "330i"]),
        new("آریسان", "وانت", ["۲ لیتر"]),
        new("پادرا", "وانت", ["تک کابین", "دو کابین"]),
        new("کاپرا", "وانت", ["۲ کابین"])
    ];

    public static IReadOnlyList<string> DefaultTrims { get; } =
    [
        "پایه",
        "متوسط",
        "فول",
        "دستی",
        "اتوماتیک"
    ];

    public static bool IsCustom(string? value) =>
        string.Equals(value, CustomValue, StringComparison.Ordinal);

    public static IReadOnlyList<string> GetTrimsForModel(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName) || IsCustom(modelName))
        {
            return DefaultTrims;
        }

        VehicleModelOption? match = Models.FirstOrDefault(model =>
            string.Equals(model.Name, modelName, StringComparison.OrdinalIgnoreCase));
        return match?.Trims ?? DefaultTrims;
    }

    public static IReadOnlyList<string> GetModelsForType(string? vehicleType)
    {
        if (string.IsNullOrWhiteSpace(vehicleType) || IsCustom(vehicleType))
        {
            return Models.Select(model => model.Name).ToArray();
        }

        string[] filtered = Models
            .Where(model => string.Equals(model.SuggestedType, vehicleType, StringComparison.OrdinalIgnoreCase))
            .Select(model => model.Name)
            .ToArray();
        return filtered.Length == 0 ? Models.Select(model => model.Name).ToArray() : filtered;
    }

    public static bool IsKnownValue(string? value, IEnumerable<string> catalog) =>
        !string.IsNullOrWhiteSpace(value) &&
        catalog.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
}

public sealed record VehicleModelOption(
    string Name,
    string SuggestedType,
    IReadOnlyList<string> Trims);
