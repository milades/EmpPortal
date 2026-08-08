using System.Globalization;
using System.Text.Json;
using EmpPortal.Application.Forms.Schema;
using EmpPortal.Domain.Forms;

namespace EmpPortal.Infrastructure.Forms;

internal static class FormAnswerIndexer
{
    public static IReadOnlyList<FormAnswerIndex> Create(
        Guid submissionId,
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, JsonElement> values)
    {
        List<FormAnswerIndex> answers = [];
        foreach (FormElementDefinition element in schema.Pages
                     .SelectMany(page => page.Sections)
                     .SelectMany(section => section.Elements))
        {
            if (!values.TryGetValue(element.Key, out JsonElement value))
            {
                continue;
            }

            if (element.Type is FormElementType.Repeater or FormElementType.Table)
            {
                IndexRows(submissionId, element, value, answers);
            }
            else
            {
                IndexValue(submissionId, element, value, sequence: 0, answers);
            }
        }

        return answers;
    }

    private static void IndexRows(
        Guid submissionId,
        FormElementDefinition container,
        JsonElement value,
        List<FormAnswerIndex> answers)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        int rowIndex = 0;
        foreach (JsonElement row in value.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
            {
                foreach (FormElementDefinition child in container.Children)
                {
                    if (row.TryGetProperty(child.Key, out JsonElement childValue))
                    {
                        // Reserve a deterministic sequence range for every row so multi-select
                        // and date-range values in separate rows never share the same index key.
                        IndexValue(submissionId, child, childValue, rowIndex * 1_000, answers);
                    }
                }
            }

            rowIndex++;
        }
    }

    private static void IndexValue(
        Guid submissionId,
        FormElementDefinition element,
        JsonElement value,
        int sequence,
        List<FormAnswerIndex> answers)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (element.Type == FormElementType.MultiSelect && value.ValueKind == JsonValueKind.Array)
        {
            int itemSequence = sequence;
            foreach (JsonElement item in value.EnumerateArray())
            {
                AddAnswer(submissionId, element, item, itemSequence++, answers);
            }

            return;
        }

        if (element.Type == FormElementType.DateRange && value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("start", out JsonElement start))
            {
                AddAnswer(submissionId, element, start, sequence * 2, answers);
            }

            if (value.TryGetProperty("end", out JsonElement end))
            {
                AddAnswer(submissionId, element, end, (sequence * 2) + 1, answers);
            }

            return;
        }

        AddAnswer(submissionId, element, value, sequence, answers);
    }

    private static void AddAnswer(
        Guid submissionId,
        FormElementDefinition element,
        JsonElement value,
        int sequence,
        List<FormAnswerIndex> answers)
    {
        string? stringValue = null;
        decimal? decimalValue = null;
        DateTimeOffset? dateTimeValue = null;
        bool? booleanValue = null;

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                stringValue = Truncate(value.GetString(), 700);
                if (element.Type is FormElementType.Date or FormElementType.DateTime or FormElementType.DateRange &&
                    DateTimeOffset.TryParse(
                        stringValue,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTimeOffset parsedDate))
                {
                    dateTimeValue = parsedDate;
                }

                if (element.Type is FormElementType.Number or FormElementType.Currency or
                    FormElementType.Percentage or FormElementType.Slider or FormElementType.Calculated &&
                    decimal.TryParse(
                        stringValue,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal parsedDecimal))
                {
                    decimalValue = parsedDecimal;
                }

                break;
            case JsonValueKind.Number:
                if (value.TryGetDecimal(out decimal number))
                {
                    decimalValue = number;
                }

                stringValue = Truncate(value.GetRawText(), 700);
                break;
            case JsonValueKind.True:
                booleanValue = true;
                stringValue = bool.TrueString;
                break;
            case JsonValueKind.False:
                booleanValue = false;
                stringValue = bool.FalseString;
                break;
            default:
                stringValue = Truncate(value.GetRawText(), 700);
                break;
        }

        answers.Add(FormAnswerIndex.Create(
            submissionId,
            element.Id,
            element.Key,
            element.Type.ToString(),
            sequence,
            stringValue,
            decimalValue,
            dateTimeValue,
            booleanValue));
    }

    private static string? Truncate(string? value, int maximumLength) =>
        value is null || value.Length <= maximumLength ? value : value[..maximumLength];
}
