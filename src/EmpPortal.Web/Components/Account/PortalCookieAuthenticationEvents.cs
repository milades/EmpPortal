using System.Security.Claims;
using EmpPortal.Application.Identity;
using EmpPortal.Application.Security;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmpPortal.Web.Components.Account;

internal sealed class PortalCookieAuthenticationEvents(
    UserManager<ApplicationUser> userManager,
    PortalDbContext dbContext,
    IEnterpriseIdentityProvider directoryIdentityProvider,
    SessionPolicy sessionPolicy,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> identityOptions,
    ILogger<PortalCookieAuthenticationEvents> logger) : CookieAuthenticationEvents
{
    private const string SessionIdClaimType = "sid";
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(1);
    private static readonly Action<ILogger, string, Exception?> LogCookieRejected =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1001, nameof(LogCookieRejected)),
            "Identity cookie rejected. Reason: {Reason}");

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        CancellationToken cancellationToken = context.HttpContext.RequestAborted;
        ApplicationUser? user = await userManager.GetUserAsync(context.Principal);
        if (user is null || !user.IsDirectoryEnabled)
        {
            await RejectAsync(context, "identity-user-disabled-or-missing");
            return;
        }

        if (userManager.SupportsUserSecurityStamp)
        {
            string? principalStamp = context.Principal.FindFirstValue(
                identityOptions.Value.ClaimsIdentity.SecurityStampClaimType);
            string? persistedStamp = await userManager.GetSecurityStampAsync(user);
            if (!string.Equals(principalStamp, persistedStamp, StringComparison.Ordinal))
            {
                await RejectAsync(context, "security-stamp-mismatch");
                return;
            }
        }

        string? sessionIdValue = context.Principal.FindFirstValue(SessionIdClaimType);
        if (!Guid.TryParse(sessionIdValue, out Guid sessionId))
        {
            await RejectAsync(context, "session-claim-missing");
            return;
        }

        ApplicationSession? session = await dbContext.ApplicationSessions.SingleOrDefaultAsync(
            candidate => candidate.Id == sessionId && candidate.UserId == user.Id,
            cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        if (session is null || !session.IsActiveAt(nowUtc))
        {
            await RejectAsync(context, "session-inactive");
            return;
        }

        DirectoryAccountState state = await directoryIdentityProvider.GetAccountStateAsync(
            user.DirectoryObjectGuid,
            cancellationToken);
        if (state != DirectoryAccountState.Enabled)
        {
            user.IsDirectoryEnabled = false;
            await userManager.UpdateSecurityStampAsync(user);
            await RevokeAllSessionsAsync(user.Id, nowUtc, state.ToString(), cancellationToken);
            await RejectAsync(context, "directory-account-not-enabled");
            return;
        }

        if (nowUtc - session.LastActivityAtUtc >= ActivityWriteInterval)
        {
            session.RecordActivity(nowUtc, sessionPolicy.IdleTimeout);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RevokeAllSessionsAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        string accountState,
        CancellationToken cancellationToken)
    {
        List<ApplicationSession> activeSessions = await dbContext.ApplicationSessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (ApplicationSession activeSession in activeSessions)
        {
            activeSession.Revoke(nowUtc, $"directory-account-{accountState.ToLowerInvariant()}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
    {
        LogCookieRejected(logger, reason, null);
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
}
