using System.Security.Claims;
using EmpPortal.Application.Authorization;
using EmpPortal.Application.Identity;
using EmpPortal.Application.Security;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmpPortal.Infrastructure.Identity;

public sealed class IdentityPortalSignInService(
    IEnterpriseIdentityProvider directoryIdentityProvider,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    PortalDbContext dbContext,
    IOptions<BootstrapAdministratorOptions> bootstrapAdministratorOptions,
    SessionPolicy sessionPolicy,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : IPortalSignInService
{
    private const string DirectoryLoginProvider = "ActiveDirectory";
    private const string SessionIdClaimType = "sid";

    public async Task<PortalSignInResult> SsoSignInAsync(
        string loginName,
        CancellationToken cancellationToken = default)
    {
        EnterpriseIdentity? identity = await directoryIdentityProvider.FindByLoginNameAsync(
            loginName,
            cancellationToken);

        return identity is null
            ? await RecordFailedSignInAsync(
                loginName,
                "ActiveDirectorySso",
                PortalSignInStatus.InvalidCredentials,
                cancellationToken)
            : await CompleteSignInAsync(identity, "ActiveDirectorySso", cancellationToken);
    }

    public async Task<PortalSignInResult> PasswordSignInAsync(
        string upn,
        string password,
        CancellationToken cancellationToken = default)
    {
        PasswordAuthenticationResult directoryResult =
            await directoryIdentityProvider.AuthenticatePasswordAsync(upn, password, cancellationToken);

        if (!directoryResult.Succeeded)
        {
            PortalSignInResult failure = MapDirectoryFailure(directoryResult.Failure);
            return await RecordFailedSignInAsync(
                upn,
                "ActiveDirectoryPassword",
                failure.Status,
                cancellationToken);
        }

        return await CompleteSignInAsync(
            directoryResult.Identity!,
            "ActiveDirectoryPassword",
            cancellationToken);
    }

    private async Task<PortalSignInResult> CompleteSignInAsync(
        EnterpriseIdentity identity,
        string authenticationMethod,
        CancellationToken cancellationToken)
    {
        if (identity.State != DirectoryAccountState.Enabled)
        {
            return await RecordFailedSignInAsync(
                identity.Upn,
                authenticationMethod,
                PortalSignInStatus.AccountNotAllowed,
                cancellationToken);
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        ApplicationUser? user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.DirectoryObjectGuid == identity.ObjectGuid,
            cancellationToken);

        IdentityResult persistenceResult;
        if (user is null)
        {
            user = CreateApplicationUser(identity, nowUtc);
            persistenceResult = await userManager.CreateAsync(user);

            if (persistenceResult.Succeeded)
            {
                persistenceResult = await userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo(
                        DirectoryLoginProvider,
                        identity.ObjectGuid.ToString("D"),
                        "Active Directory"));
            }
        }
        else
        {
            SynchronizeApplicationUser(user, identity, nowUtc);
            persistenceResult = await userManager.UpdateAsync(user);
        }

        if (!persistenceResult.Succeeded)
        {
            return await RecordFailedSignInAsync(
                identity.Upn,
                authenticationMethod,
                PortalSignInStatus.IdentityStoreFailure,
                cancellationToken);
        }

        IdentityResult roleResult = await EnsureRolesAsync(user, identity.Upn);
        if (!roleResult.Succeeded)
        {
            return await RecordFailedSignInAsync(
                identity.Upn,
                authenticationMethod,
                PortalSignInStatus.IdentityStoreFailure,
                cancellationToken);
        }

        await RevokeExcessSessionsAsync(user.Id, sessionPolicy, nowUtc, cancellationToken);

        ApplicationSession session = ApplicationSession.Create(
            user.Id,
            nowUtc,
            sessionPolicy.AbsoluteLifetime,
            sessionPolicy.IdleTimeout);
        dbContext.ApplicationSessions.Add(session);
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "UserSignedIn",
            "Succeeded",
            user.Id,
            identity.Upn,
            authenticationMethod,
            httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("D"),
            httpContext?.Connection.RemoteIpAddress?.ToString()));
        await dbContext.SaveChangesAsync(cancellationToken);

        AuthenticationProperties properties = new()
        {
            AllowRefresh = false,
            ExpiresUtc = session.AbsoluteExpiresAtUtc,
            IsPersistent = false
        };
        Claim[] additionalClaims =
        [
            new Claim(SessionIdClaimType, session.Id.ToString("D")),
            new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod)
        ];

        await signInManager.SignInWithClaimsAsync(user, properties, additionalClaims);
        return new PortalSignInResult(PortalSignInStatus.Succeeded);
    }

    private async Task<PortalSignInResult> RecordFailedSignInAsync(
        string attemptedUpn,
        string authenticationMethod,
        PortalSignInStatus status,
        CancellationToken cancellationToken)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        dbContext.AuditEvents.Add(AuditEvent.Create(
            timeProvider.GetUtcNow(),
            "UserSignInFailed",
            status.ToString(),
            actorUserId: null,
            attemptedUpn,
            authenticationMethod,
            httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("D"),
            httpContext?.Connection.RemoteIpAddress?.ToString()));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PortalSignInResult(status);
    }

    private async Task<IdentityResult> EnsureRolesAsync(ApplicationUser user, string upn)
    {
        IdentityResult employeeRoleResult = await EnsureRoleExistsAsync(
            PortalRoles.Employee,
            "کاربر عادی پرتال");
        if (!employeeRoleResult.Succeeded)
        {
            return employeeRoleResult;
        }

        if (!await userManager.IsInRoleAsync(user, PortalRoles.Employee))
        {
            IdentityResult employeeAssignment = await userManager.AddToRoleAsync(
                user,
                PortalRoles.Employee);
            if (!employeeAssignment.Succeeded)
            {
                return employeeAssignment;
            }
        }

        string bootstrapUpn = bootstrapAdministratorOptions.Value.Upn.Trim();
        if (!string.Equals(upn, bootstrapUpn, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityResult.Success;
        }

        IdentityResult administratorRoleResult = await EnsureRoleExistsAsync(
            PortalRoles.SystemAdministrator,
            "مدیر کل سامانه");
        if (!administratorRoleResult.Succeeded)
        {
            return administratorRoleResult;
        }

        return await userManager.IsInRoleAsync(user, PortalRoles.SystemAdministrator)
            ? IdentityResult.Success
            : await userManager.AddToRoleAsync(user, PortalRoles.SystemAdministrator);
    }

    private async Task<IdentityResult> EnsureRoleExistsAsync(string roleName, string description)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return IdentityResult.Success;
        }

        return await roleManager.CreateAsync(new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            Description = description
        });
    }

    private async Task RevokeExcessSessionsAsync(
        Guid userId,
        SessionPolicy policy,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        List<ApplicationSession> activeSessions = await dbContext.ApplicationSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAtUtc == null &&
                session.AbsoluteExpiresAtUtc > nowUtc &&
                session.IdleExpiresAtUtc > nowUtc)
            .OrderBy(session => session.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        int sessionsToRevoke = activeSessions.Count - policy.MaxConcurrentSessions + 1;
        foreach (ApplicationSession session in activeSessions.Take(Math.Max(0, sessionsToRevoke)))
        {
            session.Revoke(nowUtc, "concurrent-session-limit");
        }
    }

    private static ApplicationUser CreateApplicationUser(
        EnterpriseIdentity identity,
        DateTimeOffset nowUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserName = identity.Upn,
            Email = identity.Email,
            EmailConfirmed = !string.IsNullOrWhiteSpace(identity.Email),
            DirectoryObjectGuid = identity.ObjectGuid,
            Sid = identity.Sid,
            DisplayName = identity.DisplayName,
            IsDirectoryEnabled = true,
            LastDirectoryValidationAtUtc = nowUtc,
            AuthorizationVersion = 0
        };

    private static void SynchronizeApplicationUser(
        ApplicationUser user,
        EnterpriseIdentity identity,
        DateTimeOffset nowUtc)
    {
        user.UserName = identity.Upn;
        user.Email = identity.Email;
        user.EmailConfirmed = !string.IsNullOrWhiteSpace(identity.Email);
        user.Sid = identity.Sid;
        user.DisplayName = identity.DisplayName;
        user.IsDirectoryEnabled = true;
        user.LastDirectoryValidationAtUtc = nowUtc;
    }

    private static PortalSignInResult MapDirectoryFailure(PasswordAuthenticationFailure failure) =>
        failure switch
        {
            PasswordAuthenticationFailure.InvalidCredentials =>
                new PortalSignInResult(PortalSignInStatus.InvalidCredentials),
            PasswordAuthenticationFailure.Disabled or
            PasswordAuthenticationFailure.Locked or
            PasswordAuthenticationFailure.PasswordExpired or
            PasswordAuthenticationFailure.Expired =>
                new PortalSignInResult(PortalSignInStatus.AccountNotAllowed),
            _ => new PortalSignInResult(PortalSignInStatus.DirectoryUnavailable)
        };
}
