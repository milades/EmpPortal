using System.Text.Json;
using EmpPortal.Application.Forms.Schema;

namespace EmpPortal.Application.UnitTests;

public sealed class FormSubmissionValidatorTests
{
    private static readonly string[] InvalidSkills = ["csharp", "tampered"];

    private static readonly Dictionary<string, string>[] EmptyDependants =
    [
        new() { ["dependant_name"] = string.Empty }
    ];

    [Fact]
    public void RequiredVisibleFieldIsValidated()
    {
        FormSchemaDefinition schema = CreateSchema();
        Dictionary<string, JsonElement> values = new()
        {
            ["has_phone"] = JsonSerializer.SerializeToElement(true)
        };

        FormSchemaValidationResult result = FormSubmissionValidator.Validate(schema, values);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Path == "phone");
    }

    [Fact]
    public void HiddenRequiredFieldIsIgnored()
    {
        FormSchemaDefinition schema = CreateSchema();
        Dictionary<string, JsonElement> values = new()
        {
            ["has_phone"] = JsonSerializer.SerializeToElement(false)
        };

        FormSchemaValidationResult result = FormSubmissionValidator.Validate(schema, values);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void MultiSelectRejectsUnknownAndTooManyOptions()
    {
        FormSchemaDefinition schema = CreateSchema();
        schema.Pages[0].Sections[0].Elements.Add(new FormElementDefinition
        {
            Key = "skills",
            Label = "مهارت‌ها",
            Type = FormElementType.MultiSelect,
            Options =
            [
                new FormOptionDefinition { Value = "csharp", Label = "سی‌شارپ" },
                new FormOptionDefinition { Value = "sql", Label = "SQL" }
            ],
            Validation = new FormValidationDefinition { MaximumLength = 1 }
        });
        Dictionary<string, JsonElement> values = new()
        {
            ["has_phone"] = JsonSerializer.SerializeToElement(false),
            ["skills"] = JsonSerializer.SerializeToElement(InvalidSkills)
        };

        FormSchemaValidationResult result = FormSubmissionValidator.Validate(schema, values);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Path == "skills" && error.Message.Contains("معتبر", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Path == "skills" && error.Message.Contains("حداکثر", StringComparison.Ordinal));
    }

    [Fact]
    public void RepeaterValidatesRequiredChildPerRow()
    {
        FormSchemaDefinition schema = CreateSchema();
        schema.Pages[0].Sections[0].Elements.Add(new FormElementDefinition
        {
            Key = "dependants",
            Label = "افراد تحت تکفل",
            Type = FormElementType.Repeater,
            Children =
            [
                new FormElementDefinition
                {
                    Key = "dependant_name",
                    Label = "نام",
                    Type = FormElementType.Text,
                    Required = true
                }
            ]
        });
        Dictionary<string, JsonElement> values = new()
        {
            ["has_phone"] = JsonSerializer.SerializeToElement(false),
            ["dependants"] = JsonSerializer.SerializeToElement(EmptyDependants)
        };

        FormSchemaValidationResult result = FormSubmissionValidator.Validate(schema, values);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Path == "dependants[0].dependant_name");
    }

    private static FormSchemaDefinition CreateSchema() =>
        new()
        {
            Title = "تماس",
            Pages =
            [
                new FormPageDefinition
                {
                    Title = "اطلاعات",
                    Sections =
                    [
                        new FormSectionDefinition
                        {
                            Elements =
                            [
                                new FormElementDefinition
                                {
                                    Key = "has_phone",
                                    Label = "شماره تماس دارید؟",
                                    Type = FormElementType.Switch
                                },
                                new FormElementDefinition
                                {
                                    Key = "phone",
                                    Label = "شماره تماس",
                                    Type = FormElementType.Phone,
                                    Required = true,
                                    VisibilityCondition = new FormConditionGroupDefinition
                                    {
                                        Rules =
                                        [
                                            new FormConditionRuleDefinition
                                            {
                                                FieldKey = "has_phone",
                                                Operator = FormConditionOperator.Equals,
                                                Value = bool.TrueString
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
}
