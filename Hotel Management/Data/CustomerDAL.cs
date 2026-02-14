using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class CustomerDAL
    {
        public List<Customer> GetAll()
        {
            var list = new List<Customer>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = "SELECT KhachHangID, HoTen, CCCD, DienThoai, DiaChi FROM KHACHHANG";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Customer
                        {
                            KhachHangID = rd.GetInt32(0),
                            HoTen = rd.GetString(1),
                            CCCD = rd.GetString(2),
                            DienThoai = rd.IsDBNull(3) ? null : rd.GetString(3),
                            DiaChi = rd.IsDBNull(4) ? null : rd.GetString(4)
                        });
                    }
                }
            }
            return list;
        }

        public void Insert(Customer c)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"INSERT INTO KHACHHANG (HoTen, CCCD, DienThoai, DiaChi)
                                 VALUES(@HoTen, @CCCD, @DienThoai, @DiaChi)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@HoTen", c.HoTen);
                cmd.Parameters.AddWithValue("@CCCD", c.CCCD);
                cmd.Parameters.AddWithValue("@DienThoai", (object)c.DienThoai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiaChi", (object)c.DiaChi ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Customer c)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"UPDATE KHACHHANG
                                 SET HoTen = @HoTen,
                                     CCCD = @CCCD,
                                     DienThoai = @DienThoai,
                                     DiaChi = @DiaChi
                                 WHERE KhachHangID = @KhachHangID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@HoTen", c.HoTen);
                cmd.Parameters.AddWithValue("@CCCD", c.CCCD);
                cmd.Parameters.AddWithValue("@DienThoai", (object)c.DienThoai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiaChi", (object)c.DiaChi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KhachHangID", c.KhachHangID);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = "DELETE FROM KHACHHANG WHERE KhachHangID = @Id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
