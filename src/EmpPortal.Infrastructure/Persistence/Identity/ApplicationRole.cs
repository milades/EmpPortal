using Microsoft.AspNetCore.Identity;

namespace EmpPortal.Infrastructure.Persistence.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
}
