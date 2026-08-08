namespace EmpPortal.Application.Forms.Schema;

public sealed record FormSchemaValidationError(string Path, string Message);

public sealed record FormSchemaValidationResult(IReadOnlyList<FormSchemaValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
