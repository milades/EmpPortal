using EmpPortal.Application.Forms;
using EmpPortal.Application.Forms.Schema;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Forms;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Forms;

public sealed class FormManagementService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    TimeProvider timeProvider) : IFormManagementService
{
    private const FormAccessRights OwnerRights =
        FormAccessRights.View |
        FormAccessRights.Submit |
        FormAccessRights.Manage |
        FormAccessRights.ViewSubmissions |
        FormAccessRights.Export;

    public async Task<PagedResult<FormSummary>> GetFormsAsync(
        FormListQuery query,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        EnsureValidActor(actor);
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);

        IQueryable<FormDefinition> formsQuery = dbContext.Forms.AsNoTracking();
        if (!FormAuthorizationEvaluator.IsGlobalAdministrator(actor))
        {
            string userKey = actor.UserId.ToString("D");
            string[] roles = actor.Roles.ToArray();
            formsQuery = formsQuery.Where(form =>
                form.CreatedByUserId == actor.UserId ||
                dbContext.FormAccessRules.Any(rule =>
                    rule.FormId == form.Id &&
                    (rule.Rights & FormAccessRights.Manage) == FormAccessRights.Manage &&
                    (rule.SubjectType == FormAccessSubjectType.User && rule.SubjectKey == userKey ||
                     rule.SubjectType == FormAccessSubjectType.Role && roles.Contains(rule.SubjectKey))));
        }

