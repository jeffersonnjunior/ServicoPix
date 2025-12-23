namespace ServicoPix.Application.UseCases.SolicitarPix.Queries;

public class PixDto
{
    public Guid Id { get; set; }
    public decimal Valor { get; set; }
    public string Status { get; set; }
}
