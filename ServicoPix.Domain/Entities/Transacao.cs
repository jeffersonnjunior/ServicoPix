using ServicoPix.Domain.Enums;

namespace ServicoPix.Domain.Entities;

public class Transacao
{
    public Guid Id { get; private set; }
    public Guid ContaOrigemId { get; private set; }
    public Guid ContaDestinoId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public StatusTransacao Status { get; private set; }
    public string? MensagemErro { get; private set; }

    protected Transacao() { }

    public Transacao(Guid id, Guid origem, Guid destino, decimal valor)
    {
        Id = id;
        ContaOrigemId = origem;
        ContaDestinoId = destino;
        Valor = valor;
        DataCriacao = DateTime.UtcNow;
        Status = StatusTransacao.Pendente;
    }

    public void ConcluirComSucesso()
    {
        Status = StatusTransacao.Concluido;
    }

    public void Falhar(string motivo)
    {
        Status = StatusTransacao.Falha;
        MensagemErro = motivo;
    }
}