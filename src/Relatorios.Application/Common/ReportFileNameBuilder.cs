using System.Text;
using Relatorios.Domain.Reporting;

namespace Relatorios.Application.Common;

// Monta um nome amigável e seguro para arquivo
public static class ReportFileNameBuilder
{
    public static string BuildBaseFileName(ReportIntent intent)
    {
        var reportType = (intent.ReportType ?? string.Empty).Trim().ToLowerInvariant();
        var entity = (intent.Entity ?? string.Empty).Trim().ToLowerInvariant();

        string fileName;

        if (reportType == "total_sales")
        {
            fileName = "total-vendas";
        }
        else if (reportType == "sales_ranking" &&
                 (entity == "pedidos" || entity == "pedido" || entity == "vendas" || entity == "venda"))
        {
            fileName = intent.Limit is int limit
                ? $"{limit}-maiores-vendas"
                : "ranking-vendas";
        }
        else if (reportType == "sales_ranking" &&
                 (entity == "clientes" || entity == "cliente"))
        {
            fileName = intent.Limit is int limit
                ? $"{limit}-maiores-clientes"
                : "ranking-clientes";
        }
        else if (reportType == "single_sale")
        {
            fileName = "venda-especifica";
        }
        else
        {
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

            fileName = partes.Count > 0
                ? string.Join("-", partes)
                : "relatorio";
        }

        return Sanitize(fileName);
    }

    private static string Sanitize(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder();

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (invalidChars.Contains(ch))
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                builder.Append('-');
            }
        }

        var sanitized = builder.ToString();

        while (sanitized.Contains("--"))
        {
            sanitized = sanitized.Replace("--", "-");
        }

        return sanitized.Trim('-');
    }
}