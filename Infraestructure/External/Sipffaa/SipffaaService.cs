using Application.Contracts.Externos;
using Dapper;

namespace Infraestructure.External.Sipffaa
{
    public class SipffaaService(ISipffaaConnectionFactory connectionFactory) : ISipffaaService
    {
        private const int CommandTimeoutSeconds = 30;
        private const int MaxOcupantesPorCargo = 10;

        public async Task<MiembroExternoDto?> GetMiembroByCedulaAsync(string cedula, CancellationToken cancellationToken)
        {
            using var connection = connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SipffaaQueries.MiembroPorCedula,
                new { Cedula = NormalizarCedula(cedula) },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<MiembroExternoDto>(command);
        }

        /// <summary>Si el texto son solo dígitos (con o sin guiones) busca por inicio de cédula; si no, por nombre.</summary>
        public async Task<IReadOnlyList<MiembroExternoDto>> BuscarMiembrosAsync(string texto, int maxResultados, CancellationToken cancellationToken)
        {
            using var connection = connectionFactory.CreateConnection();

            var limpio = texto.Trim();
            var digitos = NormalizarCedula(limpio);
            var esCedula = digitos.Length > 0 && digitos.All(char.IsDigit);

            var command = new CommandDefinition(
                SipffaaQueries.MiembrosPorTexto,
                new
                {
                    CedulaPrefijo = esCedula ? digitos : null,
                    Patron = esCedula ? null : $"%{EscaparLike(limpio)}%",
                    MaxResultados = maxResultados
                },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            return (await connection.QueryAsync<MiembroExternoDto>(command)).AsList();
        }

        public async Task<IReadOnlyList<CargoExternoDto>> BuscarCargosAsync(string nombre, int maxResultados, CancellationToken cancellationToken)
        {
            using var connection = connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SipffaaQueries.CargosPorNombre,
                new { Patron = $"%{EscaparLike(nombre.Trim())}%", MaxResultados = maxResultados },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var cargos = (await connection.QueryAsync<CargoExternoDto>(command)).AsList();
            if (cargos.Count == 0) return cargos;

            var ocupantesCommand = new CommandDefinition(
                SipffaaQueries.OcupantesPorCargos,
                new { CodCargos = cargos.Select(c => c.CodCargo).ToArray(), MaxOcupantes = MaxOcupantesPorCargo },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var ocupantes = (await connection.QueryAsync<OcupanteCargoExternoDto>(ocupantesCommand))
                .ToLookup(o => o.CodCargo);

            foreach (var cargo in cargos)
            {
                cargo.Ocupantes = ocupantes[cargo.CodCargo].ToList();
            }

            return cargos;
        }

        public async Task<IReadOnlyList<DepartamentoExternoDto>> BuscarDepartamentosAsync(string nombre, int maxResultados, CancellationToken cancellationToken)
        {
            using var connection = connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SipffaaQueries.DepartamentosPorNombre,
                new { Patron = $"%{EscaparLike(nombre.Trim())}%", MaxResultados = maxResultados },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            return (await connection.QueryAsync<DepartamentoExternoDto>(command)).AsList();
        }

        public async Task<DepartamentoExternoDto?> GetDepartamentoAsync(int codDepartamento, CancellationToken cancellationToken)
        {
            using var connection = connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SipffaaQueries.DepartamentoPorCodigo,
                new { CodDepartamento = codDepartamento },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<DepartamentoExternoDto>(command);
        }

        /// <summary>Acepta la cédula con o sin guiones (001-1234567-8 / 00112345678).</summary>
        private static string NormalizarCedula(string cedula) => cedula.Trim().Replace("-", string.Empty);

        private static string EscaparLike(string value) =>
            value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
