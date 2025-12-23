using FluentValidation;

namespace ServicoPix.Application.UseCases.SolicitarPix.Commands;

public class SolicitarPixValidator : AbstractValidator<SolicitarPixCommand>
{
    public SolicitarPixValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("Valor deve ser positivo.");
        RuleFor(x => x.ContaOrigemId).NotEmpty();
        RuleFor(x => x.ContaDestinoId).NotEmpty();
        RuleFor(x => x.ContaOrigemId).NotEqual(x => x.ContaDestinoId).WithMessage("Conta de origem e destino não podem ser iguais.");
    }
}