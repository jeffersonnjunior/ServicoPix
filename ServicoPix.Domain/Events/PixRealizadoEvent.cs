namespace ServicoPix.Domain.Events;

public class PixRealizadoEvent
{
    public Guid TransacaoId { get; set; }
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataProcessamento { get; set; }
    public string Status { get; set; }
}