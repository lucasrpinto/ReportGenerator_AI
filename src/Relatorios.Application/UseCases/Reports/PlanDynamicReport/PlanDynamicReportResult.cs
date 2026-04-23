using Relatorios.Domain.DynamicQuerying;

namespace Relatorios.Application.UseCases.Reports.PlanDynamicReport;

public sealed class PlanDynamicReportResult
{
    public DynamicQueryPlanDto Plan { get; set; } = new();
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}