using FluentValidation;
using Relatorios.Application.UseCases.Reports.GenerateReport;

namespace Relatorios.Application.Validation;

public sealed class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .MinimumLength(5);

        RuleFor(x => x.Formats)
            .NotEmpty();
    }
}