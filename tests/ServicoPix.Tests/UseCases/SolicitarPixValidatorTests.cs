using ServicoPix.Application.UseCases.SolicitarPix.Commands;
using Xunit;

namespace ServicoPix.Tests.UseCases;

public class SolicitarPixValidatorTests
{
    [Fact]
    public void Validator_accepts_valid_command()
    {
        var validator = new SolicitarPixValidator();

        var cmd = new SolicitarPixCommand
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaDestinoId = Guid.NewGuid(),
            Valor = 10m
        };

        var result = validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_rejects_non_positive_value(decimal valor)
    {
        var validator = new SolicitarPixValidator();

        var cmd = new SolicitarPixCommand
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaDestinoId = Guid.NewGuid(),
            Valor = valor
        };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Valor deve ser positivo"));
    }

    [Fact]
    public void Validator_rejects_same_origin_and_destination()
    {
        var validator = new SolicitarPixValidator();

        var id = Guid.NewGuid();
        var cmd = new SolicitarPixCommand
        {
            ContaOrigemId = id,
            ContaDestinoId = id,
            Valor = 10m
        };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("não podem ser iguais"));
    }
}
