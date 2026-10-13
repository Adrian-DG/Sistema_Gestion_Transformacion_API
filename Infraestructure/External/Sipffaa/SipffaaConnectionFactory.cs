using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Infraestructure.External.Sipffaa
{
    public interface ISipffaaConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class SipffaaConnectionFactory(IConfiguration configuration) : ISipffaaConnectionFactory
    {
        public const string ConnectionStringName = "SipffaaConnection";

        private readonly string _connectionString = configuration.GetConnectionString(ConnectionStringName) is { Length: > 0 } cs
            ? cs
            : throw new InvalidOperationException(
                $"La cadena de conexión '{ConnectionStringName}' no está configurada (ver user-secrets del proyecto API).");

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
