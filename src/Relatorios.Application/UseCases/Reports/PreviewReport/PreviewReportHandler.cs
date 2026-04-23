using System.Data;
using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Abstractions.Querying;

namespace Relatorios.Application.UseCases.Reports.PreviewReport;

// Handler para montar a pré-visualização exibida no front-end
public sealed class PreviewReportHandler
{
    private readonly IReportInterpreter _reportInterpreter;
    private readonly IQueryPlanBuilder _queryPlanBuilder;
    private readonly IReportDataExecutor _reportDataExecutor;

    public PreviewReportHandler(
        IReportInterpreter reportInterpreter,
        IQueryPlanBuilder queryPlanBuilder,
        IReportDataExecutor reportDataExecutor)
    {
        _reportInterpreter = reportInterpreter;
        _queryPlanBuilder = queryPlanBuilder;
        _reportDataExecutor = reportDataExecutor;
    }

    public async Task<PreviewReportResult> HandleAsync(
        PreviewReportCommand command,
        CancellationToken cancellationToken)
    {
        var intent = await _reportInterpreter.InterpretAsync(command.Prompt, cancellationToken);
        var queryPlan = await _queryPlanBuilder.BuildAsync(intent, cancellationToken);
        var data = await _reportDataExecutor.ExecuteAsync(queryPlan, cancellationToken);

        var result = new PreviewReportResult
        {
            ReportName = BuildNomeRelatorioAmigavel(intent),
            ReportType = intent.ReportType,
            Entity = intent.Entity,
            Metric = intent.Metric,
            Summary = new PreviewReportSummaryResult
            {
                RowCount = data.Rows.Count,
                ValorTotal = TryExtractValorTotal(data)
            },
            Columns = data.Columns
                .Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToList(),
            Rows = ConvertRows(data)
        };

        if (intent.TimeRange is not null)
        {
            result.Period = new PreviewReportPeriodResult
            {
                StartDate = intent.TimeRange.StartDate,
                EndDate = intent.TimeRange.EndDate
            };
        }

        return result;
    }

    private static List<Dictionary<string, object?>> ConvertRows(DataTable data)
    {
        var rows = new List<Dictionary<string, object?>>();

        foreach (DataRow row in data.Rows)
        {
            var item = new Dictionary<string, object?>();

            foreach (DataColumn column in data.Columns)
            {
                var value = row[column];
                item[column.ColumnName] = value == DBNull.Value ? null : value;
            }

            rows.Add(item);
        }

        return rows;
    }

    private static string BuildNomeRelatorioAmigavel(dynamic intent)
    {
        var reportType = (intent.ReportType ?? string.Empty).ToString().Trim().ToLowerInvariant();
        var entity = (intent.Entity ?? string.Empty).ToString().Trim().ToLowerInvariant();

        if (reportType == "total_sales")
        {
            return "Total de vendas";
        }

        if (reportType == "sales_ranking" && (entity == "pedidos" || entity == "pedido" || entity == "vendas" || entity == "venda"))
        {
            return intent.Limit is int limit
                ? $"Top {limit} maiores vendas"
                : "Ranking de vendas";
        }

        if (reportType == "sales_ranking" && (entity == "clientes" || entity == "cliente"))
        {
            return intent.Limit is int limit
                ? $"Top {limit} clientes"
                : "Ranking de clientes";
        }

        if (reportType == "single_sale")
        {
            return "Venda específica";
        }

        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(intent.ReportType))
        {
            partes.Add(intent.ReportType);
        }

        if (!string.IsNullOrWhiteSpace(intent.Entity))
        {
            partes.Add(intent.Entity);
        }

        if (!string.IsNullOrWhiteSpace(intent.Metric))
        {
            partes.Add(intent.Metric);
        }

        return partes.Count > 0
            ? string.Join(" - ", partes)
            : "Relatório";
    }

    private static decimal TryExtractValorTotal(DataTable data)
    {
        if (data.Rows.Count == 0 || data.Columns.Count == 0)
        {
            return 0m;
        }

        var nomesPreferidos = new[]
        {
        "VALOR LIQUIDO(R$)",
        "VENDAS (R$)",
        "ESTORNO (R$)",
        "valor_total",
        "valor_liquido",
        "valor_venda",
        "total",
        "valor",
        "vl_total",
        "sum",
        "soma"
    };

        foreach (var nomePreferido in nomesPreferidos)
        {
            var column = data.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c =>
                    c.ColumnName.Trim().Equals(nomePreferido, StringComparison.OrdinalIgnoreCase));

            if (column is null)
            {
                continue;
            }

            decimal soma = 0m;

            foreach (DataRow row in data.Rows)
            {
                var value = row[column];

                if (value is null || value == DBNull.Value)
                {
                    continue;
                }

                if (value is decimal decimalValue)
                {
                    soma += decimalValue;
                    continue;
                }

                if (value is double doubleValue)
                {
                    soma += Convert.ToDecimal(doubleValue);
                    continue;
                }

                if (value is float floatValue)
                {
                    soma += Convert.ToDecimal(floatValue);
                    continue;
                }

                if (value is int intValue)
                {
                    soma += intValue;
                    continue;
                }

                if (value is long longValue)
                {
                    soma += longValue;
                    continue;
                }

                var text = value.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (decimal.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    new System.Globalization.CultureInfo("pt-BR"),
                    out var parsedPtBr))
                {
                    soma += parsedPtBr;
                    continue;
                }

                if (decimal.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedInvariant))
                {
                    soma += parsedInvariant;
                }
            }

            return soma;
        }

        return 0m;
    }
}