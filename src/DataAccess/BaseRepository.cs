using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using Dapper;

namespace Company.TestProject.DataAccess
{
    public abstract class BaseRepository
    {
        protected readonly string connectionString;

        protected BaseRepository(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        protected List<T> Query<T>(string sql, object param = null)
        {
            using (DbConnection conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                return new List<T>(conn.Query<T>(sql, param));
            }
        }

        protected int Execute(string sql, object param = null)
        {
            using (DbConnection conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                return conn.Execute(sql, param);
            }
        }

        protected T ExecuteScalar<T>(string sql, object param = null)
        {
            using (DbConnection conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                return conn.ExecuteScalar<T>(sql, param);
            }
        }
    }
}
