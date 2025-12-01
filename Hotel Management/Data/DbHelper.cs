using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HotelManagement.Data
{
    public static class DbHelper
    {
        // *** CHỖ NÀY BẠN CẦN CHỈNH LẠI THEO SQL SERVER CỦA BẠN ***

        // Nếu bạn dùng SQL Server Express trên máy:
        // private static readonly string _connectionString =
        //     @"Data Source=.\SQLEXPRESS;Initial Catalog=HotelDb;Integrated Security=True";

        // Nếu bạn dùng LocalDB (VS cài sẵn):
        // private static readonly string _connectionString =
        //     @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=HotelDb;Integrated Security=True";

        // Tạm thời mình để LocalDB – nếu bạn đang dùng SQLEXPRESS thì đổi lại dòng trên.
        private static readonly string _connectionString =
            @"Data Source=ALUCARD;Initial Catalog=HotelDb;Integrated Security=True";

        /// <summary>
        /// Lấy SqlConnection đã mở sẵn.
        /// Nếu lỗi sẽ hiện message box tiếng Việt và ném exception ra ngoài.
        /// </summary>
        public static SqlConnection GetConnection()
        {
            try
            {
                var conn = new SqlConnection(_connectionString);
                conn.Open();
                return conn;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Không kết nối được đến SQL Server.\n" +
                    "Vui lòng kiểm tra lại DbHelper._connectionString và database HotelDb.\n\n" +
                    "Chi tiết lỗi:\n" + ex.Message,
                    "Lỗi kết nối CSDL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                throw; // ném lỗi tiếp để code gọi biết là đã lỗi
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Đã xảy ra lỗi không mong muốn khi kết nối CSDL.\n\n" +
                    "Chi tiết lỗi:\n" + ex.Message,
                    "Lỗi kết nối CSDL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                throw;
            }
        }
    }
}
