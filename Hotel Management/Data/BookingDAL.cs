using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class BookingDAL
    {
        public List<Booking> GetAll()
        {
            var list = new List<Booking>();
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc 
                                 FROM DATPHONG";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader rd = cmd.ExecuteReader();
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
            return list;
        }

        // 0 = Đặt trước, 1 = Đang ở, 2 = Đã trả
        public int CreateBooking(Booking b)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"INSERT INTO DATPHONG (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc)
                                 VALUES(@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, NULL, @TrangThai, @TienCoc);
                                 SELECT SCOPE_IDENTITY();";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@KhachHangID", b.KhachHangID);
                cmd.Parameters.AddWithValue("@PhongID", b.PhongID);
                cmd.Parameters.AddWithValue("@NgayDen", b.NgayDen);
                cmd.Parameters.AddWithValue("@NgayDiDuKien", b.NgayDiDuKien);
                cmd.Parameters.AddWithValue("@TrangThai", b.TrangThai); // thường là 0
                cmd.Parameters.AddWithValue("@TienCoc", b.TienCoc);

                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                // Khi có booking mới (đặt trước) -> phòng chuyển sang "Đã có khách đặt" (3)
                string queryRoom = @"UPDATE PHONG SET TrangThai = 3 WHERE PhongID = @PhongID";
                SqlCommand cmdRoom = new SqlCommand(queryRoom, conn);
                cmdRoom.Parameters.AddWithValue("@PhongID", b.PhongID);
                cmdRoom.ExecuteNonQuery();

                return newId;
            }
        }

        public void UpdateStatus(int datPhongID, int status, DateTime? ngayDiThucTe)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"UPDATE DATPHONG
                                 SET TrangThai = @TrangThai,
                                     NgayDiThucTe = @NgayDiThucTe
                                 WHERE DatPhongID = @DatPhongID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TrangThai", status);
                if (ngayDiThucTe.HasValue)
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", ngayDiThucTe.Value);
                else
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", DBNull.Value);
                cmd.Parameters.AddWithValue("@DatPhongID", datPhongID);
                cmd.ExecuteNonQuery();

                // Cập nhật trạng thái PHONG tương ứng:
                // 0 = Đặt trước (Đã có khách đặt) -> 3
                // 1 = Đang ở -> 1 (Có khách)
                // 2 = Đã trả -> 2 (Chưa dọn)
                string queryRoom = "";

                if (status == 0)        // đặt trước
                    queryRoom = @"UPDATE PHONG SET TrangThai = 3 
                                  WHERE PhongID = (SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID)";
                else if (status == 1)   // đang ở
                    queryRoom = @"UPDATE PHONG SET TrangThai = 1 
                                  WHERE PhongID = (SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID)";
                else if (status == 2)   // đã trả
                    queryRoom = @"UPDATE PHONG SET TrangThai = 2 
                                  WHERE PhongID = (SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID)";

                if (!string.IsNullOrEmpty(queryRoom))
                {
                    SqlCommand cmdRoom = new SqlCommand(queryRoom, conn);
                    cmdRoom.Parameters.AddWithValue("@DatPhongID", datPhongID);
                    cmdRoom.ExecuteNonQuery();
                }
            }
        }

        public Booking GetById(int id)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc
                                 FROM DATPHONG
                                 WHERE DatPhongID = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader rd = cmd.ExecuteReader();
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
            return null;
        }

        public decimal GetDonGiaNgayByPhong(int phongID)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT lp.DonGiaNgay
                                 FROM PHONG p
                                 JOIN LOAIPHONG lp ON p.LoaiPhongID = lp.LoaiPhongID
                                 WHERE p.PhongID = @PhongID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhongID", phongID);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToDecimal(result);
            }
            return 0;
        }
    }
}
