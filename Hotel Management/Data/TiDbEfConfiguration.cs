using System;
using System.Data; // Cần thiết cho ConnectionState
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using MySql.Data.EntityFramework;
using MySql.Data.MySqlClient;

namespace HotelManagement.Data
{
    // EF6 will pick this up via App.config codeConfigurationType or DbConfigurationType attribute
    public class TiDbEfConfiguration : MySqlEFConfiguration
    {
        public TiDbEfConfiguration()
        {
            // Register interceptor at startup
            AddInterceptor(new TiDbConnectionInterceptor());
        }
    }

    // Fix: Thực thi đầy đủ TẤT CẢ các thành viên của interface IDbConnectionInterceptor
    public class TiDbConnectionInterceptor : IDbConnectionInterceptor
    {
        // --- LOGIC CHÍNH CHO TIDB ---
        public void Opened(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
            if (connection is MySqlConnection mysql)
            {
                try
                {
                    using (var cmd = mysql.CreateCommand())
                    {
                        // TiDB doesn't support SERIALIZABLE; this bypasses the check per TiDB docs/error 8048.
                        cmd.CommandText = "SET SESSION tidb_skip_isolation_level_check=1;";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Ignore errors to prevent crash
                }
            }
        }

        // --- CÁC PHƯƠNG THỨC BẮT BUỘC KHÁC (ĐỂ TRỐNG) ---
        public void Opening(DbConnection connection, DbConnectionInterceptionContext interceptionContext) { }

        public void BeganTransaction(DbConnection connection, BeginTransactionInterceptionContext interceptionContext) { }
        public void BeginningTransaction(DbConnection connection, BeginTransactionInterceptionContext interceptionContext) { }

        public void Closed(DbConnection connection, DbConnectionInterceptionContext interceptionContext) { }
        public void Closing(DbConnection connection, DbConnectionInterceptionContext interceptionContext) { }

        public void Disposed(DbConnection connection, DbConnectionInterceptionContext interceptionContext) { }
        public void Disposing(DbConnection connection, DbConnectionInterceptionContext interceptionContext) { }

        public void EnlistedTransaction(DbConnection connection, EnlistTransactionInterceptionContext interceptionContext) { }
        public void EnlistingTransaction(DbConnection connection, EnlistTransactionInterceptionContext interceptionContext) { }

        // Property Interception Methods
        public void ConnectionStringGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }
        public void ConnectionStringGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }
        public void ConnectionStringSet(DbConnection connection, DbConnectionPropertyInterceptionContext<string> interceptionContext) { }
        public void ConnectionStringSetting(DbConnection connection, DbConnectionPropertyInterceptionContext<string> interceptionContext) { }

        public void ConnectionTimeoutGetting(DbConnection connection, DbConnectionInterceptionContext<int> interceptionContext) { }
        public void ConnectionTimeoutGot(DbConnection connection, DbConnectionInterceptionContext<int> interceptionContext) { }

        public void DatabaseGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }
        public void DatabaseGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }

        public void DataSourceGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }
        public void DataSourceGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }

        public void ServerVersionGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }
        public void ServerVersionGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext) { }

        public void StateGetting(DbConnection connection, DbConnectionInterceptionContext<ConnectionState> interceptionContext) { }
        public void StateGot(DbConnection connection, DbConnectionInterceptionContext<ConnectionState> interceptionContext) { }
    }
}