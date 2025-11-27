using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HotelManagement.Data
{
    // Lớp helper dùng để tạo kết nối đến SQL Server
    public static class DbHelper
    {
        /*
         * 1. CHỈNH SỬA Ở ĐÂY:
         * 
         * - Nếu bạn dùng SQL Server Express với instance SQLEXPRESS
         *   => Server = .\SQLEXPRESS
         * 
         * - Nếu trong SSMS bạn kết nối bằng (localdb)\MSSQLLocalDB
         *   => Server = (localdb)\MSSQLLocalDB
         * 
         * - Nếu bạn kết nối bằng chỉ "localhost"
         *   => Server = localhost
         * 
         * NHỚ: Database phải đúng tên "HotelDb" (đã tạo bằng script).
         */

        private static readonly string _connectionString =
            @"Server=ALUCARD;Database=HotelDb;Trusted_Connection=True;";

        // Hàm trả về một SqlConnection đã mở
        public static SqlConnection GetConnection()
        {
            try
            {
                var conn = new SqlConnection(_connectionString);
                conn.Open(); // mở kết nối
                return conn;
            }
            catch (Exception ex)
            {
                // Hiện thông báo dễ hiểu cho bạn
                MessageBox.Show(
                    "Không kết nối được đến SQL Server.\n" +
                    "Vui lòng kiểm tra lại DbHelper._connectionString và database HotelDb.\n\n" +
                    "Chi tiết lỗi:\n" + ex.Message,
                    "Lỗi kết nối CSDL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw; // ném lại để biết chỗ lỗi nếu cần
            }
        }
    }
}
