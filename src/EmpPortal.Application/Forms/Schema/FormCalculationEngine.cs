using System.Globalization;
using System.Text.Json;

namespace EmpPortal.Application.Forms.Schema;

public sealed record FormCalculationResult(bool Succeeded, decimal Value, string? Error);

public static class FormCalculationEngine
{
    public static FormCalculationResult Evaluate(
        string expression,
        IReadOnlyDictionary<string, JsonElement> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > 500)
        {
            return new(false, 0, "عبارت محاسباتی خالی یا بیش از حد طولانی است.");
        }

        try
        {
            Parser parser = new(expression, values);
            decimal result = parser.Parse();
            return new(true, result, null);
        }
        catch (InvalidOperationException exception)
        {
            return new(false, 0, exception.Message);
        }
        catch (OverflowException)
        {
            return new(false, 0, "نتیجه محاسبه خارج از محدوده مجاز است.");
        }
    }

    private sealed class Parser(
        string expression,
        IReadOnlyDictionary<string, JsonElement> values)
    {
        private int position;
        private int depth;

        public decimal Parse()
        {
            decimal result = ParseExpression();
            SkipWhitespace();
            if (position != expression.Length)
            {
                throw Error("نویسه غیرمجاز در عبارت محاسباتی وجود دارد.");
            }

            return result;
        }

        private decimal ParseExpression()
        {
            EnterDepth();
            try
            {
                decimal value = ParseTerm();
                while (true)
                {
                    SkipWhitespace();
                    if (TryConsume('+'))
                    {
                        value += ParseTerm();
                    }
                    else if (TryConsume('-'))
                    {
                        value -= ParseTerm();
                    }
                    else
                    {
                        return value;
                    }
                }
            }
            finally
            {
                depth--;
            }
        }

        private decimal ParseTerm()
        {
            decimal value = ParseUnary();
            while (true)
            {
                SkipWhitespace();
                if (TryConsume('*'))
                {
                    value *= ParseUnary();
                }
                else if (TryConsume('/'))
                {
                    decimal divisor = ParseUnary();
                    value = divisor == 0
                        ? throw Error("تقسیم بر صفر مجاز نیست.")
                        : value / divisor;
                }
                else if (TryConsume('%'))
                {
                    decimal divisor = ParseUnary();
                    value = divisor == 0
                        ? throw Error("باقی‌مانده تقسیم بر صفر مجاز نیست.")
                        : value % divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private decimal ParseUnary()
        {
            SkipWhitespace();
            if (TryConsume('+'))
            {
                return ParseUnary();
            }

            if (TryConsume('-'))
            {
                return -ParseUnary();
            }

            return ParsePrimary();
        }

        private decimal ParsePrimary()
        {
            SkipWhitespace();
            if (TryConsume('('))
            {
                decimal nested = ParseExpression();
                SkipWhitespace();
                Expect(')');
                return nested;
            }

            if (position < expression.Length &&
                (char.IsAsciiLetter(expression[position]) || expression[position] == '_'))
            {
                string identifier = ParseIdentifier();
                SkipWhitespace();
                return position < expression.Length && expression[position] == '('
                    ? ParseFunction(identifier)
                    : ResolveIdentifier(identifier);
            }

            return ParseNumber();
        }

        private decimal ParseFunction(string identifier)
        {
            Expect('(');
            List<decimal> arguments = [];
            SkipWhitespace();
            if (!TryConsume(')'))
            {
                do
                {
                    arguments.Add(ParseExpression());
                    SkipWhitespace();
                }
                while (TryConsume(','));

                Expect(')');
            }

            return identifier.ToUpperInvariant() switch
            {
                "SUM" when arguments.Count > 0 => arguments.Sum(),
                "MIN" when arguments.Count > 0 => arguments.Min(),
                "MAX" when arguments.Count > 0 => arguments.Max(),
                "ROUND" when arguments.Count is 1 or 2 => decimal.Round(
                    arguments[0],
                    arguments.Count == 2 ? checked((int)arguments[1]) : 0,
                    MidpointRounding.AwayFromZero),
                _ => throw Error("تابع محاسباتی پشتیبانی نمی‌شود یا تعداد ورودی‌های آن صحیح نیست.")
            };
        }

        private decimal ResolveIdentifier(string identifier)
        {
            if (!values.TryGetValue(identifier, out JsonElement value))
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(
                    value.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out number))
            {
                return number;
            }

            return 0;
        }

        private decimal ParseNumber()
        {
            int start = position;
            bool hasDecimalPoint = false;
            while (position < expression.Length)
            {
                char character = expression[position];
                if (char.IsAsciiDigit(character))
                {
                    position++;
                    continue;
                }

                if (character == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    position++;
                    continue;
                }

                break;
            }

            if (start == position || !decimal.TryParse(
                    expression[start..position],
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal result))
            {
                throw Error("عدد یا نام فیلد معتبر نیست.");
            }

            return result;
        }

        private string ParseIdentifier()
        {
            int start = position++;
            while (position < expression.Length &&
                   (char.IsAsciiLetterOrDigit(expression[position]) || expression[position] == '_'))
            {
                position++;
            }

            return expression[start..position];
        }

        private void EnterDepth()
        {
            depth++;
            if (depth > 20)
            {
                throw Error("عمق عبارت محاسباتی بیش از حد مجاز است.");
            }
        }

        private bool TryConsume(char expected)
        {
            if (position >= expression.Length || expression[position] != expected)
            {
                return false;
            }

            position++;
            return true;
        }

        private void Expect(char expected)
        {
            SkipWhitespace();
            if (!TryConsume(expected))
            {
                throw Error($"نویسه «{expected}» در عبارت محاسباتی انتظار می‌رفت.");
            }
        }

        private void SkipWhitespace()
        {
            while (position < expression.Length && char.IsWhiteSpace(expression[position]))
            {
                position++;
            }
        }

        private static InvalidOperationException Error(string message) => new(message);
    }
}
