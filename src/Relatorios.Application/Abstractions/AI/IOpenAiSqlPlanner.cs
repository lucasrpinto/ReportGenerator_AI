namespace Relatorios.Application.Abstractions.AI;

public interface IOpenAiSqlPlanner
{
    Task<string> GenerateSqlAsync(string prompt, CancellationToken cancellationToken);
}