using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmpPortal.Application.Forms.Schema;

public static partial class FormSubmissionValidator
{
    public static FormSchemaValidationResult Validate(
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, JsonElement> values)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(values);
        List<FormSchemaValidationError> errors = [];

        foreach (FormElementDefinition element in schema.Pages
                     .SelectMany(page => page.Sections)
                     .SelectMany(section => section.Elements))
        {
            ValidateElement(element, values, element.Key, errors);
        }

        return new FormSchemaValidationResult(errors);
    }

    public static bool IsVisible(
        FormElementDefinition element,
        IReadOnlyDictionary<string, JsonElement> values)
    {
        if (element.VisibilityCondition is null)
        {
            return true;
        }

        IEnumerable<bool> evaluations = element.VisibilityCondition.Rules.Select(rule =>
        {
            values.TryGetValue(rule.FieldKey, out JsonElement source);
            return EvaluateRule(source, rule);
        });

        return element.VisibilityCondition.Logic == FormConditionLogic.All
            ? evaluations.All(result => result)
            : evaluations.Any(result => result);
    }

    public static IEnumerable<FormElementDefinition> EnumerateElements(FormSchemaDefinition schema) =>
        schema.Pages.SelectMany(page => page.Sections)
            .SelectMany(section => section.Elements)
            .SelectMany(EnumerateElementTree);

    private static IEnumerable<FormElementDefinition> EnumerateElementTree(FormElementDefinition element)
    {
        yield return element;
        foreach (FormElementDefinition child in element.Children.SelectMany(EnumerateElementTree))
        {
            yield return child;
        }
    }

    private static void ValidateElement(
        FormElementDefinition element,
        IReadOnlyDictionary<string, JsonElement> values,
        string path,
        List<FormSchemaValidationError> errors)
    {
        if (IsContentElement(element.Type) || !IsVisible(element, values))
        {
            return;
        }

        values.TryGetValue(element.Key, out JsonElement value);
        bool isEmpty = IsEmpty(value);
        if (element.Required && isEmpty)
        {
            errors.Add(new(path, $"فیلد «{element.Label}» الزامی است."));
            return;
        }

        if (isEmpty)
        {
            return;
        }

        if (element.Type is FormElementType.Repeater or FormElementType.Table)
        {
            ValidateRows(element, value, path, errors);
            return;
        }

        ValidateValue(element, value, errors);
    }

    private static void ValidateValue(
        FormElementDefinition element,
        JsonElement value,
        List<FormSchemaValidationError> errors)
    {
        switch (element.Type)
        {
            case FormElementType.Text:
            case FormElementType.TextArea:
            case FormElementType.RichText:
            case FormElementType.Hidden:
            case FormElementType.CurrentUser:
                ValidateText(element, value, errors);
                break;
            case FormElementType.Email:
                ValidateText(element, value, errors);
                if (value.ValueKind == JsonValueKind.String &&
                    !new EmailAddressAttribute().IsValid(value.GetString()))
                {
                    AddInvalid(element, errors, "نشانی ایمیل معتبر نیست.");
                }

                break;
            case FormElementType.Phone:
                ValidateText(element, value, errors);
                if (value.ValueKind == JsonValueKind.String &&
                    !PhonePattern().IsMatch(value.GetString() ?? string.Empty))
                {
                    AddInvalid(element, errors, "شماره تماس معتبر نیست.");
                }

                break;
            case FormElementType.Url:
                ValidateText(element, value, errors);
                if (value.ValueKind != JsonValueKind.String ||
                    !Uri.TryCreate(value.GetString(), UriKind.Absolute, out Uri? uri) ||
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    AddInvalid(element, errors, "نشانی اینترنتی معتبر نیست.");
                }

                break;
            case FormElementType.NationalId:
                ValidateText(element, value, errors);
                if (value.ValueKind != JsonValueKind.String || !IsValidIranianNationalId(value.GetString()))
                {
                    AddInvalid(element, errors, "کد ملی معتبر نیست.");
                }

                break;
            case FormElementType.Number:
            case FormElementType.Currency:
            case FormElementType.Percentage:
            case FormElementType.Slider:
            case FormElementType.Calculated:
                ValidateNumber(element, value, errors);
                break;
            case FormElementType.Date:
            case FormElementType.DateTime:
                if (value.ValueKind != JsonValueKind.String ||
                    !DateTimeOffset.TryParse(
                        value.GetString(),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out _))
                {
                    AddInvalid(element, errors, "تاریخ معتبر نیست.");
                }

                break;
            case FormElementType.Time:
                if (value.ValueKind != JsonValueKind.String ||
                    !TimeOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    AddInvalid(element, errors, "زمان معتبر نیست.");
                }

                break;
            case FormElementType.DateRange:
                ValidateDateRange(element, value, errors);
                break;
            case FormElementType.Select:
            case FormElementType.Radio:
                ValidateSingleOption(element, value, errors);
                break;
            case FormElementType.MultiSelect:
                ValidateMultipleOptions(element, value, errors);
                break;
            case FormElementType.Checkbox:
            case FormElementType.Switch:
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    AddInvalid(element, errors, "مقدار این فیلد باید بله یا خیر باشد.");
                }

                break;
            case FormElementType.Repeater:
            case FormElementType.Table:
                break;
            case FormElementType.Heading:
            case FormElementType.Paragraph:
            case FormElementType.Divider:
            case FormElementType.Alert:
            default:
                break;
        }
    }

    private static void ValidateRows(
        FormElementDefinition element,
        JsonElement value,
        string path,
        List<FormSchemaValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            AddInvalid(element, errors, "داده جدولی معتبر نیست.");
            return;
        }

        int rowCount = value.GetArrayLength();
        if (element.Validation.MinimumLength.HasValue && rowCount < element.Validation.MinimumLength.Value)
        {
            AddInvalid(element, errors, $"حداقل {element.Validation.MinimumLength.Value} ردیف لازم است.");
        }

        if (element.Validation.MaximumLength.HasValue && rowCount > element.Validation.MaximumLength.Value)
        {
            AddInvalid(element, errors, $"حداکثر {element.Validation.MaximumLength.Value} ردیف مجاز است.");
        }

        int rowIndex = 0;
        foreach (JsonElement row in value.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new($"{path}[{rowIndex}]", "ساختار ردیف معتبر نیست."));
                rowIndex++;
                continue;
            }

            Dictionary<string, JsonElement> rowValues = row.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);
            foreach (FormElementDefinition child in element.Children)
            {
                ValidateElement(child, rowValues, $"{path}[{rowIndex}].{child.Key}", errors);
            }

            rowIndex++;
        }
    }

    private static void ValidateText(
        FormElementDefinition element,
        JsonElement value,
        List<FormSchemaValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            AddInvalid(element, errors, "مقدار متنی معتبر نیست.");
            return;
        }

        string text = value.GetString() ?? string.Empty;
        FormValidationDefinition validation = element.Validation;
        if (validation.MinimumLength.HasValue && text.Length < validation.MinimumLength)
        {
            AddInvalid(element, errors, $"حداقل طول مجاز {validation.MinimumLength} نویسه است.");
        }

        if (validation.MaximumLength.HasValue && text.Length > validation.MaximumLength)
        {
            AddInvalid(element, errors, $"حداکثر طول مجاز {validation.MaximumLength} نویسه است.");
        }

        if (!string.IsNullOrWhiteSpace(validation.Pattern))
        {
            try
            {
                if (!Regex.IsMatch(
                        text,
                        validation.Pattern,
                        RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(100)))
                {
                    AddInvalid(element, errors, "قالب مقدار واردشده صحیح نیست.");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                AddInvalid(element, errors, "اعتبارسنجی این فیلد در زمان مجاز انجام نشد.");
            }
        }
    }

    private static void ValidateNumber(
        FormElementDefinition element,
        JsonElement value,
        List<FormSchemaValidationError> errors)
    {
        if (!TryGetDecimal(value, out decimal number))
        {
            AddInvalid(element, errors, "مقدار عددی معتبر نیست.");
            return;
        }

        if (element.Validation.MinimumValue.HasValue && number < element.Validation.MinimumValue)
        {
            AddInvalid(element, errors, $"مقدار نباید کمتر از {element.Validation.MinimumValue} باشد.");
        }

        if (element.Validation.MaximumValue.HasValue && number > element.Validation.MaximumValue)
        {
            AddInvalid(element, errors, $"مقدار نباید بیشتر از {element.Validation.MaximumValue} باشد.");
        }
    }

    private static void ValidateDateRange(
        FormElementDefinition element,
        JsonElement value,
        List<FormSchemaValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("start", out JsonElement start) ||
            !value.TryGetProperty("end", out JsonElement end) ||
            start.ValueKind != JsonValueKind.String ||
            end.ValueKind != JsonValueKind.String ||
            !DateTimeOffset.TryParse(start.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset startDate) ||
            !DateTimeOffset.TryParse(end.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset endDate) ||
            startDate > endDate)
        {
            AddInvalid(element, errors, "بازه تاریخ معتبر نیست.");
        }
    }

    private static void ValidateSingleOption(
        FormElementDefinition element,
        JsonElement value,
        List<FormSchemaValidationError> errors)
    {
        string? selected = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (selected is null || !element.Options.Any(option =>
                string.Equals(option.Value, selected, StringComparison.OrdinalIgnoreCase)))
        {
            AddInvalid(element, errors, "گزینه انتخاب‌شده معتبر نیست.");
        }
    }

    private static void ValidateMultipleOptions(
        FormElementDefinition element,
        JsonElement value,
        List<FormSchemaValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            AddInvalid(element, errors, "گزینه‌های انتخاب‌شده معتبر نیستند.");
            return;
        }

        HashSet<string> allowed = element.Options
            .Select(option => option.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (value.EnumerateArray().Any(item =>
                item.ValueKind != JsonValueKind.String || !allowed.Contains(item.GetString() ?? string.Empty)))
        {
            AddInvalid(element, errors, "یکی از گزینه‌های انتخاب‌شده معتبر نیست.");
        }

        int selectionCount = value.GetArrayLength();
        if (element.Validation.MinimumLength.HasValue && selectionCount < element.Validation.MinimumLength.Value)
        {
            AddInvalid(element, errors, $"حداقل {element.Validation.MinimumLength.Value} گزینه باید انتخاب شود.");
        }

        if (element.Validation.MaximumLength.HasValue && selectionCount > element.Validation.MaximumLength.Value)
        {
            AddInvalid(element, errors, $"حداکثر {element.Validation.MaximumLength.Value} گزینه قابل انتخاب است.");
        }
    }

    private static bool EvaluateRule(JsonElement source, FormConditionRuleDefinition rule)
    {
        string? sourceText = ToComparableString(source);
        string? expected = rule.Value;
        if (rule.Operator == FormConditionOperator.IsEmpty)
        {
            return IsEmpty(source);
        }

        if (rule.Operator == FormConditionOperator.IsNotEmpty)
        {
            return !IsEmpty(source);
        }

        if (rule.Operator is FormConditionOperator.Contains or FormConditionOperator.NotContains)
        {
            bool contains = source.ValueKind == JsonValueKind.Array
                ? source.EnumerateArray().Any(item =>
                    string.Equals(ToComparableString(item), expected, StringComparison.OrdinalIgnoreCase))
                : sourceText?.Contains(expected ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true;
            return rule.Operator == FormConditionOperator.Contains ? contains : !contains;
        }

        if (rule.Operator is FormConditionOperator.Equals or FormConditionOperator.NotEquals)
        {
            bool equals = string.Equals(sourceText, expected, StringComparison.OrdinalIgnoreCase);
            return rule.Operator == FormConditionOperator.Equals ? equals : !equals;
        }

        if (!decimal.TryParse(sourceText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal left) ||
            !decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal right))
        {
            return false;
        }

        return rule.Operator switch
        {
            FormConditionOperator.GreaterThan => left > right,
            FormConditionOperator.GreaterThanOrEqual => left >= right,
            FormConditionOperator.LessThan => left < right,
            FormConditionOperator.LessThanOrEqual => left <= right,
            _ => false
        };
    }

    private static bool IsEmpty(JsonElement value) =>
        value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ||
        value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString()) ||
        value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0;

    private static string? ToComparableString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => bool.TrueString,
        JsonValueKind.False => bool.FalseString,
        _ => null
    };

    private static bool TryGetDecimal(JsonElement value, out decimal number) =>
        value.ValueKind == JsonValueKind.Number
            ? value.TryGetDecimal(out number)
            : decimal.TryParse(
                value.ValueKind == JsonValueKind.String ? value.GetString() : null,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out number);

    private static bool IsValidIranianNationalId(string? value)
    {
        if (value?.Length != 10 || !value.All(char.IsAsciiDigit) || value.Distinct().Count() == 1)
        {
            return false;
        }

        int sum = 0;
        for (int index = 0; index < 9; index++)
        {
            sum += (value[index] - '0') * (10 - index);
        }

        int remainder = sum % 11;
        int control = value[9] - '0';
        return remainder < 2 ? control == remainder : control == 11 - remainder;
    }

    private static bool IsContentElement(FormElementType type) =>
        type is FormElementType.Heading or FormElementType.Paragraph or FormElementType.Divider or
            FormElementType.Alert;

    private static void AddInvalid(
        FormElementDefinition element,
        List<FormSchemaValidationError> errors,
        string fallbackMessage) =>
        errors.Add(new(
            element.Key,
            string.IsNullOrWhiteSpace(element.Validation.CustomErrorMessage)
                ? fallbackMessage
                : element.Validation.CustomErrorMessage));

    [GeneratedRegex(@"^\+?[0-9\s\-()]{7,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
