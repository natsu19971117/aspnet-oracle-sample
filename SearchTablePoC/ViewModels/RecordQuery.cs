using Microsoft.AspNetCore.Http;

namespace SearchTablePoC.ViewModels;

public class RecordQuery
{
    public int? Id { get; set; }
    public string? Keyword { get; set; }
    public string? Field01 { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Name { get; set; }
    public DateOnly? UpdatedFrom { get; set; }
    public DateOnly? UpdatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public Dictionary<string, string> ColumnFilters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? ValidateDateRange()
    {
        if (UpdatedFrom.HasValue && UpdatedTo.HasValue && UpdatedFrom > UpdatedTo)
        {
            return "Updated From must be earlier than Updated To.";
        }

        return null;
    }

    public void Normalize()
    {
        if (Page < 1)
        {
            Page = 1;
        }

        if (PageSize != 20 && PageSize != 50 && PageSize != 100)
        {
            PageSize = 20;
        }

        SortDir = SortDir?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true ? "desc" : "asc";

        Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim();
        Field01 = string.IsNullOrWhiteSpace(Field01) ? null : Field01.Trim();
        Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim();
        Status = string.IsNullOrWhiteSpace(Status) ? null : Status.Trim();
        Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim();

        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in ColumnFilters)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                sanitized[key] = value.Trim();
            }
        }

        ColumnFilters = sanitized;
    }

    public void ApplyColumnFiltersFromQuery(IQueryCollection query)
    {
        foreach (var kvp in query)
        {
            if (!kvp.Key.StartsWith("col_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var property = kvp.Key[4..];
            var value = kvp.Value.ToString();
            if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            {
                ColumnFilters[property] = value;
            }
        }
    }

    public string GetColumnFilterValue(string propertyName)
    {
        return ColumnFilters.TryGetValue(propertyName, out var value) ? value : string.Empty;
    }

    public RecordQuery Clone()
    {
        return new RecordQuery
        {
            Id = Id,
            Keyword = Keyword,
            Field01 = Field01,
            Category = Category,
            Status = Status,
            Name = Name,
            UpdatedFrom = UpdatedFrom,
            UpdatedTo = UpdatedTo,
            Page = Page,
            PageSize = PageSize,
            SortBy = SortBy,
            SortDir = SortDir,
            ColumnFilters = new Dictionary<string, string>(ColumnFilters, StringComparer.OrdinalIgnoreCase)
        };
    }
}
