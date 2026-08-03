namespace EmpPortal.Application.Identity;

public enum DirectoryAccountState
{
    Enabled = 0,
    Disabled = 1,
    Locked = 2,
    PasswordExpired = 3,
    Expired = 4,
    Unavailable = 5
}
