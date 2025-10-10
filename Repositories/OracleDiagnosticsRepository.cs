using System;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace SmkcApi.Repositories
{
    public interface IOracleDiagnosticsRepository
    {
        Task<DateTime> GetServerTimeAsync();
    }

    public class OracleDiagnosticsRepository : IOracleDiagnosticsRepository
    {
        private readonly IOracleConnectionFactory _factory;
        public OracleDiagnosticsRepository(IOracleConnectionFactory factory) => _factory = factory;

        public async Task<DateTime> GetServerTimeAsync()
        {
            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand("SELECT SYSDATE FROM DUAL", conn))
            {
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToDateTime(result);
            }
        }
    }
}
