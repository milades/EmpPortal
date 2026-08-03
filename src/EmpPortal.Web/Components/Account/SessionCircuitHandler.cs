using System.Security.Claims;
using EmpPortal.Application.Security;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Web.Components.Account;

internal sealed class SessionCircuitHandler(
    AuthenticationStateProvider authenticationStateProvider,
    IDbContextFactory<PortalDbContext> dbContextFactory,
    SessionPolicy sessionPolicy,
    TimeProvider timeProvider) : CircuitHandler
{
    private const string SessionIdClaimType = "sid";
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(1);
    private DateTimeOffset _lastActivityWriteUtc = DateTimeOffset.MinValue;

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next) =>
        async context =>
        {
            await RecordActivityAsync();
            await next(context);
        };

    private async Task RecordActivityAsync()
    {
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        if (nowUtc - _lastActivityWriteUtc < ActivityWriteInterval)
        {
            return;
        }

        AuthenticationState authenticationState =
            await authenticationStateProvider.GetAuthenticationStateAsync();
        string? sessionIdValue = authenticationState.User.FindFirstValue(SessionIdClaimType);
        if (!Guid.TryParse(sessionIdValue, out Guid sessionId))
        {
            return;
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        ApplicationSession? session = await dbContext.ApplicationSessions.SingleOrDefaultAsync(
            candidate => candidate.Id == sessionId);

        if (session is null || !session.IsActiveAt(nowUtc))
        {
            return;
        }

        session.RecordActivity(nowUtc, sessionPolicy.IdleTimeout);
        await dbContext.SaveChangesAsync();
        _lastActivityWriteUtc = nowUtc;
    }
}
