namespace EmpPortal.Application.Tabular;

public sealed record TabularDataSet(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int TotalCount);

public enum EmployeeTabularSourceKind
{
    Benefits = 1,
    Assets = 2
}

public interface IEmployeeTabularQuery
{
    public Task<TabularDataSet> QueryAsync(
        EmployeeTabularSourceKind sourceKind,
        string personnelCode,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
