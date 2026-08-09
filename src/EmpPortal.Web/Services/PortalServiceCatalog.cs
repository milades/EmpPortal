namespace EmpPortal.Web.Services;

public enum PortalServiceTone
{
    Blue,
    Violet,
    Teal,
    Amber,
    Rose,
    Slate,
    Green,
    Indigo
}

public sealed record PortalServiceDefinition(
    string Key,
    string Title,
    string ShortDescription,
    string LongDescription,
    string Href,
    PortalServiceTone Tone,
    bool IsAvailable);

public static class PortalServiceCatalog
{
    public const string FormsHref = "/forms";

    public static IReadOnlyList<PortalServiceDefinition> All { get; } =
    [
        new(
            "personnel",
            "پرونده پرسنلی",
            "مشاهده و پیگیری اطلاعات پرسنلی",
            "دسترسی به خلاصه پرونده پرسنلی، سوابق سازمانی و اطلاعات پایه کارکنان در یک نمای یکپارچه.",
            "/services/personnel",
            PortalServiceTone.Indigo,
            IsAvailable: true),
        new(
            "security",
            "امنیت",
            "امنیت حساب و نشست‌های پرتال",
            "مدیریت امنیت حساب کاربری، نشست‌های فعال و هشدارهای امنیتی مرتبط با دسترسی به پرتال.",
            "/services/security",
            PortalServiceTone.Slate,
            IsAvailable: false),
        new(
            "food",
            "رزرو غذا",
            "رزرو وعده‌های غذایی سازمانی",
            "انتخاب و رزرو وعده‌های غذایی، مشاهده برنامه روزانه و پیگیری رزروهای ثبت‌شده.",
            "/services/food",
            PortalServiceTone.Amber,
            IsAvailable: false),
        new(
            "assets",
            "اموال من",
            "اموال و تجهیزات تحویلی",
            "فهرست اموال و تجهیزات تحویل‌شده به شما به‌همراه وضعیت و جزئیات مرتبط.",
            "/services/assets",
            PortalServiceTone.Teal,
            IsAvailable: true),
        new(
            "benefits",
            "تسهیلات من",
            "تسهیلات و مزایای سازمانی",
            "مشاهده تسهیلات فعال، وضعیت درخواست‌ها و مزایای قابل استفاده در سازمان.",
            "/services/benefits",
            PortalServiceTone.Violet,
            IsAvailable: true),
        new(
            "payslip",
            "فیش حقوقی من",
            "مشاهده فیش حقوقی",
            "دسترسی به فیش‌های حقوقی دوره‌ای و جزئیات پرداخت به‌صورت امن در شبکه داخلی.",
            "/services/payslip",
            PortalServiceTone.Green,
            IsAvailable: false),
        new(
            "payslip-settings",
            "تنظیمات فیش حقوقی",
            "ترجیحات نمایش فیش حقوقی",
            "فعال یا غیرفعال کردن نمایش فیش حقوقی پرسنل بر اساس ماه و سال شمسی.",
            "/services/payslip-settings",
            PortalServiceTone.Blue,
            IsAvailable: true),
        new(
            "charity",
            "انفاق",
            "مشارکت در برنامه‌های انفاق",
            "شرکت در برنامه‌های انفاق سازمانی، ثبت مشارکت و پیگیری سوابق نیکوکاری.",
            "/services/charity",
            PortalServiceTone.Rose,
            IsAvailable: true),
        new(
            "events",
            "ثبت‌نام‌ها و رویدادها",
            "ثبت‌نام جشنواره، کنسرت، جشن و برنامه‌ها",
            "فرم‌های منتشرشده توسط مدیریت پرتال برای ثبت‌نام در رویدادها، جشن‌ها، برنامه‌ها و سایر درخواست‌های سازمانی.",
            FormsHref,
            PortalServiceTone.Blue,
            IsAvailable: true)
    ];

    public static PortalServiceDefinition? Find(string key) =>
        All.FirstOrDefault(service => string.Equals(service.Key, key, StringComparison.OrdinalIgnoreCase));
}
