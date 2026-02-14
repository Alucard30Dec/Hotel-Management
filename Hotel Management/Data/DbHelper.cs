using System;
using System.Configuration;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace HotelManagement.Data
{
    public static class DbHelper
    {
        // Reads connection string from App.config (connectionStrings name = "HotelDb")
        private static readonly string _connectionString;

        static DbHelper()
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["HotelDb"];
                _connectionString = cs?.ConnectionString ?? throw new InvalidOperationException("Connection string 'HotelDb' not found in App.config.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Cannot read connection string from App.config.\n\nDetails:\n" + ex.Message,
                    "Configuration error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Returns an opened MySqlConnection. Shows friendly message boxes on failure and rethrows.
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();
                return conn;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(
                    "Cannot connect to TiDB / MySQL.\nPlease check the connection string in App.config and network access to TiDB Cloud.\n\nDetails:\n" + ex.Message,
                    "Database connection error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unexpected error when opening database connection.\n\nDetails:\n" + ex.Message,
                    "Database connection error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }
    }
}
