using System.Text.RegularExpressions;

namespace EmpPortal.Application.Forms.Schema;

public static partial class FormSchemaValidator
{
    public const int MaximumPages = 20;
    public const int MaximumSectionsPerPage = 50;
    public const int MaximumElements = 300;
    public const int MaximumOptionsPerElement = 200;

    public static FormSchemaValidationResult Validate(FormSchemaDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        List<FormSchemaValidationError> errors = [];

        if (definition.SchemaVersion != FormSchemaDefinition.CurrentSchemaVersion)
        {
            errors.Add(new("schemaVersion", "نسخه ساختار فرم پشتیبانی نمی‌شود."));
        }

        ValidateRequiredText(definition.Title, 200, "title", "عنوان فرم", errors);
        ValidateOptionalText(definition.Description, 2000, "description", "توضیحات فرم", errors);
        ValidateRequiredText(
            definition.SubmitButtonText,
            80,
            "submitButtonText",
            "متن دکمه ثبت",
            errors);

        if (definition.Pages.Count is 0 or > MaximumPages)
        {
            errors.Add(new("pages", $"فرم باید بین ۱ تا {MaximumPages} صفحه داشته باشد."));
        }

        HashSet<Guid> ids = [];
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        List<(FormElementDefinition Element, string Path)> elements = [];

        for (int pageIndex = 0; pageIndex < definition.Pages.Count; pageIndex++)
        {
            FormPageDefinition page = definition.Pages[pageIndex];
            string pagePath = $"pages[{pageIndex}]";
            ValidateId(page.Id, $"{pagePath}.id", ids, errors);
            ValidateRequiredText(page.Title, 200, $"{pagePath}.title", "عنوان صفحه", errors);
            ValidateOptionalText(page.Description, 2000, $"{pagePath}.description", "توضیحات صفحه", errors);
            if (page.Sections.Count is 0 or > MaximumSectionsPerPage)
            {
                errors.Add(new(
                    $"{pagePath}.sections",
                    $"هر صفحه باید بین ۱ تا {MaximumSectionsPerPage} بخش داشته باشد."));
            }

            for (int sectionIndex = 0; sectionIndex < page.Sections.Count; sectionIndex++)
            {
                FormSectionDefinition section = page.Sections[sectionIndex];
                string sectionPath = $"{pagePath}.sections[{sectionIndex}]";
                ValidateId(section.Id, $"{sectionPath}.id", ids, errors);
                ValidateOptionalText(section.Title, 200, $"{sectionPath}.title", "عنوان بخش", errors);
                ValidateOptionalText(section.Description, 2000, $"{sectionPath}.description", "توضیحات بخش", errors);
                if (section.Columns is < 1 or > 12)
                {
                    errors.Add(new($"{sectionPath}.columns", "تعداد ستون بخش باید بین ۱ تا ۱۲ باشد."));
                }

                for (int elementIndex = 0; elementIndex < section.Elements.Count; elementIndex++)
                {
                    CollectAndValidateElement(
                        section.Elements[elementIndex],
                        $"{sectionPath}.elements[{elementIndex}]",
                        depth: 0,
                        ids,
                        keys,
                        elements,
                        errors);
                }
            }
        }

        if (elements.Count > MaximumElements)
        {
            errors.Add(new("pages", $"تعداد فیلدهای فرم نمی‌تواند بیشتر از {MaximumElements} باشد."));
        }

        ValidateReferences(elements, keys, errors);
        ValidateCalculations(elements, keys, errors);
        return new FormSchemaValidationResult(errors);
    }

