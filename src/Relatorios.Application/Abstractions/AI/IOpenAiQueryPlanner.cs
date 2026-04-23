using Relatorios.Domain.DynamicQuerying;

namespace Relatorios.Application.Abstractions.AI;

public interface IOpenAiQueryPlanner
{
    Task<DynamicQueryPlanDto> PlanAsync(string prompt, CancellationToken cancellationToken);
}