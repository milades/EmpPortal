namespace EmpPortal.Infrastructure.Hr;

public sealed class PayslipReportOptions
{
    public const string SectionName = "Payslip:Report";

    /// <summary>
    /// Relative to the web content root, or an absolute path.
    /// Example: Reports/Payslip.mrt
    /// </summary>
    public string TemplateRelativePath { get; set; } = "Reports/Payslip.mrt";

    /// <summary>
    /// Optional Stimulsoft license key. Leave empty for evaluation mode.
    /// </summary>
    public string? LicenseKey { get; set; }

    /// <summary>
    /// Variable name in the .mrt dictionary for the employee personnel code (optional).
    /// </summary>
    public string PersonnelCodeVariable { get; set; } = "PersonnelCode";

    /// <summary>
    /// Variable name in the .mrt dictionary for Persian year (optional).
    /// </summary>
    public string PersianYearVariable { get; set; } = "PersianYear";

    /// <summary>
    /// Variable name in the .mrt dictionary for Persian month (optional).
    /// </summary>
    public string PersianMonthVariable { get; set; } = "PersianMonth";
}
