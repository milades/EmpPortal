using EmpPortal.Application.Identity;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Identity;

public sealed class IdentityPortalSignOutService(
    SignInManager<ApplicationUser> signInManager,
    PortalDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : IPortalSignOutService
{
    public async Task SignOutAsync(
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        if (sessionId.HasValue)
        {
            ApplicationSession? session = await dbContext.ApplicationSessions
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == sessionId.Value,
                    cancellationToken);

            if (session is not null)
            {
                session.Revoke(nowUtc, "user-sign-out");
            }
        }

        string? userIdValue = httpContext?.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdValue, out Guid parsedUserId)
            ? parsedUserId
            : null;
        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "UserSignedOut",
            "Succeeded",
            userId,
            httpContext?.User.Identity?.Name,
            sessionId?.ToString("D"),
            httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("D"),
            httpContext?.Connection.RemoteIpAddress?.ToString()));
        await dbContext.SaveChangesAsync(cancellationToken);

        await signInManager.SignOutAsync();
    }
}
