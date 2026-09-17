using Microsoft.Data.SqlClient;
using System.Data;

namespace HKERP.Infrastructure.Common
{
    public interface ISqlHelper
    {
        DataSet ExecuteSpWithXml(string xml, string spName);
        DataSet ExecuteSpNoParams(string spName);
    }

    public class SqlHelper : ISqlHelper
    {
        private readonly string _connectionString;

        public SqlHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DataSet ExecuteSpWithXml(string xml, string spName)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                cmd.Parameters.AddWithValue("@XML", xml);

                DataSet ds = new DataSet();
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(ds);
                }
                return ds;
            }
        }

        public DataSet ExecuteSpNoParams(string spName)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;

                DataSet ds = new DataSet();
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(ds);
                }
                return ds;
            }
        }
    }
}
