using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace SmkcApi.Repositories
{
    public interface IOracleConnectionFactory
    {
        OracleConnection Create();
    }

    public class OracleConnectionFactory : IOracleConnectionFactory
    {
        private readonly string _cs;
        public OracleConnectionFactory(string connectionStringName = "OracleDb")
        {
            _cs = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
        }
        public OracleConnection Create() => new OracleConnection(_cs);
    }
}
