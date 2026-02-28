using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace HotelManagement.Data
{
    public static class DbHelper
    {
        // Reads connection string from App.config (connectionStrings name = "HotelDb")
        private static readonly string _connectionString;

        static DbHelper()
        {
            var cs = ConfigurationManager.ConnectionStrings["HotelDb"];
            _connectionString = cs?.ConnectionString ?? throw new InvalidOperationException("Connection string 'HotelDb' not found in App.config.");
        }

        /// <summary>
        /// Returns an opened MySqlConnection.
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
