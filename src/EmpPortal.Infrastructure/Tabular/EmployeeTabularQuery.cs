using System.Globalization;
using System.Text.RegularExpressions;
using EmpPortal.Application.Tabular;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmpPortal.Infrastructure.Tabular;

public sealed partial class EmployeeTabularQuery(
    IOptions<ExternalTabularSourceOptions> options,
    IHostEnvironment environment,
    ILogger<EmployeeTabularQuery> logger) : IEmployeeTabularQuery
{
    public async Task<TabularDataSet> QueryAsync(
        EmployeeTabularSourceKind sourceKind,
        string personnelCode,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personnelCode);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        ExternalSqlViewOptions source = ResolveSource(sourceKind);
        if (string.IsNullOrWhiteSpace(source.ConnectionString) ||
            string.IsNullOrWhiteSpace(source.ViewName))
        {
            if (environment.IsDevelopment())
            {
                return CreateStub(sourceKind, personnelCode, page, pageSize);
            }

            throw new InvalidOperationException("منبع داده خارجی برای این بخش پیکربندی نشده است.");
        }

        string viewName = QuoteObjectName(source.ViewName);
        string personnelColumn = QuoteIdentifier(source.PersonnelCodeColumn);

        try
        {
            await using SqlConnection connection = new(source.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            int totalCount;
            await using (SqlCommand countCommand = connection.CreateCommand())
            {
                countCommand.CommandText =
                    $"SELECT COUNT(1) FROM {viewName} WHERE {personnelColumn} = @personnelCode";
                countCommand.Parameters.AddWithValue("@personnelCode", personnelCode.Trim());
                object? scalar = await countCommand.ExecuteScalarAsync(cancellationToken);
                totalCount = Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
            }

            List<string> columns = [];
            List<IReadOnlyList<object?>> rows = [];
            int skip = (page - 1) * pageSize;

            await using (SqlCommand dataCommand = connection.CreateCommand())
            {
                dataCommand.CommandText =
                    $"""
                     SELECT * FROM {viewName}
                     WHERE {personnelColumn} = @personnelCode
                     ORDER BY (SELECT NULL)
                     OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
                     """;
                dataCommand.Parameters.AddWithValue("@personnelCode", personnelCode.Trim());
                dataCommand.Parameters.AddWithValue("@skip", skip);
                dataCommand.Parameters.AddWithValue("@take", pageSize);

                await using SqlDataReader reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
                for (int index = 0; index < reader.FieldCount; index++)
                {
                    columns.Add(reader.GetName(index));
                }

                while (await reader.ReadAsync(cancellationToken))
                {
                    object?[] values = new object?[reader.FieldCount];
                    for (int index = 0; index < reader.FieldCount; index++)
                    {
                        values[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    }

                    rows.Add(values);
                }
            }

            return new TabularDataSet(columns, rows, totalCount);
        }
        catch (Exception exception) when (environment.IsDevelopment())
        {
            LogStubFallback(logger, exception, sourceKind);
            return CreateStub(sourceKind, personnelCode, page, pageSize);
        }
    }

    private ExternalSqlViewOptions ResolveSource(EmployeeTabularSourceKind sourceKind) =>
        sourceKind switch
        {
            EmployeeTabularSourceKind.Benefits => options.Value.Benefits,
            EmployeeTabularSourceKind.Assets => options.Value.Assets,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
        };

    private static TabularDataSet CreateStub(
        EmployeeTabularSourceKind sourceKind,
        string personnelCode,
        int page,
        int pageSize)
    {
        IReadOnlyList<string> columns = sourceKind switch
        {
            EmployeeTabularSourceKind.Benefits =>
            [
                "PersonnelCode", "FacilityTitle", "Amount", "Status", "StartDate"
            ],
            _ =>
            [
                "PersonnelCode", "AssetTag", "AssetTitle", "Category", "AssignedOn"
            ]
        };

        List<IReadOnlyList<object?>> allRows = [];
        for (int index = 1; index <= 7; index++)
        {
            if (sourceKind == EmployeeTabularSourceKind.Benefits)
            {
                allRows.Add(
                [
                    personnelCode,
                    $"تسهیلات نمونه {index}",
                    1_000_000m * index,
                    index % 2 == 0 ? "فعال" : "تسویه‌شده",
                    DateOnly.FromDateTime(DateTime.Today.AddMonths(-index))
                ]);
            }
            else
            {
                allRows.Add(
                [
                    personnelCode,
                    $"AST-{index:000}",
                    $"تجهیز نمونه {index}",
                    index % 2 == 0 ? "رایانه" : "ملزومات",
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-index * 10))
                ]);
            }
        }

        int total = allRows.Count;
        IReadOnlyList<object?>[] pageRows = allRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        return new TabularDataSet(columns, pageRows, total);
    }

    private static string QuoteObjectName(string objectName)
    {
        string[] parts = objectName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is 0 or > 3)
        {
            throw new InvalidOperationException("نام View خارجی نامعتبر است.");
        }

        return string.Join('.', parts.Select(QuoteIdentifier));
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (!IdentifierRegex().IsMatch(identifier))
        {
            throw new InvalidOperationException($"شناسه SQL نامعتبر است: {identifier}");
        }

        return "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "Falling back to development stub tabular data for {SourceKind}.")]
    private static partial void LogStubFallback(
        ILogger logger,
        Exception exception,
        EmployeeTabularSourceKind sourceKind);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
