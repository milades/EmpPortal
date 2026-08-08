using System.Text.Json;
using EmpPortal.Application.Forms.Schema;
using EmpPortal.Domain.Forms;

namespace EmpPortal.Application.Forms;

public sealed record CreateFormRequest(
    string Slug,
    string Title,
    string? Description,
    bool AvailableToEveryone);

public sealed record FormListQuery(int Page, int PageSize);

public sealed record UpdateFormSettingsRequest(
    string Title,
    string? Description,
    DateTimeOffset? OpensAtUtc,
    DateTimeOffset? ClosesAtUtc,
    bool AllowDrafts,
    bool AllowEditAfterSubmit,
    int? MaxSubmissionsPerUser);

public sealed record FormSummary(
    Guid Id,
    string Slug,
    string Title,
    FormLifecycleStatus Status,
    DateTimeOffset? OpensAtUtc,
    DateTimeOffset? ClosesAtUtc,
    int? PublishedVersion,
    int DraftVersion,
    long SubmissionCount,
    DateTimeOffset UpdatedAtUtc,
    bool CanPhysicallyDelete,
    byte[] RowVersion);

public sealed record FormEditorData(
    Guid FormId,
    string Slug,
    string Title,
    string? Description,
    FormLifecycleStatus Status,
    DateTimeOffset? OpensAtUtc,
    DateTimeOffset? ClosesAtUtc,
    bool AllowDrafts,
    bool AllowEditAfterSubmit,
    int? MaxSubmissionsPerUser,
    int DraftVersion,
    FormSchemaDefinition Schema,
    IReadOnlyList<FormAccessRuleData> AccessRules,
    byte[] RowVersion);

public sealed record FormAccessRuleData(
    Guid? Id,
    FormAccessSubjectType SubjectType,
    string SubjectKey,
    string SubjectDisplayName,
    FormAccessRights Rights);

public sealed record FormAccessSubjectOption(
    FormAccessSubjectType SubjectType,
    string SubjectKey,
    string DisplayName);

public sealed record FormRuntimeData(
    Guid FormId,
    Guid FormVersionId,
    int VersionNumber,
    string Slug,
    FormSchemaDefinition Schema,
    bool AllowDrafts,
    bool AllowEditAfterSubmit,
    int? MaxSubmissionsPerUser,
    Guid? ExistingSubmissionId,
    FormSubmissionStatus? ExistingSubmissionStatus,
    byte[]? ExistingSubmissionRowVersion,
    IReadOnlyDictionary<string, JsonElement> ExistingValues);

public sealed record SaveSubmissionRequest(
    Guid? SubmissionId,
    Guid FormVersionId,
    IReadOnlyDictionary<string, JsonElement> Values,
    byte[]? RowVersion);

public sealed record SubmissionResult(
    Guid SubmissionId,
    FormSubmissionStatus Status,
    string TrackingCode,
    DateTimeOffset UpdatedAtUtc,
    byte[]? RowVersion,
    IReadOnlyList<FormSchemaValidationError> Errors);

public sealed record SubmissionSummary(
    Guid Id,
    string TrackingCode,
    string UserUpn,
    string UserDisplayName,
    FormSubmissionStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc);

public sealed record SubmissionDetails(
    Guid Id,
    Guid FormId,
    string TrackingCode,
    string UserUpn,
    string UserDisplayName,
    FormSubmissionStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    FormSchemaDefinition Schema,
    IReadOnlyDictionary<string, JsonElement> Values);

public sealed record SubmissionQuery(
    int Page,
    int PageSize,
    string? Search,
    FormSubmissionStatus? Status,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize);
