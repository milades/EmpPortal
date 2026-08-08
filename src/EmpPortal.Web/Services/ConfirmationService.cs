using Microsoft.JSInterop;

namespace EmpPortal.Web.Services;

public interface IConfirmationService
{
    public ValueTask<bool> ConfirmAsync(
        string title,
        string text,
        string confirmButtonText,
        string icon = "warning");
}

internal sealed class SweetAlertConfirmationService(IJSRuntime javaScript) : IConfirmationService
{
    public ValueTask<bool> ConfirmAsync(
        string title,
        string text,
        string confirmButtonText,
        string icon = "warning") =>
        javaScript.InvokeAsync<bool>(
            "empPortalDialogs.confirm",
            new
            {
                title,
                text,
                confirmButtonText,
                icon
            });
}
