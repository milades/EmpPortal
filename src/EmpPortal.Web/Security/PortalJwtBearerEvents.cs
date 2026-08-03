using System.Globalization;
using System.Security.Claims;
using EmpPortal.Application.Identity;
using EmpPortal.Application.Security;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Web.Security;

internal sealed class PortalJwtBearerEvents(
    UserManager<ApplicationUser> userManager,
    PortalDbContext dbContext,
    IEnterpriseIdentityProvider directoryIdentityProvider,
    SessionPolicy sessionPolicy,
    TimeProvider timeProvider) : JwtBearerEvents
{
    private const string SessionIdClaimType = "sid";
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(1);

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        ClaimsPrincipal? principal = context.Principal;
        string? subject = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            principal?.FindFirstValue("sub");
        string? sessionIdValue = principal?.FindFirstValue(SessionIdClaimType);
        string? authorizationVersionValue = principal?.FindFirstValue("authorization_version");

        if (!Guid.TryParse(subject, out Guid userId) ||
            !Guid.TryParse(sessionIdValue, out Guid sessionId) ||
            !long.TryParse(
                authorizationVersionValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long authorizationVersion))
        {
            context.Fail("Required token claims are invalid.");
            return;
        }

        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString("D"));
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        if (user is null ||
            !user.IsDirectoryEnabled ||
            user.AuthorizationVersion != authorizationVersion)
        {
            context.Fail("The application identity is inactive or stale.");
            return;
        }

        ApplicationSession? session = await dbContext.ApplicationSessions.SingleOrDefaultAsync(
            candidate => candidate.Id == sessionId && candidate.UserId == user.Id,
            context.HttpContext.RequestAborted);
        if (session is null || !session.IsActiveAt(nowUtc))
        {
            context.Fail("The application session is inactive.");
            return;
        }

        DirectoryAccountState state = await directoryIdentityProvider.GetAccountStateAsync(
            user.DirectoryObjectGuid,
            context.HttpContext.RequestAborted);
        if (state != DirectoryAccountState.Enabled)
        {
            user.IsDirectoryEnabled = false;
            await userManager.UpdateSecurityStampAsync(user);
            await RevokeAllSessionsAsync(
                user.Id,
                nowUtc,
                state,
                context.HttpContext.RequestAborted);
            context.Fail("The directory account is inactive.");
            return;
        }

        if (nowUtc - session.LastActivityAtUtc >= ActivityWriteInterval)
        {
            session.RecordActivity(nowUtc, sessionPolicy.IdleTimeout);
            await dbContext.SaveChangesAsync(context.HttpContext.RequestAborted);
        }
    }

    private async Task RevokeAllSessionsAsync(
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
}
