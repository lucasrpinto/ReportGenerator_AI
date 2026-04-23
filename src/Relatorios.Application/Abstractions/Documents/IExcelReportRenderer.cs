using System.Data;
using Relatorios.Domain.Reporting;

namespace Relatorios.Application.Abstractions.Documents;

public interface IExcelReportRenderer
{
    Task<string> RenderAsync(
        ReportIntent intent,
        DataTable data,
        string fileNameWithoutExtension,
        CancellationToken cancellationToken);
}