namespace EmpPortal.Application.Authorization;

public static class PortalRoles
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string Employee = "Employee";
    public const string FormAdministrator = "FormAdministrator";
    public const string FormDesigner = "FormDesigner";
    public const string FormPublisher = "FormPublisher";
    public const string SubmissionViewer = "SubmissionViewer";
    public const string ReportExporter = "ReportExporter";

    public const string FormManagementRoles =
        SystemAdministrator + "," + FormAdministrator + "," + FormDesigner;

    public const string FormPublishingRoles =
        SystemAdministrator + "," + FormAdministrator + "," + FormPublisher;

    public const string FormReportingRoles =
        SystemAdministrator + "," + FormAdministrator + "," + SubmissionViewer + "," + ReportExporter;

    public static IReadOnlyList<(string Name, string Description)> SystemRoleSeed { get; } =
    [
        (Employee, "کاربر عادی پرتال"),
        (SystemAdministrator, "مدیر کل سامانه"),
        (FormAdministrator, "مدیر فرم‌ها"),
        (FormDesigner, "طراح فرم"),
        (FormPublisher, "منتشرکننده فرم"),
        (SubmissionViewer, "مشاهده‌کننده پاسخ فرم‌ها"),
        (ReportExporter, "دریافت‌کننده خروجی گزارش‌ها")
    ];

    public static bool IsSystemRoleName(string roleName) =>
        SystemRoleSeed.Any(role => string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase));
}
