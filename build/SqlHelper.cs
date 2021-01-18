using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

public static class SqlHelper
{
    public static int Execute(string connectionString, string sql, params DbParameter[] prms)
    {
        return Execute(connectionString, CommandType.Text, sql, prms);
    }

    public static int Execute(string connectionString, CommandType commandType, string sql, params DbParameter[] prms)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = conn.ConnectionTimeout;
                cmd.CommandText = sql;
                cmd.CommandType = commandType;

                if (prms != null && prms.Any())
                {
                    cmd.Parameters.AddRange(prms);
                }

                return cmd.ExecuteNonQuery();
            }
        }
    }

    public static List<T> Query<T>(string connectionString, string sql, Func<DbDataReader, T> mapping, params DbParameter[] prms)
    {
        var result = new List<T>();

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = conn.ConnectionTimeout;
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                if (prms != null && prms.Any())
                {
                    cmd.Parameters.AddRange(prms);
                }

                using (DbDataReader dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {
                            result.Add(mapping(dataReader));
                        }
                    }
                }
            }
        }

        return result;
    }

    public static T ExecuteScalar<T>(string connectionString, string query, params DbParameter[] prms)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = conn.ConnectionTimeout;
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                if (prms != null && prms.Any())
                {
                    cmd.Parameters.AddRange(prms);
                }

                var resultData = cmd.ExecuteScalar();

                return (T)Convert.ChangeType(resultData, typeof(T));
            }
        }
    }
}