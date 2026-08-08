namespace EmpPortal.Infrastructure.Forms;

public sealed class FormPdfOptions
{
    public const string SectionName = "Forms:Pdf";

    public string License { get; set; } = string.Empty;

    public string RegularFontPath { get; set; } = "wwwroot/fonts/Vazirmatn-Regular.ttf";

    public string BoldFontPath { get; set; } = "wwwroot/fonts/Vazirmatn-Bold.ttf";
}
