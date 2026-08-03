using System.Reflection;
using EmpPortal.Application.Identity;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Identity;

namespace EmpPortal.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        Assembly assembly = typeof(ApplicationSession).Assembly;

        AssertDoesNotReference(assembly, "EmpPortal.Application");
        AssertDoesNotReference(assembly, "EmpPortal.Infrastructure");
        AssertDoesNotReference(assembly, "EmpPortal.Web");
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrWeb()
    {
        Assembly assembly = typeof(IEnterpriseIdentityProvider).Assembly;

        AssertDoesNotReference(assembly, "EmpPortal.Infrastructure");
        AssertDoesNotReference(assembly, "EmpPortal.Web");
    }

    [Fact]
    public void InfrastructureDoesNotReferenceWeb()
    {
        Assembly assembly = typeof(DevelopmentEnterpriseIdentityProvider).Assembly;

        AssertDoesNotReference(assembly, "EmpPortal.Web");
    }

    private static void AssertDoesNotReference(Assembly assembly, string forbiddenAssemblyName)
    {
        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            reference => string.Equals(
                reference.Name,
                forbiddenAssemblyName,
                StringComparison.Ordinal));
    }
}
