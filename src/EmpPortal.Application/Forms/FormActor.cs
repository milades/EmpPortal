namespace EmpPortal.Application.Forms;

public sealed record FormActor(
    Guid UserId,
    string Upn,
    IReadOnlySet<string> Roles,
    string CorrelationId,
    string? IpAddress);
