namespace ServicoPix.Application.UseCases.EfetivarPix.Queries;

public class DetalheEfetivacaoDto
{
    public Guid TransacaoId { get; set; }
    public string Status { get; set; }
    public DateTime DataProcessamento { get; set; }
    public string? MotivoFalha { get; set; } 
}