using log4net;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using XHTD_SERVICES.Helper;

public class OracleHelper
{
    private string _connectionString;

    public OracleHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<T> GetDataFromOracle<T>(string query, Func<IDataReader, T> mapFunc, OracleParameter[] parameters = null)
    {
        List<T> results = new List<T>();
        using (OracleConnection connection = new OracleConnection(_connectionString))
        {
            connection.SqlNetAllowedLogonVersionClient = OracleAllowedLogonVersionClient.Version11;
            using (OracleCommand command = new OracleCommand(query, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                try
                {
                    connection.Open();
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T result = mapFunc(reader);
                            results.Add(result);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        return results;
    }
}
