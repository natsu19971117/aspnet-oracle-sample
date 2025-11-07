using System.Text;
using SearchTablePoC.Models;
using SearchTablePoC.ViewModels;

namespace SearchTablePoC.Services;

public sealed class MockRepository
{
    private readonly List<Record> _records;
    private readonly Random _random = new();

    public MockRepository()
    {
        _records = GenerateRecords(520);
    }

    public RecordsResult GetRecords(RecordQuery query, bool applyPaging = true)
    {
        query.Normalize();

        var working = ApplySorting(ApplyColumnFilters(ApplySearch(_records.AsEnumerable(), query), query), query).ToList();
        var totalCount = working.Count;

        var pageCount = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (query.Page > pageCount)
        {
            query.Page = pageCount;
        }

        List<Record> items;
        if (applyPaging)
        {
            items = working
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();
        }
        else
        {
            items = working;
        }

        return new RecordsResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public string ToCsv(IEnumerable<Record> records)
    {
        var builder = new StringBuilder();
        var headers = RecordMetadata.Columns.Select(c => Escape(c.PropertyName));
        builder.AppendLine(string.Join(',', headers));

        foreach (var record in records)
        {
            var values = RecordMetadata.Columns
                .Select(column => Escape(RecordMetadata.FormatValue(record, column)));
            builder.AppendLine(string.Join(',', values));
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private IEnumerable<Record> ApplySearch(IEnumerable<Record> source, RecordQuery query)
    {
        var results = source;

        if (query.Id.HasValue)
        {
            results = results.Where(r => r.Id == query.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            results = results.Where(r => r.Name.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            results = results.Where(r => string.Equals(r.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            results = results.Where(r => string.Equals(r.Status, query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (query.UpdatedFrom.HasValue)
        {
            results = results.Where(r => r.UpdatedAt >= query.UpdatedFrom.Value);
        }

        if (query.UpdatedTo.HasValue)
        {
            results = results.Where(r => r.UpdatedAt <= query.UpdatedTo.Value);
        }

        return results;
    }

    private IEnumerable<Record> ApplyColumnFilters(IEnumerable<Record> source, RecordQuery query)
    {
        var results = source;

        foreach (var (propertyName, value) in query.ColumnFilters)
        {
            if (!RecordMetadata.ColumnLookup.TryGetValue(propertyName, out var column) || column.PropertyInfo is null)
            {
                continue;
            }

            var filterValue = value;
            results = results.Where(record =>
            {
                var cell = column.Accessor(record);
                if (cell is null)
                {
                    return false;
                }

                return cell switch
                {
                    DateOnly date => date.ToString("yyyy-MM-dd").Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                    IFormattable formattable => formattable.ToString(null, null)?.IndexOf(filterValue, StringComparison.OrdinalIgnoreCase) >= 0,
                    _ => cell.ToString()?.IndexOf(filterValue, StringComparison.OrdinalIgnoreCase) >= 0
                };
            });
        }

        return results;
    }

    private IEnumerable<Record> ApplySorting(IEnumerable<Record> source, RecordQuery query)
    {
        var sortBy = query.SortBy;
        if (string.IsNullOrWhiteSpace(sortBy) || !RecordMetadata.ColumnLookup.TryGetValue(sortBy, out var column))
        {
            column = RecordMetadata.ColumnLookup["Id"];
            sortBy = "Id";
        }

        var comparer = Comparer<object?>.Create((left, right) =>
        {
            if (left is null && right is null)
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            return (left, right) switch
            {
                (DateOnly dateLeft, DateOnly dateRight) => dateLeft.CompareTo(dateRight),
                (IComparable comparableLeft, IComparable comparableRight) => comparableLeft.CompareTo(comparableRight),
                _ => string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase)
            };
        });

        var sorted = query.SortDir == "desc"
            ? source.OrderByDescending(column.Accessor, comparer)
            : source.OrderBy(column.Accessor, comparer);

        return sorted;
    }

    private List<Record> GenerateRecords(int count)
    {
        var firstNames = new[] { "Alex", "Taylor", "Jordan", "Morgan", "Reese", "Casey", "Avery", "Riley" };
        var lastNames = new[] { "Smith", "Johnson", "Lee", "Brown", "Davis", "Miller", "Garcia", "Martinez" };
        var categories = new[] { "A", "B", "C" };
        var statuses = new[] { "Active", "Inactive" };
        var words = new[]
        {
            "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel", "india", "juliet",
            "kilo", "lima", "mike", "november", "oscar", "papa", "quebec", "romeo", "sierra", "tango",
            "uniform", "victor", "whiskey", "xray", "yankee", "zulu"
        };

        var list = new List<Record>(count);
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (var i = 1; i <= count; i++)
        {
            var record = new Record
            {
                Id = i,
                Name = $"{firstNames[_random.Next(firstNames.Length)]} {lastNames[_random.Next(lastNames.Length)]}",
                Category = categories[_random.Next(categories.Length)],
                Status = statuses[_random.Next(statuses.Length)],
                UpdatedAt = today.AddDays(-_random.Next(0, 730)),
                Amount = _random.Next(0, 100001)
            };

            for (var field = 1; field <= 64; field++)
            {
                var tokenCount = _random.Next(1, 4);
                var builder = new StringBuilder();
                for (var token = 0; token < tokenCount; token++)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(words[_random.Next(words.Length)]);
                }

                var property = RecordMetadata.GetProperty($"Field{field:00}");
                property?.SetValue(record, builder.ToString());
            }

            list.Add(record);
        }

        return list;
    }
}

public sealed class RecordsResult
{
    public required IReadOnlyList<Record> Items { get; init; }
    public required int TotalCount { get; init; }
}
