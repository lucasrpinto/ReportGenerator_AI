using Microsoft.Extensions.DependencyInjection;
using Relatorios.Application.DynamicReports;
using Relatorios.Application.Mapping;
using Relatorios.Application.UseCases.Reports.GenerateDynamicReport;
using Relatorios.Application.UseCases.Reports.GetDynamicReportHistory;
using Relatorios.Application.UseCases.Reports.ListDynamicReportHistory;
using Relatorios.Application.UseCases.Reports.ListReportHistory;
using Relatorios.Application.UseCases.Reports.PlanDynamicReport;
using Relatorios.Application.UseCases.Reports.PreviewDynamicReport;
using Relatorios.Application.Validation;

namespace Relatorios.Application.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListReportHistoryHandler>();
        services.AddScoped<PlanDynamicReportHandler>();
        services.AddScoped<PreviewDynamicReportHandler>();
        services.AddScoped<GenerateDynamicFromHistoryHandler>();
        services.AddScoped<ListDynamicReportHistoryHandler>();
        services.AddScoped<DynamicReportBusinessRulesApplier>();

        services.AddScoped<DynamicQueryPlanValidator>();
        services.AddScoped<DynamicQueryCatalogValidator>();
        services.AddScoped<DynamicQueryPlanMapper>();
        services.AddScoped<GetDynamicReportHistoryHandler>();

        return services;
    }
}