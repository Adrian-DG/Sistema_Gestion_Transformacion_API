using Application.Features.Configuracion;

namespace Application.Contracts.Reportes;

/// <summary>Genera documentos PDF con el encabezado de la plantilla de reportes.</summary>
public interface IPdfReportGenerator
{
    /// <summary>
    /// Constancia de una asignación de vehículo. Si <paramref name="encabezado"/> no trae logos se usa
    /// el logo predeterminado del sistema.
    /// </summary>
    byte[] ConstanciaAsignacion(DatosReportePdf encabezado, ConstanciaAsignacion datos);

    /// <summary>
    /// Tickets de una solicitud listos para recortar, cada uno con su código de barras (número y código
    /// de verificación) para escanearlo en el despacho.
    /// </summary>
    byte[] TicketsCombustible(DatosReportePdf encabezado, TicketsImpresion datos);
}

/// <param name="CodigoBarras">Contenido del código de barras: número y código de verificación.</param>
public record TicketImpresion(
    string Numero,
    string CodigoBarras,
    string? CodigoVerificacion,
    string TipoCombustible,
    decimal Galones);

public record TicketsImpresion(
    string EntregadoA,
    string? EntregadoACedula,
    string Proposito,
    DateTime? FechaAprobacion,
    IReadOnlyList<TicketImpresion> Tickets,
    DateTime Generado);

public record PersonaConstancia(string Nombre, string Cedula, string? Rango);

public record VehiculoConstancia(
    string Placa,
    string? Ficha,
    string Marca,
    string Modelo,
    int Fabricacion,
    string Color,
    string Tipo,
    string Chasis,
    string Matricula,
    int Kilometraje);

public record ConstanciaAsignacion(
    Guid Id,
    string Estatus,
    bool EsValida,
    DateTime FechaSolicitud,
    string RegistradoPor,
    DateTime? FechaAprobacion,
    string? AprobadoPor,
    PersonaConstancia Solicitante,
    string CargoSolicitante,
    string DependenciaSolicitante,
    PersonaConstancia Asignado,
    string? CargoAsignado,
    PersonaConstancia Chofer,
    VehiculoConstancia Vehiculo,
    DateTime FechaSalida,
    DateTime FechaRetornoEstimada,
    DateTime? FechaRetornoReal,
    string Proposito,
    string? Destino,
    string? MotivoRechazoCancelacion,
    DateTime Generado)
{
    /// <summary>Referencia corta para el documento (primeros 8 caracteres del Id).</summary>
    public string Referencia => Id.ToString("N")[..8].ToUpperInvariant();
}
