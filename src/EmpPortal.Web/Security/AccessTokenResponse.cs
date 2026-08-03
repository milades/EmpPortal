namespace EmpPortal.Web.Security;

internal sealed record AccessTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);
