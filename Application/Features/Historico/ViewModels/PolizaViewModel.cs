namespace Application.Features.Historico.ViewModels;

public class PolizaViewModel
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public DateOnly FechaExpedicion { get; set; }
    public DateOnly FechaEfectividad { get; set; }
    public DateOnly FechaVencimiento { get; set; }
}
