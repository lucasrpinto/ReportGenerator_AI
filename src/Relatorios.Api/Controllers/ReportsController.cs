using Microsoft.AspNetCore.Mvc;
using Relatorios.Application.UseCases.Reports.GenerateDynamicReport;
using Relatorios.Application.UseCases.Reports.GenerateReport;
using Relatorios.Application.UseCases.Reports.ListReportHistory;
using Relatorios.Application.UseCases.Reports.PlanDynamicReport;
using Relatorios.Application.UseCases.Reports.PreviewDynamicReport;
using Relatorios.Application.UseCases.Reports.PreviewReport;
using Relatorios.Contracts.Requests;
using Relatorios.Contracts.Responses;

namespace Relatorios.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    /*
    [HttpPost("preview")]
    [ProducesResponseType(typeof(PreviewReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(
        [FromBody] PreviewReportRequest request,
        [FromServices] PreviewReportHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new PreviewReportCommand
        {
            Prompt = request.Prompt
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        var response = new PreviewReportResponse
        {
            ReportName = result.ReportName,
            ReportType = result.ReportType,
            Entity = result.Entity,
            Metric = result.Metric,
            Summary = new PreviewReportSummaryResponse
            {
                RowCount = result.Summary.RowCount,
                ValorTotal = result.Summary.ValorTotal
            },
            Columns = result.Columns,
            Rows = result.Rows
        };

        if (result.Period is not null)
        {
            response.Period = new PreviewReportPeriodResponse
            {
                StartDate = result.Period.StartDate,
                EndDate = result.Period.EndDate
            };
        }

        return Ok(response);
    }*/

    [HttpPost("plan-dynamic")]
    [ProducesResponseType(typeof(PlanDynamicReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PlanDynamic(
    [FromBody] PlanDynamicReportRequest request,
    [FromServices] PlanDynamicReportHandler handler,
    CancellationToken cancellationToken)
    {
        var command = new PlanDynamicReportCommand
        {
            Prompt = request.Prompt
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        var response = new PlanDynamicReportResponse
        {
            IsValid = result.IsValid,
            Errors = result.Errors,
            Plan = new PlanDynamicQueryPlanResponse
            {
                Source = result.Plan.Source,
                SourceAlias = result.Plan.SourceAlias,
                Limit = result.Plan.Limit,
                GroupBy = result.Plan.GroupBy,
                Joins = result.Plan.Joins.Select(x => new PlanDynamicQueryJoinResponse
                {
                    Type = x.Type,
                    Table = x.Table,
                    Alias = x.Alias,
                    On = x.On
                }).ToList(),
                SelectFields = result.Plan.SelectFields.Select(x => new PlanDynamicQuerySelectFieldResponse
                {
                    Field = x.Field,
                    Alias = x.Alias,
                    Aggregation = x.Aggregation
                }).ToList(),
                Filters = result.Plan.Filters.Select(x => new PlanDynamicQueryFilterResponse
                {
                    Field = x.Field,
                    Operator = x.Operator,
                    Value = x.Value
                }).ToList(),
                OrderBy = result.Plan.OrderBy.Select(x => new PlanDynamicQueryOrderByResponse
                {
                    Field = x.Field,
                    Direction = x.Direction
                }).ToList()
            }
        };

        return Ok(response);
    }

    [HttpPost("preview-dynamic")]
    [ProducesResponseType(typeof(PreviewDynamicReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewDynamic(
    [FromBody] PreviewDynamicReportRequest request,
    [FromServices] PreviewDynamicReportHandler handler,
    CancellationToken cancellationToken)
    {
        var command = new PreviewDynamicReportCommand
        {
            Prompt = request.Prompt
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        var response = new PreviewDynamicReportResponse
        {
            Sql = result.Sql,
            RowCount = result.RowCount,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Columns = result.Columns,
            Rows = result.Rows
        };

        return Ok(response);
    }

    [HttpPost("generate-dynamic")]
    public async Task<IActionResult> GenerateDynamic(
    [FromBody] GenerateDynamicReportRequest request,
    [FromServices] GenerateDynamicReportHandler handler,
    CancellationToken cancellationToken)
    {
        var command = new GenerateDynamicReportCommand
        {
            Prompt = request.Prompt,
            Formats = request.Formats
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        var bytes = await System.IO.File.ReadAllBytesAsync(result.FilePath, cancellationToken);

        return File(bytes, result.ContentType, result.FileName);
    }

    /*
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
    [FromBody] GenerateReportRequest request,
    [FromServices] GenerateReportHandler handler,
    CancellationToken cancellationToken)
    {
        var command = new GenerateReportCommand
        {
            Prompt = request.Prompt,
            Formats = request.Formats
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        var bytes = await System.IO.File.ReadAllBytesAsync(result.FilePath, cancellationToken);

        return File(bytes, result.ContentType, result.FileName);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(List<ListReportHistoryItemResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? nomeRelatorio,
        [FromServices] ListReportHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new ListReportHistoryQuery
        {
            DataInicio = dataInicio,
            DataFim = dataFim,
            NomeRelatorio = nomeRelatorio
        };

        var result = await handler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }*/
}