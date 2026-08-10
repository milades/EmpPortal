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
    bool IsAvailable,
    bool OpensInNewTab = false);

public static class PortalServiceCatalog
{
    public const string FormsHref = "/forms";
    public const string FoodKey = "food";

    public static IReadOnlyList<PortalServiceDefinition> All { get; } =
    [
        new(
            "personnel",
            "پرونده پرسنلی",
            "اطلاعات پرسنلی",
            "دسترسی به خلاصه پرونده پرسنلی، سوابق سازمانی و اطلاعات پایه کارکنان در یک نمای یکپارچه.",
            "/services/personnel",
            PortalServiceTone.Indigo,
            IsAvailable: true),
        new(
            "food",
            "رزرو غذا",
            "سامانه خارجی",
            "ورود به سامانه خارجی رزرو وعده‌های غذایی سازمانی.",
            "/services/food",
            PortalServiceTone.Amber,
            IsAvailable: true,
            OpensInNewTab: true),
        new(
            "assets",
            "اموال من",
            "تجهیزات تحویلی",
            "فهرست اموال و تجهیزات تحویل‌شده به شما به‌همراه وضعیت و جزئیات مرتبط.",
            "/services/assets",
            PortalServiceTone.Teal,
            IsAvailable: true),
        new(
            "benefits",
            "تسهیلات من",
            "مزایای سازمانی",
            "مشاهده تسهیلات فعال، وضعیت درخواست‌ها و مزایای قابل استفاده در سازمان.",
            "/services/benefits",
            PortalServiceTone.Violet,
            IsAvailable: true),
        new(
            "payslip",
            "فیش حقوقی من",
            "فیش دوره‌ای",
            "دسترسی به فیش‌های حقوقی دوره‌ای و جزئیات پرداخت به‌صورت امن در شبکه داخلی.",
            "/services/payslip",
            PortalServiceTone.Green,
            IsAvailable: true),
        new(
            "payslip-settings",
            "تنظیمات فیش حقوقی",
            "نمایش دوره‌ها",
            "فعال یا غیرفعال کردن نمایش فیش حقوقی پرسنل بر اساس ماه و سال شمسی.",
            "/services/payslip-settings",
            PortalServiceTone.Blue,
            IsAvailable: true),
        new(
            "charity",
            "انفاق",
            "خوداظهاری",
            "شرکت در برنامه‌های انفاق سازمانی، ثبت مشارکت و پیگیری سوابق نیکوکاری.",
            "/services/charity",
            PortalServiceTone.Rose,
            IsAvailable: true),
        new(
            "charity-admin",
            "مدیریت انفاق",
            "خروجی و حذف",
            "مشاهده ثبت‌نام‌های انفاق، دریافت خروجی اکسل و حذف در صورت نیاز.",
            "/services/charity-admin",
            PortalServiceTone.Rose,
            IsAvailable: true),
        new(
            "events",
            "ثبت‌نام‌ها و رویدادها",
            "فرم‌های سازمانی",
            "فرم‌های منتشرشده توسط مدیریت پرتال برای ثبت‌نام در رویدادها، جشن‌ها، برنامه‌ها و سایر درخواست‌های سازمانی.",
            FormsHref,
            PortalServiceTone.Blue,
            IsAvailable: true)
    ];

    public static PortalServiceDefinition? Find(string key) =>
        All.FirstOrDefault(service => string.Equals(service.Key, key, StringComparison.OrdinalIgnoreCase));
}
