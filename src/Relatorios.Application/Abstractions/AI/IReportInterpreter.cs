using Relatorios.Domain.Reporting;

namespace Relatorios.Application.Abstractions.AI;

public interface IReportInterpreter
{
    Task<ReportIntent> InterpretAsync(string prompt, CancellationToken cancellationToken);
}