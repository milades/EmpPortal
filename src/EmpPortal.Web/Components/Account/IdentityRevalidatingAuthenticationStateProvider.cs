using System.Security.Claims;
using EmpPortal.Application.Identity;
using EmpPortal.Application.Security;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmpPortal.Web.Components.Account;

internal sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> identityOptions,
    SessionPolicy sessionPolicy,
    TimeProvider timeProvider)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    private const string SessionIdClaimType = "sid";

    protected override TimeSpan RevalidationInterval => sessionPolicy.DirectoryRevalidationInterval;

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        PortalDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        IEnterpriseIdentityProvider directoryIdentityProvider =
            scope.ServiceProvider.GetRequiredService<IEnterpriseIdentityProvider>();

        ApplicationUser? user = await userManager.GetUserAsync(authenticationState.User);
        if (user is null || !user.IsDirectoryEnabled)
        {
            return false;
        }

        if (!await HasValidSecurityStampAsync(userManager, user, authenticationState.User))
        {
            return false;
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        DirectoryAccountState state = await directoryIdentityProvider.GetAccountStateAsync(
            user.DirectoryObjectGuid,
            cancellationToken);
        if (state != DirectoryAccountState.Enabled)
        {
            user.IsDirectoryEnabled = false;
            await userManager.UpdateSecurityStampAsync(user);
            await RevokeAllSessionsAsync(
                dbContext,
                user.Id,
                nowUtc,
                state,
                cancellationToken);
            return false;
        }

        string? sessionIdValue = authenticationState.User.FindFirstValue(SessionIdClaimType);
        if (!Guid.TryParse(sessionIdValue, out Guid sessionId))
        {
            return false;
        }

        ApplicationSession? session = await dbContext.ApplicationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken);

        return session is not null && session.IsActiveAt(nowUtc);
    }

    private static async Task RevokeAllSessionsAsync(
        PortalDbContext dbContext,
        Guid userId,
        DateTimeOffset nowUtc,
        DirectoryAccountState state,
        CancellationToken cancellationToken)
    {
        List<ApplicationSession> sessions = await dbContext.ApplicationSessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (ApplicationSession session in sessions)
        {
            session.Revoke(nowUtc, $"directory-account-{state.ToString().ToLowerInvariant()}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasValidSecurityStampAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        ClaimsPrincipal principal)
    {
        if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }

        string? principalStamp = principal.FindFirstValue(
            identityOptions.Value.ClaimsIdentity.SecurityStampClaimType);
        string? userStamp = await userManager.GetSecurityStampAsync(user);
        return string.Equals(principalStamp, userStamp, StringComparison.Ordinal);
    }
}
