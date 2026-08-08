using System.Security.Claims;
using EmpPortal.Application.Forms;
using Microsoft.AspNetCore.Components.Authorization;

namespace EmpPortal.Web.Security;

public sealed class FormActorFactory(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<FormActor> CreateAsync()
    {
        AuthenticationState authenticationState =
            await authenticationStateProvider.GetAuthenticationStateAsync();
        return Create(authenticationState.User, httpContextAccessor.HttpContext);
    }

    public static FormActor Create(ClaimsPrincipal principal, HttpContext? httpContext)
    {
        ArgumentNullException.ThrowIfNull(principal);
        string? userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new UnauthorizedAccessException("شناسه کاربر واردشده معتبر نیست.");
        }

        string upn = principal.FindFirstValue(ClaimTypes.Upn) ??
            principal.Identity?.Name ??
            throw new UnauthorizedAccessException("نام کاربری واردشده معتبر نیست.");
        string correlationId = httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("D");
        return new FormActor(
            userId,
            upn,
            principal.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            correlationId,
            httpContext?.Connection.RemoteIpAddress?.ToString());
    }
}
