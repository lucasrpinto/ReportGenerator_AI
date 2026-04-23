namespace Relatorios.Domain.Querying;

public sealed class QueryPlan
{
    public string Source { get; set; } = string.Empty;
    public string? SourceAlias { get; set; }
    public List<QueryJoin> Joins { get; set; } = new();
    public List<QuerySelectField> SelectFields { get; set; } = new();
    public List<QueryFilter> Filters { get; set; } = new();
    public List<string> GroupByFields { get; set; } = new();
    public List<QueryOrderBy> OrderByFields { get; set; } = new();
    public int? Limit { get; set; }
}

public sealed class QueryJoin
{
    public string Type { get; set; } = "INNER JOIN";
    public string Table { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string On { get; set; } = string.Empty;
}

public sealed class QuerySelectField
{
    public string Field { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Aggregation { get; set; }
}

public sealed class QueryFilter
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "=";
    public object? Value { get; set; }
}

public sealed class QueryOrderBy
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "DESC";
}