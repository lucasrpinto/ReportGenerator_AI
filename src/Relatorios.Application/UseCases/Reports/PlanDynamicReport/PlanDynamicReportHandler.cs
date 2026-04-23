using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Validation;

namespace Relatorios.Application.UseCases.Reports.PlanDynamicReport;

public sealed class PlanDynamicReportHandler
{
    private readonly IOpenAiQueryPlanner _openAiQueryPlanner;
    private readonly DynamicQueryPlanValidator _validator;

    public PlanDynamicReportHandler(
        IOpenAiQueryPlanner openAiQueryPlanner,
        DynamicQueryPlanValidator validator)
    {
        _openAiQueryPlanner = openAiQueryPlanner;
        _validator = validator;
    }

    public async Task<PlanDynamicReportResult> HandleAsync(
        PlanDynamicReportCommand command,
        CancellationToken cancellationToken)
    {
        var plan = await _openAiQueryPlanner.PlanAsync(command.Prompt, cancellationToken);

        var errors = _validator.Validate(plan);

        return new PlanDynamicReportResult
        {
            Plan = plan,
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}