using System.Text.Json;
using Npgsql;

namespace Relatorios.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Não foi possível processar o relatório.",
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Dados inválidos.",
                ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "Erro PostgreSQL ao executar relatório.");

            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Erro ao executar consulta no banco de dados.",
                ex.MessageText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado na API.");

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Erro interno ao processar a solicitação.",
                "Verifique os logs da aplicação para mais detalhes.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message,
            detail
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}