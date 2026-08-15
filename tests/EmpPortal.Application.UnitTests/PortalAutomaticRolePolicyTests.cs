using EmpPortal.Application.Authorization;
using EmpPortal.Application.Identity;

namespace EmpPortal.Application.UnitTests;

public sealed class PortalAutomaticRolePolicyTests
{
    [Fact]
    public void BootstrapAdministratorReceivesEmployeeAndAdministratorRoles()
    {
        IReadOnlyList<string> roles = PortalAutomaticRolePolicy.GetRequiredRoles(
            "PORTAL.ADMIN@EXAMPLE.TEST",
            " portal.admin@example.test ");

        Assert.Equal([PortalRoles.Employee, PortalRoles.SystemAdministrator], roles);
    }

    [Fact]
    public void OrdinaryUserOnlyReceivesEmployeeRoleAutomatically()
    {
        IReadOnlyList<string> roles = PortalAutomaticRolePolicy.GetRequiredRoles(
            "employee@example.test",
            "portal.admin@example.test");

        Assert.Equal([PortalRoles.Employee], roles);
    }

    [Fact]
    public void EmptyBootstrapConfigurationDoesNotPromoteAnOrdinaryUser()
    {
        IReadOnlyList<string> roles = PortalAutomaticRolePolicy.GetRequiredRoles(
            "employee@example.test",
            "  ");

        Assert.Equal([PortalRoles.Employee], roles);
    }
}
