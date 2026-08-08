namespace EmpPortal.Domain.Forms;

public sealed class FormAnswerIndex
{
    private FormAnswerIndex()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubmissionId { get; private set; }

    public Guid FieldId { get; private set; }

    public string FieldName { get; private set; } = string.Empty;

    public string FieldType { get; private set; } = string.Empty;

    public int Sequence { get; private set; }

    public string? StringValue { get; private set; }

    public decimal? DecimalValue { get; private set; }

    public DateTimeOffset? DateTimeValue { get; private set; }

    public bool? BooleanValue { get; private set; }

    public static FormAnswerIndex Create(
        Guid submissionId,
        Guid fieldId,
        string fieldName,
        string fieldType,
        int sequence,
        string? stringValue,
        decimal? decimalValue,
        DateTimeOffset? dateTimeValue,
        bool? booleanValue)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(submissionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(fieldId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldType);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);

        return new FormAnswerIndex
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FieldId = fieldId,
            FieldName = fieldName.Trim(),
            FieldType = fieldType.Trim(),
            Sequence = sequence,
            StringValue = stringValue,
            DecimalValue = decimalValue,
            DateTimeValue = dateTimeValue,
            BooleanValue = booleanValue
        };
    }
}