    private static void CollectAndValidateElement(
        FormElementDefinition element,
        string path,
        int depth,
        HashSet<Guid> ids,
        HashSet<string> keys,
        List<(FormElementDefinition Element, string Path)> elements,
        List<FormSchemaValidationError> errors)
    {
        ValidateId(element.Id, $"{path}.id", ids, errors);
        if (element.Width is < 1 or > 12)
        {
            errors.Add(new($"{path}.width", "عرض فیلد باید بین ۱ تا ۱۲ باشد."));
        }

        bool isContentElement = IsContentElement(element.Type);
        if (isContentElement)
        {
            ValidateRequiredText(element.Content, 4000, $"{path}.content", "محتوای نمایشی", errors);
        }
        else
        {
            ValidateRequiredText(element.Label, 200, $"{path}.label", "عنوان فیلد", errors);
            if (string.IsNullOrWhiteSpace(element.Key) || !FieldKeyPattern().IsMatch(element.Key))
            {
                errors.Add(new(
                    $"{path}.key",
                    "کلید فیلد باید با حرف لاتین آغاز شود و فقط شامل حرف، عدد و underscore باشد."));
            }
            else if (!keys.Add(element.Key))
            {
                errors.Add(new($"{path}.key", "کلید فیلد در فرم تکراری است."));
            }
        }

        ValidateOptionalText(element.HelpText, 1000, $"{path}.helpText", "متن راهنما", errors);
        ValidateOptionalText(element.Placeholder, 300, $"{path}.placeholder", "متن نمونه", errors);
        ValidateOptionalText(element.DefaultValue, 4000, $"{path}.defaultValue", "مقدار پیش‌فرض", errors);

        if (RequiresOptions(element.Type))
        {
            ValidateOptions(element, path, errors);
        }

        ValidateValidationRules(element.Validation, path, errors);
        elements.Add((element, path));

        if (element.Type is FormElementType.Repeater or FormElementType.Table)
        {
            if (depth >= 1)
            {
                errors.Add(new($"{path}.children", "جدول یا Repeater تو در تو مجاز نیست."));
            }

            if (element.Children.Count == 0)
            {
                errors.Add(new($"{path}.children", "جدول یا Repeater باید حداقل یک فیلد داشته باشد."));
            }
        }
        else if (element.Children.Count > 0)
        {
            errors.Add(new($"{path}.children", "این نوع المنت امکان فیلد فرزند ندارد."));
        }

        for (int childIndex = 0; childIndex < element.Children.Count; childIndex++)
        {
            CollectAndValidateElement(
                element.Children[childIndex],
                $"{path}.children[{childIndex}]",
                depth + 1,
                ids,
                keys,
                elements,
                errors);
        }
    }

    private static void ValidateReferences(
        IEnumerable<(FormElementDefinition Element, string Path)> elements,
        HashSet<string> keys,
        List<FormSchemaValidationError> errors)
    {
        foreach ((FormElementDefinition element, string path) in elements)
        {
            if (element.VisibilityCondition is null)
            {
                continue;
            }

            if (element.VisibilityCondition.Rules.Count == 0)
            {
                errors.Add(new($"{path}.visibilityCondition", "شرط نمایش باید حداقل یک قانون داشته باشد."));
            }

            for (int ruleIndex = 0; ruleIndex < element.VisibilityCondition.Rules.Count; ruleIndex++)
            {
                FormConditionRuleDefinition rule = element.VisibilityCondition.Rules[ruleIndex];
                string rulePath = $"{path}.visibilityCondition.rules[{ruleIndex}]";
                if (!keys.Contains(rule.FieldKey))
                {
                    errors.Add(new($"{rulePath}.fieldKey", "فیلد مرجع شرط وجود ندارد."));
                }

                if (string.Equals(rule.FieldKey, element.Key, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new($"{rulePath}.fieldKey", "فیلد نمی‌تواند شرط نمایش خودش باشد."));
                }
            }
        }
    }

