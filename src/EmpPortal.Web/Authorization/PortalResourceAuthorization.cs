using EmpPortal.Application.Authorization;
using EmpPortal.Web.Security;
using Microsoft.AspNetCore.Authorization;

namespace EmpPortal.Web.Authorization;

public sealed class PortalResourceRequirement(string resourceKey) : IAuthorizationRequirement
{
    public string ResourceKey { get; } = resourceKey;
}

public sealed class PortalResourceAuthorizationHandler(
    IPortalAccessEvaluator accessEvaluator,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PortalResourceRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PortalResourceRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        try
        {
            PortalActor actor = PortalActorFactory.Create(context.User, httpContextAccessor.HttpContext);
            if (await accessEvaluator.HasAccessAsync(actor, requirement.ResourceKey))
            {
                context.Succeed(requirement);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Leave the requirement unsatisfied.
        }
    }
}
