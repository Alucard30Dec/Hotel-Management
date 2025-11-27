using HotelManagement.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace HotelManagement.Data
{
    public class RoomDAL
    {
        // Lấy toàn bộ danh sách phòng
        public List<Room> GetAll()
        {
            var rooms = new List<Room>();
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = "SELECT PhongID, MaPhong, LoaiPhongID, TrangThai, GhiChu FROM PHONG";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rooms.Add(new Room
                    {
                        PhongID = reader.GetInt32(0),
                        MaPhong = reader.GetString(1),
                        LoaiPhongID = reader.GetInt32(2),
                        TrangThai = reader.GetInt32(3),
                        GhiChu = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }
            return rooms;
        }

        // Thêm phòng mới
        public void Insert(Room room)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"INSERT INTO PHONG(MaPhong, LoaiPhongID, TrangThai, GhiChu)
                                 VALUES(@MaPhong, @LoaiPhongID, @TrangThai, @GhiChu)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MaPhong", room.MaPhong);
                cmd.Parameters.AddWithValue("@LoaiPhongID", room.LoaiPhongID);
                cmd.Parameters.AddWithValue("@TrangThai", room.TrangThai);
                cmd.Parameters.AddWithValue("@GhiChu", (object)room.GhiChu ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // Cập nhật phòng
        public void Update(Room room)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"UPDATE PHONG
                                 SET MaPhong = @MaPhong,
                                     LoaiPhongID = @LoaiPhongID,
                                     TrangThai = @TrangThai,
                                     GhiChu = @GhiChu
                                 WHERE PhongID = @PhongID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MaPhong", room.MaPhong);
                cmd.Parameters.AddWithValue("@LoaiPhongID", room.LoaiPhongID);
                cmd.Parameters.AddWithValue("@TrangThai", room.TrangThai);
                cmd.Parameters.AddWithValue("@GhiChu", (object)room.GhiChu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                cmd.ExecuteNonQuery();
            }
        }

        // Xoá phòng
        public void Delete(int phongID)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = "DELETE FROM PHONG WHERE PhongID = @PhongID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhongID", phongID);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
