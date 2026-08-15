using EmpPortal.Application.Authorization;

namespace EmpPortal.Application.Identity;

public static class PortalAutomaticRolePolicy
{
    public static IReadOnlyList<string> GetRequiredRoles(
        string upn,
        string? bootstrapAdministratorUpn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(upn);

        if (!string.IsNullOrWhiteSpace(bootstrapAdministratorUpn) &&
            string.Equals(
                upn.Trim(),
                bootstrapAdministratorUpn.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return [PortalRoles.Employee, PortalRoles.SystemAdministrator];
        }

        return [PortalRoles.Employee];
    }
}
