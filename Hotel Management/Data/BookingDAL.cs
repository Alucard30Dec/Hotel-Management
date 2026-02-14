using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class BookingDAL                                                         
    {
        public List<Booking> GetAll()
        {
            var list = new List<Booking>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc 
                                 FROM DATPHONG";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Booking
                        {
                            DatPhongID = rd.GetInt32(0),
                            KhachHangID = rd.GetInt32(1),
                            PhongID = rd.GetInt32(2),
                            NgayDen = rd.GetDateTime(3),
                            NgayDiDuKien = rd.GetDateTime(4),
                            NgayDiThucTe = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                            TrangThai = rd.GetInt32(6),
                            TienCoc = rd.GetDecimal(7)
                        });
                    }
                }
            }
            return list;
        }

        // 0 = Đặt trước, 1 = Đang ở, 2 = Đã trả
        public int CreateBooking(Booking b)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                string query = @"INSERT INTO DATPHONG (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc)
                                 VALUES(@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, NULL, @TrangThai, @TienCoc);
                                 SELECT LAST_INSERT_ID();";
                MySqlCommand cmd = new MySqlCommand(query, conn, tx);
                cmd.Parameters.AddWithValue("@KhachHangID", b.KhachHangID);
                cmd.Parameters.AddWithValue("@PhongID", b.PhongID);
                cmd.Parameters.AddWithValue("@NgayDen", b.NgayDen);
                cmd.Parameters.AddWithValue("@NgayDiDuKien", b.NgayDiDuKien);
                cmd.Parameters.AddWithValue("@TrangThai", b.TrangThai);
                cmd.Parameters.AddWithValue("@TienCoc", b.TienCoc);

                var result = cmd.ExecuteScalar();
                int newId = Convert.ToInt32(result);

                // When booking created -> room status to 3
                string queryRoom = @"UPDATE PHONG SET TrangThai = 3 WHERE PhongID = @PhongID";
                MySqlCommand cmdRoom = new MySqlCommand(queryRoom, conn, tx);
                cmdRoom.Parameters.AddWithValue("@PhongID", b.PhongID);
                cmdRoom.ExecuteNonQuery();

                tx.Commit();
                return newId;
            }
        }

        public void UpdateStatus(int datPhongID, int status, DateTime? ngayDiThucTe)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                string query = @"UPDATE DATPHONG
                                 SET TrangThai = @TrangThai,
                                     NgayDiThucTe = @NgayDiThucTe
                                 WHERE DatPhongID = @DatPhongID";

                MySqlCommand cmd = new MySqlCommand(query, conn, tx);
                cmd.Parameters.AddWithValue("@TrangThai", status);
                if (ngayDiThucTe.HasValue)
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", ngayDiThucTe.Value);
                else
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", DBNull.Value);
                cmd.Parameters.AddWithValue("@DatPhongID", datPhongID);
                cmd.ExecuteNonQuery();

                string queryRoom = "";

                if (status == 0)
                    queryRoom = @"UPDATE PHONG SET TrangThai = 3 
                                  WHERE PhongID = (SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID)";
                else if (status == 1)
                    queryRoom = @"UPDATE PHONG SET TrangThai = 1 
                                  WHERE PhongID = (SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID)";
                else if (status == 2)
                    queryRoom = @"UPDATE PHONG SET TrangThai = 2 
                                  WHERE PhongID = (SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID)";

                if (!string.IsNullOrEmpty(queryRoom))
                {
                    MySqlCommand cmdRoom = new MySqlCommand(queryRoom, conn, tx);
                    cmdRoom.Parameters.AddWithValue("@DatPhongID", datPhongID);
                    cmdRoom.ExecuteNonQuery();
                }

                tx.Commit();
            }
        }

        public Booking GetById(int id)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc
                                 FROM DATPHONG
                                 WHERE DatPhongID = @Id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        return new Booking
                        {
                            DatPhongID = rd.GetInt32(0),
                            KhachHangID = rd.GetInt32(1),
                            PhongID = rd.GetInt32(2),
                            NgayDen = rd.GetDateTime(3),
                            NgayDiDuKien = rd.GetDateTime(4),
                            NgayDiThucTe = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                            TrangThai = rd.GetInt32(6),
                            TienCoc = rd.GetDecimal(7)
                        };
                    }
                }
            }
            return null;
        }

        public decimal GetDonGiaNgayByPhong(int phongID)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT lp.DonGiaNgay
                                 FROM PHONG p
                                 JOIN LOAIPHONG lp ON p.LoaiPhongID = lp.LoaiPhongID
                                 WHERE p.PhongID = @PhongID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhongID", phongID);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToDecimal(result);
            }
            return 0;
        }
    }
}
