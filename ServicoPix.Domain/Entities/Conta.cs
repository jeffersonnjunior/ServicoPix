using ServicoPix.Domain.Exceptions;

namespace ServicoPix.Domain.Entities;

public class Conta
{
    public Guid Id { get; private set; }
    public string Numero { get; private set; }
    public decimal Saldo { get; private set; }

    protected Conta() { }

    public Conta(Guid id, string numero, decimal saldoInicial)
    {
        Id = id;
        Numero = numero;
        Saldo = saldoInicial;
    }

    public void Debitar(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("O valor do débito deve ser maior que zero.");

        if (Saldo < valor)
            throw new DomainException("Saldo insuficiente para realizar a transação.");

        Saldo -= valor;
    }

    public void Creditar(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("O valor do crédito deve ser maior que zero.");

        Saldo += valor;
    }
}