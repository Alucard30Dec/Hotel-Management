using System.Data.SqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class UserDAL
    {
        public User Login(string username, string password)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT UserID, Username, [Password], [Role]
                                 FROM USERS
                                 WHERE Username = @Username AND [Password] = @Password";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserID = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Password = reader.GetString(2),
                        Role = reader.GetString(3)
                    };
                }
            }
            return null; // không tìm thấy -> đăng nhập sai
        }
    }
}
