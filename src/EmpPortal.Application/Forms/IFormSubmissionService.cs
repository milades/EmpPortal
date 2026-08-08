namespace EmpPortal.Application.Forms;

public interface IFormSubmissionService
{
    public Task<IReadOnlyList<FormSummary>> GetAvailableFormsAsync(
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<FormRuntimeData?> GetRuntimeAsync(
        string slug,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<SubmissionResult> SaveDraftAsync(
        SaveSubmissionRequest request,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<SubmissionResult> SubmitAsync(
        SaveSubmissionRequest request,
        FormActor actor,
        CancellationToken cancellationToken = default);
}
