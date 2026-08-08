namespace EmpPortal.Domain.Forms;

[Flags]
public enum FormAccessRights
{
    None = 0,
    View = 1,
    Submit = 2,
    Manage = 4,
    ViewSubmissions = 8,
    Export = 16
}
