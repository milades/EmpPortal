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
}
