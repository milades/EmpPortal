using System.Text.Json;
using EmpPortal.Application.Forms.Schema;

namespace EmpPortal.Application.UnitTests;

public sealed class FormCalculationEngineTests
{
    [Fact]
    public void ArithmeticExpressionUsesFieldValuesAndFunctions()
    {
        Dictionary<string, JsonElement> values = new()
        {
            ["quantity"] = JsonSerializer.SerializeToElement(3),
            ["unit_price"] = JsonSerializer.SerializeToElement(12.5m)
        };

        FormCalculationResult result = FormCalculationEngine.Evaluate(
            "ROUND(quantity * unit_price, 0)",
            values);

        Assert.True(result.Succeeded);
        Assert.Equal(38m, result.Value);
    }

    [Fact]
    public void DivisionByZeroReturnsControlledFailure()
    {
        FormCalculationResult result = FormCalculationEngine.Evaluate(
            "10 / 0",
            new Dictionary<string, JsonElement>());

        Assert.False(result.Succeeded);
        Assert.Contains("صفر", result.Error, StringComparison.Ordinal);
    }
}
