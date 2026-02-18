using System;
using System.Data.Entity;
using System.Windows.Forms;
using Hotel_Management.Migrations;
using HotelManagement.Data;
using HotelManagement.Forms; // namespace chứa MainForm
using MySql.Data.MySqlClient;

namespace HotelManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<HotelDbContext, Configuration>());
            using (var db = new HotelDbContext())
            {
                db.Database.Initialize(false);
            }
            EnsurePerformanceIndexes();

            Application.Run(new MainForm());
        }

        private static void EnsurePerformanceIndexes()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_NGAYDEN", "CREATE INDEX IDX_DATPHONG_NGAYDEN ON DATPHONG (NgayDen)");
                    EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_TRANGTHAI", "CREATE INDEX IDX_DATPHONG_TRANGTHAI ON DATPHONG (TrangThai)");
                    EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_PHONGID", "CREATE INDEX IDX_DATPHONG_PHONGID ON DATPHONG (PhongID)");
                    EnsureIndex(conn, "PHONG", "IDX_PHONG_TRANGTHAI", "CREATE INDEX IDX_PHONG_TRANGTHAI ON PHONG (TrangThai)");
                    EnsureIndex(conn, "HOADON", "IDX_HOADON_NGAYLAP", "CREATE INDEX IDX_HOADON_NGAYLAP ON HOADON (NgayLap)");
                }
            }
            catch
            {
                // Ignore index creation errors to avoid blocking application startup.
            }
        }

        private static void EnsureIndex(MySqlConnection conn, string tableName, string indexName, string createSql)
        {
            string checkSql = @"SELECT COUNT(1)
                                FROM INFORMATION_SCHEMA.STATISTICS
                                WHERE TABLE_SCHEMA = DATABASE()
                                  AND TABLE_NAME = @TableName
                                  AND INDEX_NAME = @IndexName";

            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@TableName", tableName);
                checkCmd.Parameters.AddWithValue("@IndexName", indexName);
                bool exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (exists) return;
            }

            using (var createCmd = new MySqlCommand(createSql, conn))
            {
                createCmd.ExecuteNonQuery();
            }
        }
    }
}
