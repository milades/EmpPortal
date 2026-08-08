namespace EmpPortal.Application.Forms;

public interface IFormReportingService
{
    public Task<PagedResult<SubmissionSummary>> GetSubmissionsAsync(
        Guid formId,
        SubmissionQuery query,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<SubmissionDetails?> GetSubmissionAsync(
        Guid submissionId,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<byte[]> ExportExcelAsync(
        Guid formId,
        SubmissionQuery query,
        FormActor actor,
        CancellationToken cancellationToken = default);

    public Task<byte[]> ExportPdfAsync(
        Guid submissionId,
        FormActor actor,
        CancellationToken cancellationToken = default);
}
