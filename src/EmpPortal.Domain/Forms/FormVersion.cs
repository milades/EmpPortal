namespace EmpPortal.Domain.Forms;

public sealed class FormVersion
{
    private FormVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid FormId { get; private set; }

    public int VersionNumber { get; private set; }

    public FormVersionStatus Status { get; private set; }

    public string DefinitionJson { get; private set; } = string.Empty;

    public string SchemaHash { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static FormVersion CreateDraft(
        Guid formId,
        int versionNumber,
        string definitionJson,
        string schemaHash,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        Validate(formId, versionNumber, definitionJson, schemaHash, actorUserId);

        return new FormVersion
        {
            Id = Guid.NewGuid(),
            FormId = formId,
            VersionNumber = versionNumber,
            Status = FormVersionStatus.Draft,
            DefinitionJson = definitionJson,
            SchemaHash = schemaHash,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc
        };
    }

    public void ReplaceDefinition(
        string definitionJson,
        string schemaHash,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        EnsureDraft();
        Validate(FormId, VersionNumber, definitionJson, schemaHash, actorUserId);
        DefinitionJson = definitionJson;
        SchemaHash = schemaHash;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    public void Publish(Guid actorUserId, DateTimeOffset nowUtc)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        Status = FormVersionStatus.Published;
        PublishedAtUtc = nowUtc;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    public void Supersede(Guid actorUserId, DateTimeOffset nowUtc)
    {
        if (Status != FormVersionStatus.Published)
        {
            throw new InvalidOperationException("Only a published version can be superseded.");
        }

        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        Status = FormVersionStatus.Superseded;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(
        Guid formId,
        int versionNumber,
        string definitionJson,
        string schemaHash,
        Guid actorUserId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(formId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaHash);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
    }

    private void EnsureDraft()
    {
        if (Status != FormVersionStatus.Draft)
        {
            throw new InvalidOperationException("A published form version is immutable.");
        }
    }
}
