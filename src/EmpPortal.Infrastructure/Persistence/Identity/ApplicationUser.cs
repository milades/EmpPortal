using Microsoft.AspNetCore.Identity;

namespace EmpPortal.Infrastructure.Persistence.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid DirectoryObjectGuid { get; set; }

    public string Sid { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsDirectoryEnabled { get; set; } = true;

    public DateTimeOffset LastDirectoryValidationAtUtc { get; set; }

    public long AuthorizationVersion { get; set; }
}
