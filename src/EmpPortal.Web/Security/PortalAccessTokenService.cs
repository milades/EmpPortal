using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EmpPortal.Application.Security;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Web.Security;

internal sealed class PortalAccessTokenService(
    UserManager<ApplicationUser> userManager,
    PortalDbContext dbContext,
    JwtOptions options,
    SessionPolicy sessionPolicy,
    JwtSigningKeyProvider signingKeyProvider,
    TimeProvider timeProvider)
{
    private const string SessionIdClaimType = "sid";

    public async Task<AccessTokenResponse?> CreateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.GetUserAsync(principal);
        string? sessionIdValue = principal.FindFirstValue(SessionIdClaimType);
        if (user is null ||
            !user.IsDirectoryEnabled ||
            !Guid.TryParse(sessionIdValue, out Guid sessionId))
        {
            return null;
        }

        ApplicationSession? session = await dbContext.ApplicationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == sessionId && candidate.UserId == user.Id,
                cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        if (session is null || !session.IsActiveAt(nowUtc))
        {
            return null;
        }

        DateTimeOffset configuredExpiration = nowUtc.Add(sessionPolicy.AccessTokenLifetime);
        DateTimeOffset expiresUtc = new[]
        {
            configuredExpiration,
            session.AbsoluteExpiresAtUtc,
            session.IdleExpiresAtUtc
        }.Min();

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Id.ToString("D")),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new Claim(SessionIdClaimType, session.Id.ToString("D")),
            new Claim("authorization_version", user.AuthorizationVersion.ToString(
                System.Globalization.CultureInfo.InvariantCulture))
        ];

        IList<string> roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        JwtSecurityToken token = new(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: nowUtc.UtcDateTime,
            expires: expiresUtc.UtcDateTime,
            signingCredentials: signingKeyProvider.SigningCredentials);
        string serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResponse(
            serializedToken,
            "Bearer",
            Math.Max(0, (int)(expiresUtc - nowUtc).TotalSeconds));
    }
}
