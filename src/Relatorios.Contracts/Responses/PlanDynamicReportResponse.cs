namespace Relatorios.Contracts.Responses;

public sealed class PlanDynamicReportResponse
{
    public PlanDynamicQueryPlanResponse Plan { get; set; } = new();
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class PlanDynamicQueryPlanResponse
{
    public string Source { get; set; } = string.Empty;
    public string? SourceAlias { get; set; }

    public List<PlanDynamicQueryJoinResponse> Joins { get; set; } = new();
    public List<PlanDynamicQuerySelectFieldResponse> SelectFields { get; set; } = new();
    public List<PlanDynamicQueryFilterResponse> Filters { get; set; } = new();
    public List<string> GroupBy { get; set; } = new();
    public List<PlanDynamicQueryOrderByResponse> OrderBy { get; set; } = new();

    public int? Limit { get; set; }
}

public sealed class PlanDynamicQueryJoinResponse
{
    public string Type { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string On { get; set; } = string.Empty;
}

public sealed class PlanDynamicQuerySelectFieldResponse
{
    public string Field { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Aggregation { get; set; }
}

public sealed class PlanDynamicQueryFilterResponse
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object? Value { get; set; }
}

public sealed class PlanDynamicQueryOrderByResponse
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
}