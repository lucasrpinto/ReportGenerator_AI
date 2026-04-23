using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Abstractions.Documents;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.Schema;
using Relatorios.Infrastructure.AI.OpenAI;
using Relatorios.Infrastructure.Documents;
using Relatorios.Infrastructure.Documents.Pdf;
using Relatorios.Infrastructure.Options;
using Relatorios.Infrastructure.Persistence.QueryExecution;
using Relatorios.Infrastructure.Repositories;
using Relatorios.Infrastructure.Schema;
using Relatorios.Infrastructure.Security;

namespace Relatorios.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PostgresOptions>(
            configuration.GetSection(PostgresOptions.SectionName));

        services.Configure<ReportStorageOptions>(
            configuration.GetSection(ReportStorageOptions.SectionName));

        services.Configure<ReportHistoryDatabaseOptions>(
            configuration.GetSection(ReportHistoryDatabaseOptions.SectionName));

        services.Configure<OpenAiOptions>(
            configuration.GetSection(OpenAiOptions.SectionName));

        services.AddHttpClient<IOpenAiQueryPlanner, OpenAiQueryPlanner>();

        services.AddScoped<ISchemaCatalogProvider, StaticSchemaCatalogProvider>();
        services.AddScoped<ISqlSafetyValidator, SqlSafetyValidator>();
        services.AddScoped<IQuerySqlBuilder, PostgresQuerySqlBuilder>();

        services.AddScoped<IReportRepository, InMemoryReportRepository>();
        services.AddScoped<IHistoricoRelatorioRepository, PostgresHistoricoRelatorioRepository>();
        services.AddScoped<IReportDataExecutor, PostgresReportDataExecutor>();

        services.AddScoped<IPdfReportRenderer, PdfReportRenderer>();
        services.AddScoped<IExcelReportRenderer, ClosedXmlExcelReportRenderer>();

        return services;
    }
}