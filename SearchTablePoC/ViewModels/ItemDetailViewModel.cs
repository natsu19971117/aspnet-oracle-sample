using System.Text.Json.Serialization;

namespace SearchTablePoC.ViewModels;

public sealed class ItemDetailViewModel
{
    public required string ItemCode { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string FabricType { get; init; } = string.Empty;
    public List<ColorSizePlan> Colors { get; init; } = new();
}

public sealed class ColorSizePlan
{
    [JsonPropertyName("fabricColor")]
    public string FabricColor { get; init; } = string.Empty;

    [JsonPropertyName("colorNumber")]
    public string ColorNumber { get; init; } = string.Empty;

    [JsonPropertyName("colorName")]
    public string ColorName { get; init; } = string.Empty;

    [JsonPropertyName("sizes")]
    public List<SizeEntry> Sizes { get; init; } = new();
}

public sealed class SizeEntry
{
    public SizeEntry(string size, int quantity, string? dueDate)
    {
        Size = size;
        Quantity = quantity;
        DueDate = dueDate;
    }

    [JsonPropertyName("size")]
    public string Size { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }
}
