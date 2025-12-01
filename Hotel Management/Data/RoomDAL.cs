using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class RoomDAL
    {
        // Lấy tất cả phòng
        public List<Room> GetAll()
        {
            var list = new List<Room>();

            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT PhongID, MaPhong, LoaiPhongID, Tang, TrangThai, GhiChu,
                                        ThoiGianBatDau, KieuThue, TenKhachHienThi
                                 FROM PHONG";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var room = new Room
                        {
                            PhongID = rd.GetInt32(0),
                            MaPhong = rd.GetString(1),
                            LoaiPhongID = rd.GetInt32(2),
                            Tang = rd.GetInt32(3),
                            TrangThai = rd.GetInt32(4),
                            GhiChu = rd.IsDBNull(5) ? null : rd.GetString(5),
                            ThoiGianBatDau = rd.IsDBNull(6) ? (DateTime?)null : rd.GetDateTime(6),
                            KieuThue = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7),
                            TenKhachHienThi = rd.IsDBNull(8) ? null : rd.GetString(8)
                        };

                        list.Add(room);
                    }
                }
            }

            return list;
        }

        // Lấy 1 phòng theo ID (dùng nếu cần)
        public Room GetById(int id)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT PhongID, MaPhong, LoaiPhongID, Tang, TrangThai, GhiChu,
                                        ThoiGianBatDau, KieuThue, TenKhachHienThi
                                 FROM PHONG
                                 WHERE PhongID = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            return new Room
                            {
                                PhongID = rd.GetInt32(0),
                                MaPhong = rd.GetString(1),
                                LoaiPhongID = rd.GetInt32(2),
                                Tang = rd.GetInt32(3),
                                TrangThai = rd.GetInt32(4),
                                GhiChu = rd.IsDBNull(5) ? null : rd.GetString(5),
                                ThoiGianBatDau = rd.IsDBNull(6) ? (DateTime?)null : rd.GetDateTime(6),
                                KieuThue = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7),
                                TenKhachHienThi = rd.IsDBNull(8) ? null : rd.GetString(8)
                            };
                        }
                    }
                }
            }

            return null;
        }

        // Thêm phòng mới
        public void Insert(Room room)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"INSERT INTO PHONG
                                 (MaPhong, LoaiPhongID, Tang, TrangThai, GhiChu,
                                  ThoiGianBatDau, KieuThue, TenKhachHienThi)
                                 VALUES
                                 (@MaPhong, @LoaiPhongID, @Tang, @TrangThai, @GhiChu,
                                  @ThoiGianBatDau, @KieuThue, @TenKhachHienThi)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", room.MaPhong);
                    cmd.Parameters.AddWithValue("@LoaiPhongID", room.LoaiPhongID);
                    cmd.Parameters.AddWithValue("@Tang", room.Tang);
                    cmd.Parameters.AddWithValue("@TrangThai", room.TrangThai);

                    if (string.IsNullOrWhiteSpace(room.GhiChu))
                        cmd.Parameters.AddWithValue("@GhiChu", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@GhiChu", room.GhiChu);

                    if (room.ThoiGianBatDau.HasValue)
                        cmd.Parameters.AddWithValue("@ThoiGianBatDau", room.ThoiGianBatDau.Value);
                    else
                        cmd.Parameters.AddWithValue("@ThoiGianBatDau", DBNull.Value);

                    if (room.KieuThue.HasValue)
                        cmd.Parameters.AddWithValue("@KieuThue", room.KieuThue.Value);
                    else
                        cmd.Parameters.AddWithValue("@KieuThue", DBNull.Value);

                    if (string.IsNullOrWhiteSpace(room.TenKhachHienThi))
                        cmd.Parameters.AddWithValue("@TenKhachHienThi", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@TenKhachHienThi", room.TenKhachHienThi);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Cập nhật phòng (sử dụng khi quản lý danh mục phòng)
        public void Update(Room room)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"UPDATE PHONG SET
                                    MaPhong        = @MaPhong,
                                    LoaiPhongID    = @LoaiPhongID,
                                    Tang           = @Tang,
                                    TrangThai      = @TrangThai,
                                    GhiChu         = @GhiChu,
                                    ThoiGianBatDau = @ThoiGianBatDau,
                                    KieuThue       = @KieuThue,
                                    TenKhachHienThi = @TenKhachHienThi
                                 WHERE PhongID = @PhongID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    cmd.Parameters.AddWithValue("@MaPhong", room.MaPhong);
                    cmd.Parameters.AddWithValue("@LoaiPhongID", room.LoaiPhongID);
                    cmd.Parameters.AddWithValue("@Tang", room.Tang);
                    cmd.Parameters.AddWithValue("@TrangThai", room.TrangThai);

                    if (string.IsNullOrWhiteSpace(room.GhiChu))
                        cmd.Parameters.AddWithValue("@GhiChu", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@GhiChu", room.GhiChu);

                    if (room.ThoiGianBatDau.HasValue)
                        cmd.Parameters.AddWithValue("@ThoiGianBatDau", room.ThoiGianBatDau.Value);
                    else
                        cmd.Parameters.AddWithValue("@ThoiGianBatDau", DBNull.Value);

                    if (room.KieuThue.HasValue)
                        cmd.Parameters.AddWithValue("@KieuThue", room.KieuThue.Value);
                    else
                        cmd.Parameters.AddWithValue("@KieuThue", DBNull.Value);

                    if (string.IsNullOrWhiteSpace(room.TenKhachHienThi))
                        cmd.Parameters.AddWithValue("@TenKhachHienThi", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@TenKhachHienThi", room.TenKhachHienThi);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Xoá phòng
        public void Delete(int phongId)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"DELETE FROM PHONG WHERE PhongID = @PhongID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PhongID", phongId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Cập nhật trạng thái + ghi chú + thời gian + kiểu thuê + tên khách
        // Dùng trong RoomDetailForm khi nhấn Lưu
        public void UpdateTrangThaiFull(int phongId,
                                        int trangThai,
                                        string ghiChu,
                                        DateTime? thoiGianBatDau,
                                        int? kieuThue,
                                        string tenKhachHienThi)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"UPDATE PHONG SET
                                    TrangThai       = @TrangThai,
                                    GhiChu          = @GhiChu,
                                    ThoiGianBatDau  = @ThoiGianBatDau,
                                    KieuThue        = @KieuThue,
                                    TenKhachHienThi = @TenKhachHienThi
                                 WHERE PhongID = @PhongID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TrangThai", trangThai);

                    if (string.IsNullOrWhiteSpace(ghiChu))
                        cmd.Parameters.AddWithValue("@GhiChu", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@GhiChu", ghiChu);

                    if (thoiGianBatDau.HasValue)
                        cmd.Parameters.AddWithValue("@ThoiGianBatDau", thoiGianBatDau.Value);
                    else
                        cmd.Parameters.AddWithValue("@ThoiGianBatDau", DBNull.Value);

                    if (kieuThue.HasValue)
                        cmd.Parameters.AddWithValue("@KieuThue", kieuThue.Value);
                    else
                        cmd.Parameters.AddWithValue("@KieuThue", DBNull.Value);

                    if (string.IsNullOrWhiteSpace(tenKhachHienThi))
                        cmd.Parameters.AddWithValue("@TenKhachHienThi", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@TenKhachHienThi", tenKhachHienThi);

                    cmd.Parameters.AddWithValue("@PhongID", phongId);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