    private static void ValidateCalculations(
        IEnumerable<(FormElementDefinition Element, string Path)> elements,
        HashSet<string> keys,
        List<FormSchemaValidationError> errors)
    {
        (FormElementDefinition Element, string Path)[] elementArray = elements.ToArray();
        HashSet<string> calculatedKeys = elementArray
            .Where(item => item.Element.Type == FormElementType.Calculated)
            .Select(item => item.Element.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, System.Text.Json.JsonElement> zeroValues = keys.ToDictionary(
            key => key,
            _ => System.Text.Json.JsonSerializer.SerializeToElement(0),
            StringComparer.OrdinalIgnoreCase);

        foreach ((FormElementDefinition element, string path) in elementArray)
        {
            if (element.Type != FormElementType.Calculated)
            {
                if (!string.IsNullOrWhiteSpace(element.CalculationExpression))
                {
                    errors.Add(new(
                        $"{path}.calculationExpression",
                        "عبارت محاسباتی فقط برای فیلد محاسباتی مجاز است."));
                }

                continue;
            }

            FormCalculationResult result = FormCalculationEngine.Evaluate(
                element.CalculationExpression ?? string.Empty,
                zeroValues);
            if (!result.Succeeded)
            {
                errors.Add(new(
                    $"{path}.calculationExpression",
                    $"عبارت محاسباتی معتبر نیست: {result.Error}"));
            }

            foreach (Match identifier in CalculationIdentifierPattern().Matches(
                         element.CalculationExpression ?? string.Empty))
            {
                string candidate = identifier.Value;
                if (!keys.Contains(candidate) &&
                    !string.Equals(candidate, "SUM", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(candidate, "MIN", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(candidate, "MAX", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(candidate, "ROUND", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new(
                        $"{path}.calculationExpression",
                        $"فیلد «{candidate}» در عبارت محاسباتی وجود ندارد."));
                }
                else if (calculatedKeys.Contains(candidate))
                {
                    errors.Add(new(
                        $"{path}.calculationExpression",
                        "ارجاع یک فیلد محاسباتی به فیلد محاسباتی دیگر مجاز نیست."));
                }
            }
        }
    }

    private static void ValidateOptions(
        FormElementDefinition element,
        string path,
        List<FormSchemaValidationError> errors)
    {
        if (element.Options.Count < 1)
        {
            errors.Add(new($"{path}.options", "این فیلد باید حداقل یک گزینه داشته باشد."));
            return;
        }

        if (element.Options.Count > MaximumOptionsPerElement)
        {
            errors.Add(new(
                $"{path}.options",
                $"تعداد گزینه‌ها نمی‌تواند بیش از {MaximumOptionsPerElement} باشد."));
        }

        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < element.Options.Count; index++)
        {
            FormOptionDefinition option = element.Options[index];
            string optionPath = $"{path}.options[{index}]";
            ValidateRequiredText(option.Value, 200, $"{optionPath}.value", "مقدار گزینه", errors);
            ValidateRequiredText(option.Label, 200, $"{optionPath}.label", "عنوان گزینه", errors);
            if (!string.IsNullOrWhiteSpace(option.Value) && !values.Add(option.Value))
            {
                errors.Add(new($"{optionPath}.value", "مقدار گزینه تکراری است."));
            }
        }
    }

    private static void ValidateValidationRules(
        FormValidationDefinition validation,
        string path,
        List<FormSchemaValidationError> errors)
    {
        if (validation.MinimumLength is < 0 || validation.MaximumLength is < 0 ||
            validation.MinimumLength > validation.MaximumLength)
        {
            errors.Add(new($"{path}.validation", "محدوده طول فیلد معتبر نیست."));
        }

        if (validation.MaximumLength > 100_000)
        {
            errors.Add(new($"{path}.validation.maximumLength", "حداکثر طول نمی‌تواند بیش از ۱۰۰٬۰۰۰ باشد."));
        }

        if (validation.MinimumValue > validation.MaximumValue)
        {
            errors.Add(new($"{path}.validation", "محدوده عددی فیلد معتبر نیست."));
        }

        if (!string.IsNullOrWhiteSpace(validation.Pattern))
        {
            try
            {
                _ = new Regex(validation.Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException)
            {
                errors.Add(new($"{path}.validation.pattern", "عبارت منظم معتبر نیست."));
            }
        }
    }

    private static void ValidateId(
        Guid id,
        string path,
        HashSet<Guid> ids,
        List<FormSchemaValidationError> errors)
    {
        if (id == Guid.Empty)
        {
            errors.Add(new(path, "شناسه نمی‌تواند خالی باشد."));
        }
        else if (!ids.Add(id))
        {
            errors.Add(new(path, "شناسه در ساختار فرم تکراری است."));
        }
    }

    private static void ValidateRequiredText(
        string? value,
        int maximumLength,
        string path,
        string displayName,
        List<FormSchemaValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new(path, $"{displayName} الزامی است."));
        }
        else if (value.Length > maximumLength)
        {
            errors.Add(new(path, $"{displayName} نمی‌تواند بیشتر از {maximumLength} نویسه باشد."));
        }
    }

    private static void ValidateOptionalText(
        string? value,
        int maximumLength,
        string path,
        string displayName,
        List<FormSchemaValidationError> errors)
    {
        if (value?.Length > maximumLength)
        {
            errors.Add(new(path, $"{displayName} نمی‌تواند بیشتر از {maximumLength} نویسه باشد."));
        }
    }

    private static bool RequiresOptions(FormElementType type) =>
        type is FormElementType.Select or FormElementType.MultiSelect or FormElementType.Radio;

    private static bool IsContentElement(FormElementType type) =>
        type is FormElementType.Heading or FormElementType.Paragraph or FormElementType.Divider or
            FormElementType.Alert;

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,99}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyPattern();

    [GeneratedRegex("[A-Za-z_][A-Za-z0-9_]*", RegexOptions.CultureInvariant)]
    private static partial Regex CalculationIdentifierPattern();
}
