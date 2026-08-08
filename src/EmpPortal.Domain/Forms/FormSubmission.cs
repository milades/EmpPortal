namespace EmpPortal.Domain.Forms;

public sealed class FormSubmission
{
    private FormSubmission()
    {
    }

    public Guid Id { get; private set; }

    public Guid FormId { get; private set; }

    public Guid FormVersionId { get; private set; }

    public Guid SubmittedByUserId { get; private set; }

    public FormSubmissionStatus Status { get; private set; }

    public string DataJson { get; private set; } = string.Empty;

    public string TrackingCode { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? WithdrawnAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static FormSubmission CreateDraft(
        Guid formId,
        Guid formVersionId,
        Guid userId,
        string dataJson,
        string trackingCode,
        DateTimeOffset nowUtc)
    {
        Validate(formId, formVersionId, userId, dataJson, trackingCode);

        return new FormSubmission
        {
            Id = Guid.NewGuid(),
            FormId = formId,
            FormVersionId = formVersionId,
            SubmittedByUserId = userId,
            Status = FormSubmissionStatus.Draft,
            DataJson = dataJson,
            TrackingCode = trackingCode.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public void Save(string dataJson, bool allowSubmittedEdit, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataJson);
        if (Status == FormSubmissionStatus.Withdrawn)
        {
            throw new InvalidOperationException("A withdrawn submission cannot be changed.");
        }

        if (Status == FormSubmissionStatus.Submitted && !allowSubmittedEdit)
        {
            throw new InvalidOperationException("This form does not allow editing after submission.");
        }

        DataJson = dataJson;
        UpdatedAtUtc = nowUtc;
    }

    public void Submit(string dataJson, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataJson);
        if (Status != FormSubmissionStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft submission can be finalized.");
        }

        DataJson = dataJson;
        Status = FormSubmissionStatus.Submitted;
        SubmittedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Withdraw(DateTimeOffset nowUtc)
    {
        if (Status != FormSubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted response can be withdrawn.");
        }

        Status = FormSubmissionStatus.Withdrawn;
        WithdrawnAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(
        Guid formId,
        Guid formVersionId,
        Guid userId,
        string dataJson,
        string trackingCode)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(formId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(formVersionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);
    }
}
