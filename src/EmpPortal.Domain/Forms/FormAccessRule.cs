namespace EmpPortal.Domain.Forms;

public sealed class FormAccessRule
{
    private FormAccessRule()
    {
    }

    public Guid Id { get; private set; }

    public Guid FormId { get; private set; }

    public FormAccessSubjectType SubjectType { get; private set; }

    public string SubjectKey { get; private set; } = string.Empty;

    public FormAccessRights Rights { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static FormAccessRule Create(
        Guid formId,
        FormAccessSubjectType subjectType,
        string subjectKey,
        FormAccessRights rights,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(formId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        if (rights == FormAccessRights.None)
        {
            throw new ArgumentOutOfRangeException(nameof(rights));
        }

        string normalizedKey = subjectType switch
        {
            FormAccessSubjectType.Everyone => "*",
            FormAccessSubjectType.User when Guid.TryParse(subjectKey, out Guid userId) && userId != Guid.Empty =>
                userId.ToString("D"),
            FormAccessSubjectType.Role when !string.IsNullOrWhiteSpace(subjectKey) => subjectKey.Trim(),
            _ => throw new ArgumentException("The access-rule subject is invalid.", nameof(subjectKey))
        };

        return new FormAccessRule
        {
            Id = Guid.NewGuid(),
            FormId = formId,
            SubjectType = subjectType,
            SubjectKey = normalizedKey,
            Rights = rights,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc
        };
    }

    public void ChangeRights(FormAccessRights rights)
    {
        if (rights == FormAccessRights.None)
        {
            throw new ArgumentOutOfRangeException(nameof(rights));
        }

        Rights = rights;
    }
}
