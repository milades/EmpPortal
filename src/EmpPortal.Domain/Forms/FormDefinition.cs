namespace EmpPortal.Domain.Forms;

public sealed class FormDefinition
{
    private FormDefinition()
    {
    }

    public Guid Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public FormLifecycleStatus Status { get; private set; }

    public Guid? CurrentPublishedVersionId { get; private set; }

    public DateTimeOffset? OpensAtUtc { get; private set; }

    public DateTimeOffset? ClosesAtUtc { get; private set; }

    public bool AllowDrafts { get; private set; }

    public bool AllowEditAfterSubmit { get; private set; }

    public int? MaxSubmissionsPerUser { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static FormDefinition Create(
        string slug,
        string title,
        string? description,
        Guid createdByUserId,
        DateTimeOffset nowUtc)
    {
        ValidateIdentity(createdByUserId);
        string normalizedSlug = NormalizeSlug(slug);
        string normalizedTitle = NormalizeRequired(title, nameof(title));

        return new FormDefinition
        {
            Id = Guid.NewGuid(),
            Slug = normalizedSlug,
            Title = normalizedTitle,
            Description = NormalizeOptional(description),
            Status = FormLifecycleStatus.Draft,
            AllowDrafts = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = createdByUserId,
            UpdatedAtUtc = nowUtc
        };
    }

    public void UpdateDetails(
        string title,
        string? description,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        EnsureNotArchived();
        ValidateIdentity(actorUserId);
        Title = NormalizeRequired(title, nameof(title));
        Description = NormalizeOptional(description);
        RecordUpdate(actorUserId, nowUtc);
    }

    public void ConfigureSchedule(
        DateTimeOffset? opensAtUtc,
        DateTimeOffset? closesAtUtc,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        EnsureNotArchived();
        ValidateIdentity(actorUserId);
        if (opensAtUtc.HasValue && closesAtUtc.HasValue && opensAtUtc >= closesAtUtc)
        {
            throw new ArgumentException("زمان بسته شدن باید بعد از زمان باز شدن باشد.");
        }

        OpensAtUtc = opensAtUtc;
        ClosesAtUtc = closesAtUtc;
        RecordUpdate(actorUserId, nowUtc);
    }

    public void ConfigureSubmissionPolicy(
        bool allowDrafts,
        bool allowEditAfterSubmit,
        int? maxSubmissionsPerUser,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        EnsureNotArchived();
        ValidateIdentity(actorUserId);
        if (maxSubmissionsPerUser is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSubmissionsPerUser),
                "محدودیت ارسال باید هنگام پیکربندی مثبت باشد.");
        }

        AllowDrafts = allowDrafts;
        AllowEditAfterSubmit = allowEditAfterSubmit;
        MaxSubmissionsPerUser = maxSubmissionsPerUser;
        RecordUpdate(actorUserId, nowUtc);
    }

    public void Publish(Guid versionId, Guid actorUserId, DateTimeOffset nowUtc)
    {
        EnsureNotArchived();
        ValidateIdentity(actorUserId);
        ArgumentOutOfRangeException.ThrowIfEqual(versionId, Guid.Empty);

        CurrentPublishedVersionId = versionId;
        Status = FormLifecycleStatus.Published;
        RecordUpdate(actorUserId, nowUtc);
    }

    public void Pause(Guid actorUserId, DateTimeOffset nowUtc)
    {
        if (Status != FormLifecycleStatus.Published)
        {
            throw new InvalidOperationException("فقط فرم منتشر شده را می‌توان متوقف کرد.");
        }

        ValidateIdentity(actorUserId);
        Status = FormLifecycleStatus.Paused;
        RecordUpdate(actorUserId, nowUtc);
    }

    public void Resume(Guid actorUserId, DateTimeOffset nowUtc)
    {
        if (Status != FormLifecycleStatus.Paused || !CurrentPublishedVersionId.HasValue)
        {
            throw new InvalidOperationException("فقط فرم منتشر شده‌ای که متوقف شده است را می‌توان از سر گرفت.");
        }

        ValidateIdentity(actorUserId);
        Status = FormLifecycleStatus.Published;
        RecordUpdate(actorUserId, nowUtc);
    }

    public void Archive(Guid actorUserId, DateTimeOffset nowUtc)
    {
        ValidateIdentity(actorUserId);
        Status = FormLifecycleStatus.Archived;
        RecordUpdate(actorUserId, nowUtc);
    }

    public bool IsAvailableAt(DateTimeOffset nowUtc) =>
        Status == FormLifecycleStatus.Published &&
        (!OpensAtUtc.HasValue || nowUtc >= OpensAtUtc.Value) &&
        (!ClosesAtUtc.HasValue || nowUtc < ClosesAtUtc.Value);

    private static string NormalizeSlug(string slug)
    {
        string normalized = NormalizeRequired(slug, nameof(slug)).ToLowerInvariant();
        bool isValid = normalized.Length <= 120 &&
            normalized[0] is >= 'a' and <= 'z' &&
            normalized.All(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
        if (!isValid)
        {
            throw new ArgumentException(
                "نام باید با یک حرف شروع شود و فقط شامل حروف کوچک لاتین، اعداد یا خط فاصله باشد.",
                nameof(slug));
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateIdentity(Guid userId) =>
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

    private void EnsureNotArchived()
    {
        if (Status == FormLifecycleStatus.Archived)
        {
            throw new InvalidOperationException("فرم بایگانی شده قابل تغییر نیست.");
        }
    }

    private void RecordUpdate(Guid actorUserId, DateTimeOffset nowUtc)
    {
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }
}
