using System.Text.Json;
using EmpPortal.Application.Forms;
using EmpPortal.Application.Forms.Schema;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Forms;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Forms;

public sealed class FormSubmissionService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    TimeProvider timeProvider) : IFormSubmissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaximumSubmissionJsonLength = 1_000_000;

    public async Task<IReadOnlyList<FormSummary>> GetAvailableFormsAsync(
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition[] candidates = await dbContext.Forms
            .AsNoTracking()
            .Where(form =>
                form.Status == FormLifecycleStatus.Published &&
                (!form.OpensAtUtc.HasValue || form.OpensAtUtc <= nowUtc) &&
                (!form.ClosesAtUtc.HasValue || form.ClosesAtUtc > nowUtc))
            .OrderByDescending(form => form.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);

        List<FormSummary> available = [];
        foreach (FormDefinition form in candidates)
        {
            if (!await FormAuthorizationEvaluator.HasRightsAsync(
                    dbContext,
                    form,
                    actor,
                    FormAccessRights.View | FormAccessRights.Submit,
                    cancellationToken))
            {
                continue;
            }

            int? publishedVersion = await dbContext.FormVersions
                .AsNoTracking()
                .Where(version => version.Id == form.CurrentPublishedVersionId)
                .Select(version => (int?)version.VersionNumber)
                .SingleOrDefaultAsync(cancellationToken);
            long submissionCount = await dbContext.FormSubmissions.LongCountAsync(
                submission =>
                    submission.FormId == form.Id &&
                    submission.SubmittedByUserId == actor.UserId &&
                    submission.Status == FormSubmissionStatus.Submitted,
                cancellationToken);
            available.Add(new FormSummary(
                form.Id,
                form.Slug,
                form.Title,
                form.Status,
                form.OpensAtUtc,
                form.ClosesAtUtc,
                publishedVersion,
                DraftVersion: 0,
                submissionCount,
                form.UpdatedAtUtc,
                CanPhysicallyDelete: false,
                RowVersion: []));
        }

        return available;
    }

    public async Task<FormRuntimeData?> GetRuntimeAsync(
        string slug,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        EnsureValidActor(actor);
        string normalizedSlug = slug.Trim().ToLowerInvariant();
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition? form = await dbContext.Forms.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Slug == normalizedSlug,
            cancellationToken);
        if (form is null || !form.IsAvailableAt(nowUtc) ||
            !await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                FormAccessRights.View | FormAccessRights.Submit,
                cancellationToken))
        {
            return null;
        }

        FormSubmission? existing = await dbContext.FormSubmissions
            .AsNoTracking()
            .Where(submission =>
                submission.FormId == form.Id &&
                submission.SubmittedByUserId == actor.UserId &&
                (submission.Status == FormSubmissionStatus.Draft ||
                 form.AllowEditAfterSubmit && submission.Status == FormSubmissionStatus.Submitted))
            .OrderBy(submission => submission.Status)
            .ThenByDescending(submission => submission.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        Guid versionId = existing?.FormVersionId ?? form.CurrentPublishedVersionId ?? Guid.Empty;
        if (versionId == Guid.Empty)
        {
            return null;
        }

        FormVersion version = await dbContext.FormVersions.AsNoTracking().SingleAsync(
            candidate => candidate.Id == versionId,
            cancellationToken);
        Dictionary<string, JsonElement> values = existing is null
            ? new(StringComparer.OrdinalIgnoreCase)
            : DeserializeValues(existing.DataJson);
        FormSchemaDefinition schema = FormSchemaSerializer.Deserialize(version.DefinitionJson);
        foreach (FormElementDefinition element in schema.Pages
                     .SelectMany(page => page.Sections)
                     .SelectMany(section => section.Elements)
                     .Where(element => element.Type == FormElementType.CurrentUser))
        {
            values[element.Key] = JsonSerializer.SerializeToElement(actor.Upn);
        }

        return new FormRuntimeData(
            form.Id,
            version.Id,
            version.VersionNumber,
            form.Slug,
            schema,
            form.AllowDrafts,
            form.AllowEditAfterSubmit,
            form.MaxSubmissionsPerUser,
            existing?.Id,
            existing?.Status,
            existing?.RowVersion.ToArray(),
            values);
    }

    public Task<SubmissionResult> SaveDraftAsync(
        SaveSubmissionRequest request,
        FormActor actor,
        CancellationToken cancellationToken = default) =>
        SaveAsync(request, actor, finalize: false, cancellationToken);

    public Task<SubmissionResult> SubmitAsync(
        SaveSubmissionRequest request,
        FormActor actor,
        CancellationToken cancellationToken = default) =>
        SaveAsync(request, actor, finalize: true, cancellationToken);

    private async Task<SubmissionResult> SaveAsync(
        SaveSubmissionRequest request,
        FormActor actor,
        bool finalize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidActor(actor);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormVersion version = await dbContext.FormVersions.SingleOrDefaultAsync(
            candidate => candidate.Id == request.FormVersionId,
            cancellationToken) ?? throw new KeyNotFoundException("نسخه فرم پیدا نشد.");
        FormDefinition form = await dbContext.Forms.SingleAsync(
            candidate => candidate.Id == version.FormId,
            cancellationToken);
        if (!form.IsAvailableAt(nowUtc) ||
            !await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                FormAccessRights.Submit,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("فرم در دسترس نیست یا کاربر نمی‌تواند آن را ارسال کند.");
        }

        if (!finalize && !form.AllowDrafts)
        {
            throw new InvalidOperationException("این فرم اجازه ارسال پاسخ‌های پیش‌نویس را نمی‌دهد.");
        }

        FormSchemaDefinition schema = FormSchemaSerializer.Deserialize(version.DefinitionJson);
        Dictionary<string, JsonElement> normalizedValues = NormalizeValues(schema, request.Values, actor);
        string dataJson = JsonSerializer.Serialize(normalizedValues, JsonOptions);
        if (dataJson.Length > MaximumSubmissionJsonLength)
        {
            throw new InvalidOperationException("پاسخ فرم از حداکثر اندازه مجاز فراتر می‌رود.");
        }

        FormSchemaValidationResult validation = finalize
            ? FormSubmissionValidator.Validate(schema, normalizedValues)
            : new FormSchemaValidationResult([]);
        if (!validation.IsValid)
        {
            return new SubmissionResult(
                request.SubmissionId ?? Guid.Empty,
                FormSubmissionStatus.Draft,
                string.Empty,
                nowUtc,
                null,
                validation.Errors);
        }

        FormSubmission? submission = null;
        bool isNew = !request.SubmissionId.HasValue;
        if (request.SubmissionId.HasValue)
        {
            submission = await dbContext.FormSubmissions.SingleOrDefaultAsync(
                candidate => candidate.Id == request.SubmissionId,
                cancellationToken);
            if (submission is null || submission.SubmittedByUserId != actor.UserId ||
                submission.FormId != form.Id || submission.FormVersionId != version.Id)
            {
                throw new UnauthorizedAccessException("پاسخ متعلق به کاربر فعلی نیست.");
            }

            if (request.RowVersion is null || request.RowVersion.Length == 0)
            {
                throw new InvalidOperationException("برای به‌روزرسانی پاسخ، به یک توکن همزمانی نیاز است.");
            }

            dbContext.Entry(submission).Property(candidate => candidate.RowVersion).OriginalValue = request.RowVersion;
        }

        if (submission is null)
        {
            bool hasOpenDraft = await dbContext.FormSubmissions.AnyAsync(
                candidate => candidate.FormId == form.Id &&
                    candidate.SubmittedByUserId == actor.UserId &&
                    candidate.Status == FormSubmissionStatus.Draft,
                cancellationToken);
            if (hasOpenDraft)
            {
                throw new InvalidOperationException(
                    "برای این فرم یک پیش‌نویس باز دارید؛ همان پیش‌نویس را ادامه دهید.");
            }

            await EnsureSubmissionLimitAsync(dbContext, form, actor.UserId, cancellationToken);
            submission = FormSubmission.CreateDraft(
                form.Id,
                version.Id,
                actor.UserId,
                dataJson,
                CreateTrackingCode(nowUtc),
                nowUtc);
            dbContext.FormSubmissions.Add(submission);
        }

        string eventType;
        if (finalize && submission.Status == FormSubmissionStatus.Draft)
        {
            submission.Submit(dataJson, nowUtc);
            eventType = "FormSubmissionCreated";
        }
        else
        {
            submission.Save(dataJson, form.AllowEditAfterSubmit, nowUtc);
            eventType = submission.Status == FormSubmissionStatus.Submitted
                ? "FormSubmissionUpdated"
                : "FormSubmissionDraftSaved";
        }

        if (submission.Status == FormSubmissionStatus.Submitted)
        {
            FormAnswerIndex[] previousAnswers = await dbContext.FormAnswerIndexes
                .Where(answer => answer.SubmissionId == submission.Id)
                .ToArrayAsync(cancellationToken);
            dbContext.FormAnswerIndexes.RemoveRange(previousAnswers);
            dbContext.FormAnswerIndexes.AddRange(
                FormAnswerIndexer.Create(submission.Id, schema, normalizedValues));
        }

        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            eventType,
            "Succeeded",
            actor.UserId,
            actor.Upn,
            submission.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new
            {
                form.Id,
                form.Slug,
                version.VersionNumber,
                IsNew = isNew,
                submission.TrackingCode
            })));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new FormConcurrencyException(
                "این پاسخ هم‌زمان در نشست دیگری تغییر کرده است.",
                exception);
        }

        return new SubmissionResult(
            submission.Id,
            submission.Status,
            submission.TrackingCode,
            submission.UpdatedAtUtc,
            submission.RowVersion.ToArray(),
            []);
    }

    private static Dictionary<string, JsonElement> NormalizeValues(
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, JsonElement> input,
        FormActor actor) => NormalizeElementCollection(
        schema.Pages.SelectMany(page => page.Sections).SelectMany(section => section.Elements).ToArray(),
        input,
        actor);

    private static Dictionary<string, JsonElement> NormalizeElementCollection(
        IReadOnlyList<FormElementDefinition> elements,
        IReadOnlyDictionary<string, JsonElement> input,
        FormActor actor)
    {
        Dictionary<string, JsonElement> normalized = new(StringComparer.OrdinalIgnoreCase);
        foreach (FormElementDefinition element in elements)
        {
            if (input.TryGetValue(element.Key, out JsonElement submittedValue))
            {
                normalized[element.Key] = submittedValue.Clone();
            }

            switch (element.Type)
            {
                case FormElementType.CurrentUser:
                    normalized[element.Key] = JsonSerializer.SerializeToElement(actor.Upn);
                    break;
                case FormElementType.Hidden:
                    if (element.DefaultValue is null)
                    {
                        normalized.Remove(element.Key);
                    }
                    else
                    {
                        normalized[element.Key] = CreateDefaultValue(element);
                    }

                    break;
                case FormElementType.Repeater:
                case FormElementType.Table:
                    if (submittedValue.ValueKind == JsonValueKind.Array)
                    {
                        List<Dictionary<string, JsonElement>> rows = [];
                        foreach (JsonElement row in submittedValue.EnumerateArray())
                        {
                            if (row.ValueKind != JsonValueKind.Object)
                            {
                                continue;
                            }

                            Dictionary<string, JsonElement> rowValues = row.EnumerateObject()
                                .ToDictionary(
                                    property => property.Name,
                                    property => property.Value,
                                    StringComparer.OrdinalIgnoreCase);
                            rows.Add(NormalizeElementCollection(element.Children, rowValues, actor));
                        }

                        normalized[element.Key] = JsonSerializer.SerializeToElement(rows);
                    }

                    break;
                case FormElementType.Heading:
                case FormElementType.Paragraph:
                case FormElementType.Divider:
                case FormElementType.Alert:
                    normalized.Remove(element.Key);
                    break;
                default:
                    if (!normalized.ContainsKey(element.Key) && element.DefaultValue is not null)
                    {
                        normalized[element.Key] = CreateDefaultValue(element);
                    }

                    break;
            }
        }

        foreach (FormElementDefinition element in elements.Where(candidate =>
                     candidate.Type == FormElementType.Calculated))
        {
            FormCalculationResult calculation = FormCalculationEngine.Evaluate(
                element.CalculationExpression ?? string.Empty,
                normalized);
            if (!calculation.Succeeded)
            {
                throw new InvalidOperationException(calculation.Error);
            }

            normalized[element.Key] = JsonSerializer.SerializeToElement(calculation.Value);
        }

        foreach (FormElementDefinition element in elements)
        {
            if (!FormSubmissionValidator.IsVisible(element, normalized))
            {
                normalized.Remove(element.Key);
            }
        }

        return normalized;
    }

    private static JsonElement CreateDefaultValue(FormElementDefinition element)
    {
        string defaultValue = element.DefaultValue ?? string.Empty;
        if (element.Type is FormElementType.Checkbox or FormElementType.Switch &&
            bool.TryParse(defaultValue, out bool booleanValue))
        {
            return JsonSerializer.SerializeToElement(booleanValue);
        }

        if (element.Type is FormElementType.Number or FormElementType.Currency or
            FormElementType.Percentage or FormElementType.Slider &&
            decimal.TryParse(
                defaultValue,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal decimalValue))
        {
            return JsonSerializer.SerializeToElement(decimalValue);
        }

        if (element.Type == FormElementType.MultiSelect)
        {
            return JsonSerializer.SerializeToElement(defaultValue.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return JsonSerializer.SerializeToElement(defaultValue);
    }

    private static async Task EnsureSubmissionLimitAsync(
        PortalDbContext dbContext,
        FormDefinition form,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!form.MaxSubmissionsPerUser.HasValue)
        {
            return;
        }

        int count = await dbContext.FormSubmissions.CountAsync(
            submission =>
                submission.FormId == form.Id &&
                submission.SubmittedByUserId == userId &&
                submission.Status == FormSubmissionStatus.Submitted,
            cancellationToken);
        if (count >= form.MaxSubmissionsPerUser.Value)
        {
            throw new InvalidOperationException("کاربر به محدودیت ارسال برای این فرم رسیده است.");
        }
    }

    private static Dictionary<string, JsonElement> DeserializeValues(string dataJson) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(dataJson, JsonOptions) ??
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    private static string CreateTrackingCode(DateTimeOffset nowUtc)
    {
        string random = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        return $"F-{nowUtc:yyyyMMdd}-{random}";
    }

    private static void EnsureValidActor(FormActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentOutOfRangeException.ThrowIfEqual(actor.UserId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.Upn);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.CorrelationId);
    }
}
