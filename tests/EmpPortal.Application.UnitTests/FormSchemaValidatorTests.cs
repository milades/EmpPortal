using EmpPortal.Application.Forms.Schema;

namespace EmpPortal.Application.UnitTests;

public sealed class FormSchemaValidatorTests
{
    [Fact]
    public void ValidSchemaPassesValidationAndRoundTrips()
    {
        FormSchemaDefinition schema = CreateValidSchema();
        FormSchemaValidationResult result = FormSchemaValidator.Validate(schema);
        string json = FormSchemaSerializer.Serialize(schema);
        FormSchemaDefinition restored = FormSchemaSerializer.Deserialize(json);

        Assert.True(result.IsValid);
        Assert.Equal(schema.Title, restored.Title);
        Assert.Equal(64, FormSchemaSerializer.ComputeHash(json).Length);
    }

    [Fact]
    public void DuplicateKeysAndUnknownConditionReferencesAreRejected()
    {
        FormSchemaDefinition schema = CreateValidSchema();
        FormSectionDefinition section = schema.Pages[0].Sections[0];
        section.Elements.Add(new FormElementDefinition
        {
            Key = "employee_name",
            Label = "نام دوم",
            Type = FormElementType.Text,
            VisibilityCondition = new FormConditionGroupDefinition
            {
                Rules =
                [
                    new FormConditionRuleDefinition
                    {
                        FieldKey = "missing_field",
                        Operator = FormConditionOperator.Equals,
                        Value = "1"
                    }
                ]
            }
        });

        FormSchemaValidationResult result = FormSchemaValidator.Validate(schema);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("تکراری", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Message.Contains("وجود ندارد", StringComparison.Ordinal));
    }

    [Fact]
    public void CalculatedFieldCannotReferenceAnotherCalculatedField()
    {
        FormSchemaDefinition schema = CreateValidSchema();
        FormSectionDefinition section = schema.Pages[0].Sections[0];
        section.Elements.Add(new FormElementDefinition
        {
            Key = "base_amount",
            Label = "مبلغ پایه",
            Type = FormElementType.Number
        });
        section.Elements.Add(new FormElementDefinition
        {
            Key = "tax_amount",
            Label = "مالیات",
            Type = FormElementType.Calculated,
            CalculationExpression = "base_amount * 0.1"
        });
        section.Elements.Add(new FormElementDefinition
        {
            Key = "total_amount",
            Label = "جمع",
            Type = FormElementType.Calculated,
            CalculationExpression = "base_amount + tax_amount"
        });

        FormSchemaValidationResult result = FormSchemaValidator.Validate(schema);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.Message.Contains("فیلد محاسباتی دیگر", StringComparison.Ordinal));
    }

    private static FormSchemaDefinition CreateValidSchema() =>
        new()
        {
            Title = "فرم اطلاعات کارمند",
            Pages =
            [
                new FormPageDefinition
                {
                    Title = "اطلاعات پایه",
                    Sections =
                    [
                        new FormSectionDefinition
                        {
                            Title = "مشخصات",
                            Elements =
                            [
                                new FormElementDefinition
                                {
                                    Key = "employee_name",
                                    Label = "نام و نام خانوادگی",
                                    Type = FormElementType.Text,
                                    Required = true
                                }
                            ]
                        }
                    ]
                }
            ]
        };
}
