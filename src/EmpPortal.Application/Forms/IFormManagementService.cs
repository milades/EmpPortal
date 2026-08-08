using EmpPortal.Application.Forms.Schema;

namespace EmpPortal.Application.Forms;

public interface IFormManagementService
{
    public Task<PagedResult<FormSummary>> GetFormsAsync(
        FormListQuery query,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<FormEditorData?> GetEditorAsync(
        Guid formId,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<FormAccessSubjectOption>> GetAccessSubjectsAsync(
        string? search,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<Guid> CreateAsync(
        CreateFormRequest request,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task SaveDraftAsync(
        Guid formId,
        FormSchemaDefinition schema,
        UpdateFormSettingsRequest settings,
        IReadOnlyList<FormAccessRuleData> accessRules,
        byte[] rowVersion,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task PublishAsync(Guid formId, FormActor actor, CancellationToken cancellationToken = default);

    public Task PauseAsync(Guid formId, FormActor actor, CancellationToken cancellationToken = default);

    public Task ResumeAsync(Guid formId, FormActor actor, CancellationToken cancellationToken = default);

    public Task ArchiveAsync(Guid formId, FormActor actor, CancellationToken cancellationToken = default);

    public Task DeleteAsync(
        Guid formId,
        byte[] rowVersion,
        FormActor actor,
        CancellationToken cancellationToken = default);
}
