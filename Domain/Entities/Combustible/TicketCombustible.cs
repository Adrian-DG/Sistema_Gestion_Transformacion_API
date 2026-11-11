using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Domain.Entities.Combustible
{
    /// <summary>
    /// Ticket de combustible. Su número lo genera el sistema en secuencia anual (TC-2026-000001).
    /// Se consume completo; quien lo recibe puede cederlo, solo se controla
    /// a quién se entregó y cuándo fue utilizado.
    /// El ticket impreso lleva un código de barras con el número y un <see cref="CodigoVerificacion"/>
    /// aleatorio: como el número es secuencial, sin el código bastaría adivinarlo para falsificar un ticket.
    /// </summary>
    public partial class TicketCombustible : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public const int LongitudCodigoVerificacion = 6;

        /// <summary>Sin caracteres que se confunden al leerlos (0/O, 1/I/L, 2/Z, 5/S, 8/B).</summary>
        private const string AlfabetoVerificacion = "ACDEFGHJKMNPQRTUVWXY34679";
        /// <summary>Código de la <see cref="Configuracion.Secuencia"/> que numera los tickets.</summary>
        public const string CodigoSecuencia = "TicketCombustible";

        public static string FormatearNumero(int anio, long secuencia) => $"TC-{anio}-{secuencia:D6}";

        public required string Numero { get; set; }
        public TipoCombustible TipoCombustible { get; set; }
        public decimal Galones { get; set; }
        public EstatusTicket Estatus { get; set; }

        public Guid SolicitudTicketId { get; set; }
        public virtual SolicitudTicket? SolicitudTicket { get; set; }

        /// <summary>Nulo en los tickets registrados antes de que se imprimieran con código de barras.</summary>
        public string? CodigoVerificacion { get; set; }

        /// <summary>Fecha y hora del despacho (canje).</summary>
        public DateTime? FechaUso { get; set; }
        /// <summary>Usuario (operador) que despachó el combustible.</summary>
        public Guid? DespachadoPor { get; set; }

        /// <summary>Contenido del código de barras del ticket impreso: TC-2026-000001-K7QX3M.</summary>
        [NotMapped]
        public string CodigoBarras => CodigoVerificacion is null ? Numero : $"{Numero}-{CodigoVerificacion}";

        public static string GenerarCodigoVerificacion() =>
            RandomNumberGenerator.GetString(AlfabetoVerificacion, LongitudCodigoVerificacion);

        /// <summary>
        /// Número y código de verificación de lo escaneado o escrito. Tolera minúsculas y separadores
        /// distintos del guion: un lector configurado con otra distribución de teclado puede enviar
        /// "TC'2026'000001'K7QX3M". Si no tiene la forma de un ticket, se devuelve el texto tal cual.
        /// </summary>
        public static (string Numero, string? Codigo) InterpretarCodigo(string texto)
        {
            var limpio = texto.Trim().ToUpperInvariant();
            var m = FormatoCodigo().Match(limpio);
            return m.Success
                ? (FormatearNumero(int.Parse(m.Groups["anio"].Value), long.Parse(m.Groups["sec"].Value)),
                   m.Groups["cod"].Success ? m.Groups["cod"].Value : null)
                : (limpio, null);
        }

        [GeneratedRegex(@"^TC[^A-Z0-9]?(?<anio>\d{4})[^A-Z0-9]?(?<sec>\d{6})(?:[^A-Z0-9]?(?<cod>[A-Z0-9]{6}))?$")]
        private static partial Regex FormatoCodigo();

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public Result Utilizar(DateTime fechaUso, Guid? despachadoPor = null)
        {
            if (Estatus == EstatusTicket.Utilizado && FechaUso is { } usado)
            {
                return Result.Failure(TicketCombustibleErrors.YaUtilizado(Numero, usado));
            }

            if (Estatus != EstatusTicket.Disponible)
            {
                return Result.Failure(TicketCombustibleErrors.NoDisponible(Numero, Estatus.ToString()));
            }

            Estatus = EstatusTicket.Utilizado;
            FechaUso = fechaUso;
            DespachadoPor = despachadoPor;
            return Result.Success();
        }
    }
}
