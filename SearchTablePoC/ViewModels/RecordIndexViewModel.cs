using SearchTablePoC.Models;

namespace SearchTablePoC.ViewModels;

public class RecordIndexViewModel
{
    public required RecordQuery Query { get; init; }
    public required IReadOnlyList<Record> Records { get; init; }
    public required IReadOnlyList<ColumnDefinition> Columns { get; init; }
    public int TotalCount { get; init; }
    public string? ErrorMessage { get; init; }

    public int PageCount => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)Query.PageSize);

    public int StartIndex => TotalCount == 0 ? 0 : (Query.Page - 1) * Query.PageSize + 1;

    public int EndIndex => TotalCount == 0 ? 0 : Math.Min(Query.Page * Query.PageSize, TotalCount);
}
