namespace EmpPortal.Application.Authorization;

public sealed record PortalResourceDefinition(
    string Key,
    string Title,
    string Description,
    string Category,
    string? MenuHref = null,
    bool DefaultEmployeeGrant = false,
    bool ComingSoon = false);

/// <summary>
/// Single source of truth for portal sections used by menu gating, policies, and access-admin grants.
/// Add new features here so AccessAdmin "منبع / بخش" stays in sync.
/// </summary>
public static class PortalResources
{
    public const string Dashboard = "portal.dashboard";
    public const string PersonnelView = "personnel.view";
    public const string PayslipView = "payslip.view";
    public const string PayslipSettings = "payslip.settings";
    public const string BenefitsView = "benefits.view";
    public const string FormsEvents = "forms.events";
    public const string FormsAdmin = "forms.admin";
    public const string FoodView = "food.view";
    public const string AssetsView = "assets.view";
    public const string CharityView = "charity.view";
    public const string SecurityView = "security.view";
    public const string AccessManage = "access.manage";
    public const string RuntimeSettings = "runtime.settings";

    public const string PersonnelViewPolicy = "PortalResource:personnel.view";
    public const string PayslipViewPolicy = "PortalResource:payslip.view";
    public const string PayslipSettingsPolicy = "PortalResource:payslip.settings";
    public const string BenefitsViewPolicy = "PortalResource:benefits.view";
    public const string FormsAdminPolicy = "PortalResource:forms.admin";
    public const string AssetsViewPolicy = "PortalResource:assets.view";
    public const string CharityViewPolicy = "PortalResource:charity.view";
    public const string AccessManagePolicy = "PortalResource:access.manage";
    public const string RuntimeSettingsPolicy = "PortalResource:runtime.settings";

    public static IReadOnlyList<PortalResourceDefinition> All { get; } =
    [
        new(Dashboard, "داشبورد", "صفحه اصلی پرتال پس از ورود.", "پرتال", "/", DefaultEmployeeGrant: true),
        new(PersonnelView, "پرونده پرسنلی", "مشاهده و ویرایش اطلاعات پرونده پرسنلی.", "منابع انسانی", "/services/personnel", DefaultEmployeeGrant: true),
        new(PayslipView, "فیش حقوقی من", "دسترسی به صفحه و منوی فیش حقوقی پرسنل.", "منابع انسانی", "/services/payslip", DefaultEmployeeGrant: true, ComingSoon: true),
        new(PayslipSettings, "تنظیمات فیش حقوقی", "فعال یا غیرفعال کردن نمایش فیش برای دوره‌های ماهانه.", "منابع انسانی", "/services/payslip-settings"),
        new(BenefitsView, "تسهیلات من", "مشاهده فهرست تسهیلات و مزایای پرسنل.", "منابع انسانی", "/services/benefits", DefaultEmployeeGrant: true),
        new(FormsEvents, "ثبت‌نام‌ها و رویدادها", "فهرست و تکمیل فرم‌های منتشرشده.", "خدمات کارکنان", "/forms", DefaultEmployeeGrant: true),
        new(FormsAdmin, "مدیریت و گزارش فرم‌ها", "طراحی، انتشار و گزارش فرم‌های سازمانی.", "مدیریت فرم‌ها", "/admin/forms"),
        new(FoodView, "رزرو غذا", "رزرو وعده‌های غذایی سازمانی.", "خدمات کارکنان", "/services/food", ComingSoon: true),
        new(AssetsView, "اموال من", "مشاهده فهرست اموال و تجهیزات تحویلی.", "خدمات کارکنان", "/services/assets", DefaultEmployeeGrant: true),
        new(CharityView, "انفاق", "خوداظهاری انفاق از حقوق ماهانه.", "خدمات کارکنان", "/services/charity", DefaultEmployeeGrant: true),
        new(SecurityView, "امنیت", "امنیت حساب و نشست‌های پرتال.", "امنیت", "/services/security", ComingSoon: true),
        new(AccessManage, "مدیریت دسترسی‌ها", "تخصیص نقش و مجوز بخش‌های سامانه به کاربران.", "سامانه", "/admin/access"),
        new(RuntimeSettings, "تنظیمات عملیاتی", "ویرایش تنظیمات runtime سامانه.", "سامانه", "/admin/settings")
    ];

    public static string PolicyName(string resourceKey) => $"PortalResource:{resourceKey}";

    public static PortalResourceDefinition? Find(string key) =>
        All.FirstOrDefault(resource => string.Equals(resource.Key, key, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnown(string key) => Find(key) is not null;

    public static IReadOnlyList<string> DefaultEmployeeResourceKeys { get; } =
        All.Where(resource => resource.DefaultEmployeeGrant).Select(resource => resource.Key).ToArray();
}
