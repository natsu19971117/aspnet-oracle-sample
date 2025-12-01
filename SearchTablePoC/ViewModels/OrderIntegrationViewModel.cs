using SearchTablePoC.Models;
using SearchTablePoC.Services;

namespace SearchTablePoC.ViewModels;

public sealed class OrderIntegrationViewModel
{
    public IReadOnlyList<Record> AvailableRecords { get; init; } = Array.Empty<Record>();
    public IReadOnlyList<IntegrationGroup> IntegratedOrders { get; init; } = Array.Empty<IntegrationGroup>();
    public IntegrationFilter Filter { get; init; } = new();
    public IntegrationOverrides Overrides { get; init; } = new();
    public bool ShowUndoResults { get; init; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
