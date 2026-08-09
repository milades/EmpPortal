namespace EmpPortal.Domain.Access;

public sealed class PortalAccessGrant
{
    private PortalAccessGrant()
    {
    }

    public Guid Id { get; private set; }

    public string ResourceKey { get; private set; } = string.Empty;

    public PortalAccessSubjectType SubjectType { get; private set; }

    public string SubjectKey { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static PortalAccessGrant Create(
        string resourceKey,
        PortalAccessSubjectType subjectType,
        string subjectKey,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);

        string normalizedKey = subjectType switch
        {
            PortalAccessSubjectType.Everyone => "*",
            PortalAccessSubjectType.User when Guid.TryParse(subjectKey, out Guid userId) && userId != Guid.Empty =>
                userId.ToString("D"),
            PortalAccessSubjectType.Role when !string.IsNullOrWhiteSpace(subjectKey) => subjectKey.Trim(),
            _ => throw new ArgumentException("The access-grant subject is invalid.", nameof(subjectKey))
        };

        return new PortalAccessGrant
        {
            Id = Guid.NewGuid(),
            ResourceKey = resourceKey.Trim(),
            SubjectType = subjectType,
            SubjectKey = normalizedKey,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc
        };
    }
}
