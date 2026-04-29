using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relatorios.Application.UseCases.Reports.GenerateDynamicReport;
using Relatorios.Application.UseCases.Reports.GetDynamicReportHistory;
using Relatorios.Application.UseCases.Reports.ListDynamicReportHistory;
using Relatorios.Application.UseCases.Reports.PlanDynamicReport;
using Relatorios.Application.UseCases.Reports.PreviewDynamicReport;
using Relatorios.Contracts.Requests;
using Relatorios.Contracts.Responses;

namespace Relatorios.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    /*[HttpPost("plan-dynamic")]
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
    }*/

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
            HistoryId = result.HistoryId,
            Sql = result.Sql,
            RowCount = result.RowCount,
            MaxPreviewRows = result.MaxPreviewRows,
            ExecutionTimeMs = result.ExecutionTimeMs,
            StartDate = result.StartDate,
            EndDate = result.EndDate,
            TotalLiquido = result.TotalLiquido,
            Columns = result.Columns,
            Rows = result.Rows
        };

        return Ok(response);
    }

    [HttpPost("generate-dynamic/from-history")]
    public async Task<IActionResult> GenerateDynamicFromHistory(
    [FromBody] GenerateDynamicFromHistoryRequest request,
    [FromServices] GenerateDynamicFromHistoryHandler handler,
    CancellationToken cancellationToken)
    {
        var command = new GenerateDynamicFromHistoryCommand
        {
            HistoryId = request.HistoryId,
            Formats = request.Formats
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        var bytes = await System.IO.File.ReadAllBytesAsync(result.FilePath, cancellationToken);

        return File(bytes, result.ContentType, result.FileName);
    }

    [HttpGet("dynamic-history/{id:guid}")]
    [ProducesResponseType(typeof(DynamicReportHistoryDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DynamicHistoryById(
    [FromRoute] Guid id,
    [FromServices] GetDynamicReportHistoryHandler handler,
    CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        var response = new DynamicReportHistoryDetailsResponse
        {
            Id = result.Id,
            SourceHistoryId = result.SourceHistoryId,
            Prompt = result.Prompt,
            PlanJson = result.PlanJson,
            Sql = result.Sql,
            Action = result.Action,
            FileName = result.FileName,
            Format = result.Format,
            RowCount = result.RowCount,
            ExecutionTimeMs = result.ExecutionTimeMs,
            CreatedAt = result.CreatedAt
        };

        return Ok(response);
    }

    [HttpGet("dynamic-history")]
    [ProducesResponseType(typeof(List<DynamicReportHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DynamicHistory(
    [FromServices] ListDynamicReportHistoryHandler handler,
    CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        var response = result.Select(x => new DynamicReportHistoryResponse
        {
            Id = x.Id,
            SourceHistoryId = x.SourceHistoryId,
            Prompt = x.Prompt,
            Action = x.Action,
            FileName = x.FileName,
            Format = x.Format,
            RowCount = x.RowCount,
            ExecutionTimeMs = x.ExecutionTimeMs,
            CreatedAt = x.CreatedAt
        }).ToList();

        return Ok(response);
    }
}