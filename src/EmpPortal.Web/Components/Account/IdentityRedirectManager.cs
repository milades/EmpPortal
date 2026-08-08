using Microsoft.AspNetCore.Components;

namespace EmpPortal.Web.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public void RedirectTo(string? uri)
    {
        string target = string.IsNullOrWhiteSpace(uri) ? "/" : uri.Trim();

        if (Uri.TryCreate(target, UriKind.Absolute, out Uri? absoluteTarget))
        {
            Uri baseUri = new(navigationManager.BaseUri);
            target = string.Equals(absoluteTarget.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(absoluteTarget.Authority, baseUri.Authority, StringComparison.OrdinalIgnoreCase)
                ? "/" + navigationManager.ToBaseRelativePath(absoluteTarget.ToString()).TrimStart('/')
                : "/";
        }
        else if (target.StartsWith("//", StringComparison.Ordinal))
        {
            target = "/";
        }
        else if (!target.StartsWith('/'))
        {
            target = "/" + target;
        }

        target = target.Length == 0 ? "/" : target;

        navigationManager.NavigateTo(target);
    }
}
