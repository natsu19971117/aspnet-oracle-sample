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
            new("Id", "依頼No", ColumnDataType.Number),
            new("Field01", "発注No", ColumnDataType.Text),
            new("IntegrationStatus", "発注統合有無", ColumnDataType.Text),
            new("Category", "部課", ColumnDataType.Text),
            new("Status", "係", ColumnDataType.Text),
            new("Name", "担当者", ColumnDataType.Text),
            new("UpdatedAt", "契約日", ColumnDataType.Date),
            new("Amount", "契約数量", ColumnDataType.Number),
            new("Field02", "品目コード", ColumnDataType.Text),
            new("Field03", "品目名", ColumnDataType.Text),
            new("Field04", "原産国", ColumnDataType.Text),
            new("Field05", "仕向国", ColumnDataType.Text),
            new("Field06", "生地種別", ColumnDataType.Text),
            new("Field07", "糸番手", ColumnDataType.Text),
            new("Field08", "織組織", ColumnDataType.Text),
            new("Field09", "目付", ColumnDataType.Text),
            new("Field10", "巾", ColumnDataType.Text),
            new("Field11", "色番", ColumnDataType.Text),
            new("Field12", "ロットNo", ColumnDataType.Text),
            new("Field13", "ブランド", ColumnDataType.Text),
            new("Field14", "シーズン", ColumnDataType.Text),
            new("Field15", "用途", ColumnDataType.Text),
            new("Field16", "商社", ColumnDataType.Text),
            new("Field17", "仕入先", ColumnDataType.Text),
            new("Field18", "工場", ColumnDataType.Text),
            new("Field19", "検査等級", ColumnDataType.Text),
            new("Field20", "通関種別", ColumnDataType.Text),
            new("Field21", "輸送手段", ColumnDataType.Text),
            new("Field22", "インコタームズ", ColumnDataType.Text),
            new("Field23", "通貨", ColumnDataType.Text),
            new("Field24", "為替レート", ColumnDataType.Text),
            new("Field25", "単価", ColumnDataType.Text),
            new("Field26", "見積番号", ColumnDataType.Text),
            new("Field27", "見積日", ColumnDataType.Text),
            new("Field28", "契約番号", ColumnDataType.Text),
            new("Field29", "支払条件", ColumnDataType.Text),
            new("Field30", "船積予定日", ColumnDataType.Text),
            new("Field31", "本船名", ColumnDataType.Text),
            new("Field32", "航路", ColumnDataType.Text),
            new("Field33", "船社", ColumnDataType.Text),
            new("Field34", "B/LNo", ColumnDataType.Text),
            new("Field35", "コンテナNo", ColumnDataType.Text),
            new("Field36", "本数", ColumnDataType.Text),
            new("Field37", "数量単位", ColumnDataType.Text),
            new("Field38", "重量", ColumnDataType.Text),
            new("Field39", "重量単位", ColumnDataType.Text),
            new("Field40", "保管倉庫", ColumnDataType.Text),
            new("Field41", "倉庫担当", ColumnDataType.Text),
            new("Field42", "搬入予定日", ColumnDataType.Text),
            new("Field43", "搬出予定日", ColumnDataType.Text),
            new("Field44", "通関担当", ColumnDataType.Text),
            new("Field45", "通関業者", ColumnDataType.Text),
            new("Field46", "関税率", ColumnDataType.Text),
            new("Field47", "消費税額", ColumnDataType.Text),
            new("Field48", "保険会社", ColumnDataType.Text),
            new("Field49", "保険証券No", ColumnDataType.Text),
            new("Field50", "保険金額", ColumnDataType.Text),
            new("Field51", "検品日", ColumnDataType.Text),
            new("Field52", "検品担当", ColumnDataType.Text),
            new("Field53", "検査機関", ColumnDataType.Text),
            new("Field54", "サンプルNo", ColumnDataType.Text),
            new("Field55", "サンプル送付日", ColumnDataType.Text),
            new("Field56", "サンプル回答", ColumnDataType.Text),
            new("Field57", "クレーム有無", ColumnDataType.Text),
            new("Field58", "クレーム内容", ColumnDataType.Text),
            new("Field59", "対応状況", ColumnDataType.Text),
            new("Field60", "最終顧客", ColumnDataType.Text),
            new("Field61", "用途先", ColumnDataType.Text),
            new("Field62", "納入先", ColumnDataType.Text),
            new("Field63", "納入先住所", ColumnDataType.Text),
            new("Field64", "備考", ColumnDataType.Text)
        };

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