        long totalCount = await formsQuery.LongCountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(Math.Min(totalCount, int.MaxValue) / (double)pageSize));
        page = Math.Min(page, totalPages);
        FormSummary[] items = await formsQuery
            .OrderByDescending(form => form.UpdatedAtUtc)
            .ThenByDescending(form => form.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(form => new FormSummary(
                form.Id,
                form.Slug,
                form.Title,
                form.Status,
                form.OpensAtUtc,
                form.ClosesAtUtc,
                dbContext.FormVersions
                    .Where(version => version.Id == form.CurrentPublishedVersionId)
                    .Select(version => (int?)version.VersionNumber)
                    .SingleOrDefault(),
                dbContext.FormVersions
                    .Where(version => version.FormId == form.Id && version.Status == FormVersionStatus.Draft)
                    .Select(version => version.VersionNumber)
                    .SingleOrDefault(),
                dbContext.FormSubmissions.LongCount(submission =>
                    submission.FormId == form.Id &&
                    submission.Status == FormSubmissionStatus.Submitted),
                form.UpdatedAtUtc,
                form.Status == FormLifecycleStatus.Draft &&
                !form.CurrentPublishedVersionId.HasValue &&
                !dbContext.FormSubmissions.Any(submission => submission.FormId == form.Id) &&
                !dbContext.FormVersions.Any(version =>
                    version.FormId == form.Id && version.Status != FormVersionStatus.Draft),
                form.RowVersion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<FormSummary>(items, totalCount, page, pageSize);
    }

    public async Task<FormEditorData?> GetEditorAsync(
        Guid formId,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition? form = await dbContext.Forms
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == formId, cancellationToken);
        if (form is null || !await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                FormAccessRights.Manage,
                cancellationToken))
        {
            return null;
        }

        FormVersion? draft = await dbContext.FormVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                version => version.FormId == formId && version.Status == FormVersionStatus.Draft,
                cancellationToken);
        if (draft is null)
        {
            return null;
        }

        FormAccessRule[] rules = await dbContext.FormAccessRules
            .AsNoTracking()
            .Where(rule => rule.FormId == formId)
            .OrderBy(rule => rule.SubjectType)
            .ThenBy(rule => rule.SubjectKey)
            .ToArrayAsync(cancellationToken);
        string[] userIds = rules
            .Where(rule => rule.SubjectType == FormAccessSubjectType.User)
            .Select(rule => rule.SubjectKey)
            .ToArray();
        Dictionary<string, string> userNames = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id.ToString()))
            .ToDictionaryAsync(
                user => user.Id.ToString(),
                user => user.DisplayName,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        FormAccessRuleData[] accessRules = rules.Select(rule => new FormAccessRuleData(
            rule.Id,
            rule.SubjectType,
            rule.SubjectKey,
            GetSubjectDisplayName(rule, userNames),
            rule.Rights)).ToArray();

        return new FormEditorData(
            form.Id,
            form.Slug,
            form.Title,
            form.Description,
            form.Status,
            form.OpensAtUtc,
            form.ClosesAtUtc,
            form.AllowDrafts,
            form.AllowEditAfterSubmit,
            form.MaxSubmissionsPerUser,
            draft.VersionNumber,
            FormSchemaSerializer.Deserialize(draft.DefinitionJson),
            accessRules,
            form.RowVersion.ToArray());
    }

    public async Task<IReadOnlyList<FormAccessSubjectOption>> GetAccessSubjectsAsync(
        string? search,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        if (!FormAuthorizationEvaluator.CanCreate(actor) && !FormAuthorizationEvaluator.CanPublish(actor))
        {
            throw new UnauthorizedAccessException("The current user cannot manage form access.");
        }

        string normalizedSearch = search?.Trim() ?? string.Empty;
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormAccessSubjectOption[] users = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsDirectoryEnabled &&
                (normalizedSearch == string.Empty ||
                 user.DisplayName.Contains(normalizedSearch) ||
                 (user.UserName != null && user.UserName.Contains(normalizedSearch))))
            .OrderBy(user => user.DisplayName)
            .Take(30)
            .Select(user => new FormAccessSubjectOption(
                FormAccessSubjectType.User,
                user.Id.ToString(),
                user.DisplayName + " (" + user.UserName + ")"))
            .ToArrayAsync(cancellationToken);
        FormAccessSubjectOption[] roles = await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Name != null &&
                (normalizedSearch == string.Empty || role.Name.Contains(normalizedSearch)))
            .OrderBy(role => role.Name)
            .Take(30)
            .Select(role => new FormAccessSubjectOption(
                FormAccessSubjectType.Role,
                role.Name!,
                "نقش: " + (role.Description ?? role.Name)))
            .ToArrayAsync(cancellationToken);

        return
        [
            new FormAccessSubjectOption(FormAccessSubjectType.Everyone, "*", "همه کاربران"),
            .. roles,
            .. users
        ];
    }

    public async Task<Guid> CreateAsync(
        CreateFormRequest request,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidActor(actor);
        if (!FormAuthorizationEvaluator.CanCreate(actor))
        {
            throw new UnauthorizedAccessException("The current user cannot create forms.");
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        FormDefinition form = FormDefinition.Create(
            request.Slug,
            request.Title,
            request.Description,
            actor.UserId,
            nowUtc);
        FormSchemaDefinition schema = CreateInitialSchema(request.Title, request.Description);
        string definitionJson = FormSchemaSerializer.Serialize(schema);
        FormVersion draft = FormVersion.CreateDraft(
            form.Id,
            1,
            definitionJson,
            FormSchemaSerializer.ComputeHash(definitionJson),
            actor.UserId,
            nowUtc);

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        if (await dbContext.Forms.AnyAsync(
                candidate => candidate.Slug == form.Slug,
                cancellationToken))
        {
            throw new ArgumentException("فرمی با این نشانی قبلاً ایجاد شده است.");
        }

        dbContext.Forms.Add(form);
        dbContext.FormVersions.Add(draft);
        dbContext.FormAccessRules.Add(FormAccessRule.Create(
            form.Id,
            FormAccessSubjectType.User,
            actor.UserId.ToString("D"),
            OwnerRights,
            actor.UserId,
            nowUtc));
        if (request.AvailableToEveryone)
        {
            dbContext.FormAccessRules.Add(FormAccessRule.Create(
                form.Id,
                FormAccessSubjectType.Everyone,
                "*",
                FormAccessRights.View | FormAccessRights.Submit,
                actor.UserId,
                nowUtc));
        }

        AddAudit(dbContext, actor, nowUtc, "FormCreated", form.Id.ToString("D"), new
        {
            form.Slug,
            form.Title
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new FormConcurrencyException(
                "فرم هم‌زمان توسط مدیر دیگری تغییر کرده است.",
                exception);
        }
        return form.Id;
    }

    public async Task SaveDraftAsync(
        Guid formId,
        FormSchemaDefinition schema,
        UpdateFormSettingsRequest settings,
        IReadOnlyList<FormAccessRuleData> accessRules,
        byte[] rowVersion,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(accessRules);
        ArgumentNullException.ThrowIfNull(rowVersion);
        EnsureValidActor(actor);

        FormSchemaValidationResult validation = FormSchemaValidator.Validate(schema);
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                string.Join(" | ", validation.Errors.Select(error => error.Message)),
                nameof(schema));
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition form = await dbContext.Forms.SingleOrDefaultAsync(
            candidate => candidate.Id == formId,
            cancellationToken) ?? throw new KeyNotFoundException("The form was not found.");
        if (!await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                FormAccessRights.Manage,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("The current user cannot edit this form.");
        }

        dbContext.Entry(form).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
        schema.Title = settings.Title.Trim();
        schema.Description = string.IsNullOrWhiteSpace(settings.Description)
            ? null
            : settings.Description.Trim();
        form.UpdateDetails(settings.Title, settings.Description, actor.UserId, nowUtc);
        form.ConfigureSchedule(settings.OpensAtUtc, settings.ClosesAtUtc, actor.UserId, nowUtc);
        form.ConfigureSubmissionPolicy(
            settings.AllowDrafts,
            settings.AllowEditAfterSubmit,
            settings.MaxSubmissionsPerUser,
            actor.UserId,
            nowUtc);

        FormVersion draft = await dbContext.FormVersions.SingleAsync(
            version => version.FormId == formId && version.Status == FormVersionStatus.Draft,
            cancellationToken);
        string definitionJson = FormSchemaSerializer.Serialize(schema);
        draft.ReplaceDefinition(
            definitionJson,
            FormSchemaSerializer.ComputeHash(definitionJson),
            actor.UserId,
            nowUtc);

        FormAccessRule[] existingRules = await dbContext.FormAccessRules
            .Where(rule => rule.FormId == formId)
            .ToArrayAsync(cancellationToken);
        dbContext.FormAccessRules.RemoveRange(existingRules);
        foreach (FormAccessRuleData rule in NormalizeRules(accessRules, form, actor))
        {
            dbContext.FormAccessRules.Add(FormAccessRule.Create(
                formId,
                rule.SubjectType,
                rule.SubjectKey,
                rule.Rights,
                actor.UserId,
                nowUtc));
        }

        AddAudit(dbContext, actor, nowUtc, "FormDraftSaved", form.Id.ToString("D"), new
        {
            draft.VersionNumber,
            draft.SchemaHash
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new FormConcurrencyException(
                "فرم هم‌زمان توسط مدیر دیگری تغییر کرده است.",
                exception);
        }
    }

    public Task PublishAsync(
        Guid formId,
        FormActor actor,
        CancellationToken cancellationToken = default) =>
        ChangeLifecycleAsync(formId, FormLifecycleAction.Publish, actor, cancellationToken);

    public Task PauseAsync(
        Guid formId,
        FormActor actor,
        CancellationToken cancellationToken = default) =>
        ChangeLifecycleAsync(formId, FormLifecycleAction.Pause, actor, cancellationToken);

    public Task ResumeAsync(
        Guid formId,
        FormActor actor,
        CancellationToken cancellationToken = default) =>
        ChangeLifecycleAsync(formId, FormLifecycleAction.Resume, actor, cancellationToken);

    public Task ArchiveAsync(
        Guid formId,
        FormActor actor,
        CancellationToken cancellationToken = default) =>
        ChangeLifecycleAsync(formId, FormLifecycleAction.Archive, actor, cancellationToken);

    public async Task DeleteAsync(
        Guid formId,
        byte[] rowVersion,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rowVersion);
        if (rowVersion.Length == 0)
        {
            throw new ArgumentException("توکن هم‌زمانی فرم الزامی است.", nameof(rowVersion));
        }

        EnsureValidActor(actor);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition form = await dbContext.Forms.SingleOrDefaultAsync(
            candidate => candidate.Id == formId,
            cancellationToken) ?? throw new KeyNotFoundException("فرم یافت نشد.");
        if (!await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                FormAccessRights.Manage,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("مجوز حذف این فرم را ندارید.");
        }

        if (form.Status != FormLifecycleStatus.Draft || form.CurrentPublishedVersionId.HasValue)
        {
            throw new InvalidOperationException(
                "فقط فرم پیش‌نویسی که هرگز منتشر نشده است قابل حذف است؛ فرم منتشرشده را بایگانی کنید.");
        }

        FormVersion[] versions = await dbContext.FormVersions
            .Where(version => version.FormId == formId)
            .ToArrayAsync(cancellationToken);
        if (versions.Any(version => version.Status != FormVersionStatus.Draft))
        {
            throw new InvalidOperationException(
                "این فرم سابقه انتشار دارد و برای حفظ سوابق قابل حذف نیست.");
        }

        if (await dbContext.FormSubmissions.AnyAsync(
                submission => submission.FormId == formId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "برای این فرم داده ثبت شده است و حذف آن مجاز نیست؛ فرم را بایگانی کنید.");
        }

        FormAccessRule[] accessRules = await dbContext.FormAccessRules
            .Where(rule => rule.FormId == formId)
            .ToArrayAsync(cancellationToken);
        dbContext.Entry(form).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
        dbContext.FormAccessRules.RemoveRange(accessRules);
        dbContext.FormVersions.RemoveRange(versions);
        dbContext.Forms.Remove(form);
        AddAudit(dbContext, actor, nowUtc, "FormDeleted", form.Id.ToString("D"), new
        {
            form.Slug,
            form.Title,
            VersionCount = versions.Length
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new FormConcurrencyException(
                "فرم هم‌زمان توسط مدیر دیگری تغییر کرده است؛ صفحه را بازآوری کنید.",
                exception);
        }
    }

    private async Task ChangeLifecycleAsync(
        Guid formId,
        FormLifecycleAction action,
        FormActor actor,
        CancellationToken cancellationToken)
    {
        EnsureValidActor(actor);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition form = await dbContext.Forms.SingleOrDefaultAsync(
            candidate => candidate.Id == formId,
            cancellationToken) ?? throw new KeyNotFoundException("The form was not found.");
        if (!await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                FormAccessRights.Manage,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("The current user cannot change this form.");
        }

        string eventType;
        switch (action)
        {
            case FormLifecycleAction.Publish:
                if (!FormAuthorizationEvaluator.CanPublish(actor))
                {
                    throw new UnauthorizedAccessException("The current user cannot publish forms.");
                }

                FormVersion draft = await dbContext.FormVersions.SingleAsync(
                    version => version.FormId == formId && version.Status == FormVersionStatus.Draft,
                    cancellationToken);
                FormSchemaValidationResult validation = FormSchemaValidator.Validate(
                    FormSchemaSerializer.Deserialize(draft.DefinitionJson));
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException("The draft schema is not valid for publication.");
                }

                if (form.CurrentPublishedVersionId.HasValue)
                {
                    FormVersion previous = await dbContext.FormVersions.SingleAsync(
                        version => version.Id == form.CurrentPublishedVersionId,
                        cancellationToken);
                    previous.Supersede(actor.UserId, nowUtc);
                }

                draft.Publish(actor.UserId, nowUtc);
                form.Publish(draft.Id, actor.UserId, nowUtc);
                dbContext.FormVersions.Add(FormVersion.CreateDraft(
                    form.Id,
                    draft.VersionNumber + 1,
                    draft.DefinitionJson,
                    draft.SchemaHash,
                    actor.UserId,
                    nowUtc));
                eventType = "FormPublished";
                break;
            case FormLifecycleAction.Pause:
                form.Pause(actor.UserId, nowUtc);
                eventType = "FormPaused";
                break;
            case FormLifecycleAction.Resume:
                form.Resume(actor.UserId, nowUtc);
                eventType = "FormResumed";
                break;
            case FormLifecycleAction.Archive:
                form.Archive(actor.UserId, nowUtc);
                eventType = "FormArchived";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        AddAudit(dbContext, actor, nowUtc, eventType, form.Id.ToString("D"), null);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new FormConcurrencyException(
                "فرم هم‌زمان توسط مدیر دیگری تغییر کرده است.",
                exception);
        }
    }

    private static FormSchemaDefinition CreateInitialSchema(string title, string? description) =>
        new()
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Pages =
            [
                new FormPageDefinition
                {
                    Title = "صفحه اول",
                    Sections =
                    [
                        new FormSectionDefinition
                        {
                            Title = "اطلاعات فرم"
                        }
                    ]
                }
            ]
        };

    private static List<FormAccessRuleData> NormalizeRules(
        IReadOnlyList<FormAccessRuleData> accessRules,
        FormDefinition form,
        FormActor actor)
    {
        List<FormAccessRuleData> normalized = accessRules
            .Where(rule => rule.Rights != FormAccessRights.None)
            .GroupBy(rule => new { rule.SubjectType, Key = rule.SubjectKey.ToUpperInvariant() })
            .Select(group => group.First())
            .ToList();
        string ownerKey = form.CreatedByUserId.ToString("D");
        int ownerIndex = normalized.FindIndex(rule =>
            rule.SubjectType == FormAccessSubjectType.User &&
            string.Equals(rule.SubjectKey, ownerKey, StringComparison.OrdinalIgnoreCase));
        FormAccessRuleData ownerRule = new(
            null,
            FormAccessSubjectType.User,
            ownerKey,
            actor.UserId == form.CreatedByUserId ? actor.Upn : "مالک فرم",
            OwnerRights);
        if (ownerIndex >= 0)
        {
            normalized[ownerIndex] = ownerRule;
        }
        else
        {
            normalized.Add(ownerRule);
        }

        return normalized;
    }

    private static string GetSubjectDisplayName(
        FormAccessRule rule,
        Dictionary<string, string> userNames) => rule.SubjectType switch
        {
            FormAccessSubjectType.Everyone => "همه کاربران",
            FormAccessSubjectType.Role => rule.SubjectKey,
            FormAccessSubjectType.User when userNames.TryGetValue(rule.SubjectKey, out string? name) => name,
            _ => rule.SubjectKey
        };

    private static void AddAudit(
        PortalDbContext dbContext,
        FormActor actor,
        DateTimeOffset nowUtc,
        string eventType,
        string subject,
        object? details)
    {
        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            eventType,
            "Succeeded",
            actor.UserId,
            actor.Upn,
            subject,
            actor.CorrelationId,
            actor.IpAddress,
            details is null ? null : System.Text.Json.JsonSerializer.Serialize(details)));
    }

    private static void EnsureValidActor(FormActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentOutOfRangeException.ThrowIfEqual(actor.UserId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.Upn);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.CorrelationId);
    }

    private enum FormLifecycleAction
    {
        Publish,
        Pause,
        Resume,
        Archive
    }
}
