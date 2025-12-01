namespace SearchTablePoC.ViewModels;

public sealed class IntegrationFilter
{
    public string? Keyword { get; set; }
    public string? PersonInCharge { get; set; }
    public DateOnly? UpdatedFrom { get; set; }
    public DateOnly? UpdatedTo { get; set; }
    public string? TargetOrderNo { get; set; }
    public bool SearchPerformed { get; set; }

    public bool HasCriteria => !string.IsNullOrWhiteSpace(Keyword)
        || !string.IsNullOrWhiteSpace(PersonInCharge)
        || UpdatedFrom.HasValue
        || UpdatedTo.HasValue;
}

public sealed class IntegrationOverrides
{
    public int? ManualRequestNo { get; set; }
    public DateOnly? ManualContractDate { get; set; }
    public string? ManualPersonInCharge { get; set; }
}
