using System.Reflection;

namespace SearchTablePoC.Models;

public enum ColumnDataType
{
    Text,
    Number,
    Date
}

public sealed class ColumnDefinition
{
    public ColumnDefinition(string propertyName, string displayName, ColumnDataType dataType)
    {
        PropertyName = propertyName;
        DisplayName = displayName;
        DataType = dataType;
    }

    public string PropertyName { get; }
    public string DisplayName { get; }
    public ColumnDataType DataType { get; }
    internal PropertyInfo? PropertyInfo { get; set; }
    internal Func<Record, object?> Accessor { get; set; } = _ => null;
}

public static class RecordMetadata
{
    public static IReadOnlyList<ColumnDefinition> Columns { get; }
    public static IReadOnlyDictionary<string, ColumnDefinition> ColumnLookup { get; }

    private static readonly IReadOnlyDictionary<string, PropertyInfo> PropertyInfos;

    static RecordMetadata()
    {
        PropertyInfos = typeof(Record).GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var columns = new List<ColumnDefinition>
        {
            new("Id", "ID", ColumnDataType.Number),
            new("Name", "Name", ColumnDataType.Text),
            new("Category", "Category", ColumnDataType.Text),
            new("Status", "Status", ColumnDataType.Text),
            new("UpdatedAt", "Updated At", ColumnDataType.Date),
            new("Amount", "Amount", ColumnDataType.Number)
        };

        for (var i = 1; i <= 64; i++)
        {
            columns.Add(new($"Field{i:00}", $"Field {i:00}", ColumnDataType.Text));
        }

        foreach (var column in columns)
        {
            if (PropertyInfos.TryGetValue(column.PropertyName, out var info))
            {
                column.PropertyInfo = info;
                column.Accessor = record => info.GetValue(record);
            }
        }

        Columns = columns;
        ColumnLookup = Columns.ToDictionary(c => c.PropertyName, c => c, StringComparer.OrdinalIgnoreCase);
    }

    public static PropertyInfo? GetProperty(string propertyName)
    {
        return PropertyInfos.TryGetValue(propertyName, out var info) ? info : null;
    }

    public static string FormatValue(Record record, ColumnDefinition column)
    {
        var value = column.Accessor(record);
        if (value is null)
        {
            return string.Empty;
        }

        return column.DataType switch
        {
            ColumnDataType.Date when value is DateOnly date => date.ToString("yyyy-MM-dd"),
            _ => value switch
            {
                DateTime dateTime => dateTime.ToString("yyyy-MM-dd"),
                _ => value.ToString() ?? string.Empty
            }
        };
    }
}
