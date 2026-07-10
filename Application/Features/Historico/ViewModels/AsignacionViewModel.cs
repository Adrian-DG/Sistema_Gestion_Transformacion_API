namespace Application.Features.Historico.ViewModels;

public class AsignacionViewModel
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public Guid VehiculoId { get; set; }
    public DateOnly FechaEfectividad { get; set; }
    public string? Motivo { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public int CantidadAdjuntos { get; set; }
}
