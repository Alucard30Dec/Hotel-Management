using System;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class UserDAL
    {
        public User Login(string username, string password)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT UserID, Username, `Password`, `Role`
                                 FROM USERS
                                 WHERE Username = @Username
                                   AND `Password` = @Password
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                using (var reader = cmd.ExecuteReader())
                {
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
            }
            return null; // not found -> login failed
        }

        public bool UpdateRole(string username, string role, string actor = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.", nameof(username));
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role is required.", nameof(role));

            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"UPDATE USERS
                                 SET `Role` = @Role,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'active'
                                 WHERE Username = @Username
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Role", role.Trim());
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@UpdatedBy", AuditContext.ResolveActor(actor));
                cmd.Parameters.AddWithValue("@Username", username.Trim());

                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
