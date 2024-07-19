using log4net;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using XHTD_SERVICES.Helper;

public class OracleLogger : BaseLogger<OracleHelper>
{

}
public class OracleHelper
{
    private string _connectionString;
    protected readonly OracleLogger _oracleLogger;

    public OracleHelper(string connectionString)
    {
        _connectionString = connectionString;
        _oracleLogger = new OracleLogger();
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
                catch (Exception ex)
                {
                    _oracleLogger.LogError(ex.Message);
                    _oracleLogger.LogError(ex.StackTrace);
                }
            }
        }
        return results;
    }
}
