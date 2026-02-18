using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class BookingDAL                                                         
    {
        public class BookingDateRangeInfo
        {
            public bool HasData { get; set; }
            public DateTime MinDate { get; set; }
            public DateTime MaxDate { get; set; }
        }

        public class BookingSummaryStats
        {
            public int HourlyGuests { get; set; }
            public int OvernightGuests { get; set; }
            public int StayingBookings { get; set; }
            public int CompletedBookings { get; set; }
            public decimal TotalRevenue { get; set; }
        }

        public class BookingDetailStats
        {
            public int DatPhongID { get; set; }
            public int PhongID { get; set; }
            public string MaPhong { get; set; }
            public DateTime CheckInTime { get; set; }
            public DateTime? CheckOutTime { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public int WaterBottleCount { get; set; }
            public int SoftDrinkCount { get; set; }
            public decimal TotalAmount { get; set; }
            public int TrangThai { get; set; }
            public bool IsHourly { get; set; }
        }

        public class BookingStatisticsData
        {
            public BookingSummaryStats Summary { get; set; }
            public List<BookingDetailStats> Bookings { get; set; }
        }

        private sealed class BookingStatisticsRow
        {
            public int DatPhongID { get; set; }
            public int PhongID { get; set; }
            public string MaPhong { get; set; }
            public DateTime NgayDiDuKien { get; set; }
            public DateTime? NgayDiThucTe { get; set; }
            public DateTime NgayDen { get; set; }
            public int TrangThai { get; set; }
            public string GhiChuPhong { get; set; }
            public decimal TongTien { get; set; }
        }

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

        public BookingDateRangeInfo GetBookingDateRange()
        {
            var info = new BookingDateRangeInfo
            {
                HasData = false,
                MinDate = DateTime.Today,
                MaxDate = DateTime.Today
            };

            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT MIN(NgayDen), MAX(NgayDen) FROM DATPHONG";
                using (var cmd = new MySqlCommand(query, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return info;
                    if (rd.IsDBNull(0) || rd.IsDBNull(1)) return info;

                    info.HasData = true;
                    info.MinDate = rd.GetDateTime(0).Date;
                    info.MaxDate = rd.GetDateTime(1).Date;
                }
            }

            return info;
        }

        public string GetBookingStatisticsFingerprint(DateTime fromDate, DateTime toDate)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);

            var sb = new StringBuilder(4096);
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT 
                                    b.DatPhongID,
                                    b.KhachHangID,
                                    b.PhongID,
                                    b.NgayDen,
                                    b.NgayDiDuKien,
                                    b.NgayDiThucTe,
                                    b.TrangThai,
                                    b.TienCoc,
                                    COALESCE(p.GhiChu, '') AS GhiChuPhong,
                                    COALESCE(inv.TotalAmount, 0) AS TongTien,
                                    COALESCE(inv.InvoiceCount, 0) AS SoHoaDon,
                                    inv.MaxInvoiceAt
                                 FROM DATPHONG b
                                 LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                 LEFT JOIN (
                                    SELECT DatPhongID,
                                           SUM(TongTien) AS TotalAmount,
                                           COUNT(*) AS InvoiceCount,
                                           MAX(NgayLap) AS MaxInvoiceAt
                                    FROM HOADON
                                    GROUP BY DatPhongID
                                 ) inv ON inv.DatPhongID = b.DatPhongID
                                 WHERE b.NgayDen >= @FromDate AND b.NgayDen < @ToDateExclusive
                                 ORDER BY b.DatPhongID";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            sb.Append(rd.IsDBNull(0) ? "0" : rd.GetInt32(0).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(1) ? "0" : rd.GetInt32(1).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(2) ? "0" : rd.GetInt32(2).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(3) ? "" : rd.GetDateTime(3).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(4) ? "" : rd.GetDateTime(4).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(5) ? "" : rd.GetDateTime(5).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(6) ? "0" : rd.GetInt32(6).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(7) ? "0" : rd.GetDecimal(7).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(8) ? "" : rd.GetString(8)).Append('|')
                              .Append(rd.IsDBNull(9) ? "0" : rd.GetDecimal(9).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(10) ? "0" : rd.GetInt32(10).ToString(CultureInfo.InvariantCulture)).Append('|')
                              .Append(rd.IsDBNull(11) ? "" : rd.GetDateTime(11).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture))
                              .Append('\n');
                        }
                    }
                }
            }

            if (sb.Length == 0) return "0";

            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public BookingStatisticsData GetBookingStatistics(DateTime fromDate, DateTime toDate)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);

            var rows = new List<BookingStatisticsRow>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT b.DatPhongID, b.PhongID, p.MaPhong, b.NgayDen, b.NgayDiDuKien, b.NgayDiThucTe, b.TrangThai,
                                        COALESCE(p.GhiChu, '') AS GhiChuPhong,
                                        COALESCE(inv.TotalAmount, 0) AS TongTien
                                 FROM DATPHONG b
                                 LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                 LEFT JOIN (
                                    SELECT DatPhongID, SUM(TongTien) AS TotalAmount
                                    FROM HOADON
                                    GROUP BY DatPhongID
                                 ) inv ON inv.DatPhongID = b.DatPhongID
                                 WHERE b.NgayDen >= @FromDate AND b.NgayDen < @ToDateExclusive";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            rows.Add(new BookingStatisticsRow
                            {
                                DatPhongID = rd.IsDBNull(0) ? 0 : rd.GetInt32(0),
                                PhongID = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                                MaPhong = rd.IsDBNull(2) ? null : rd.GetString(2),
                                NgayDen = rd.GetDateTime(3),
                                NgayDiDuKien = rd.GetDateTime(4),
                                NgayDiThucTe = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                                TrangThai = rd.IsDBNull(6) ? 0 : rd.GetInt32(6),
                                GhiChuPhong = rd.IsDBNull(7) ? string.Empty : rd.GetString(7),
                                TongTien = rd.IsDBNull(8) ? 0m : rd.GetDecimal(8)
                            });
                        }
                    }
                }
            }

            var details = rows
                .Select(x =>
                {
                    DateTime checkOutForClassify = x.NgayDiThucTe ?? x.NgayDiDuKien;
                    bool isHourly = IsHourlyBooking(x.NgayDen, checkOutForClassify);

                    DateTime durationEnd = x.NgayDiThucTe ?? (x.TrangThai == 2 ? x.NgayDiDuKien : DateTime.Now);
                    TimeSpan duration = CalculateDuration(x.NgayDen, durationEnd);

                    DateTime? displayCheckOut = null;
                    if (x.TrangThai == 2)
                        displayCheckOut = x.NgayDiThucTe ?? x.NgayDiDuKien;
                    else if (x.NgayDiThucTe.HasValue)
                        displayCheckOut = x.NgayDiThucTe.Value;

                    return new BookingDetailStats
                    {
                        DatPhongID = x.DatPhongID,
                        PhongID = x.PhongID,
                        MaPhong = string.IsNullOrWhiteSpace(x.MaPhong) ? ("#" + x.PhongID) : x.MaPhong,
                        CheckInTime = x.NgayDen,
                        CheckOutTime = displayCheckOut,
                        TotalDuration = duration,
                        WaterBottleCount = x.TrangThai == 1 ? GetIntTag(x.GhiChuPhong, "NS", 0) : 0,
                        SoftDrinkCount = x.TrangThai == 1 ? GetIntTag(x.GhiChuPhong, "NN", 0) : 0,
                        TotalAmount = x.TongTien,
                        TrangThai = x.TrangThai,
                        IsHourly = isHourly
                    };
                })
                .OrderByDescending(x => x.CheckInTime)
                .ThenByDescending(x => x.DatPhongID)
                .ToList();

            var summary = new BookingSummaryStats
            {
                HourlyGuests = details.Count(x => x.IsHourly),
                OvernightGuests = details.Count(x => !x.IsHourly),
                StayingBookings = details.Count(x => x.TrangThai == 1),
                CompletedBookings = details.Count(x => x.TrangThai == 2),
                TotalRevenue = details.Sum(x => x.TotalAmount)
            };

            return new BookingStatisticsData
            {
                Summary = summary,
                Bookings = details
            };
        }

        private static bool IsHourlyBooking(DateTime checkIn, DateTime checkOutEstimate)
        {
            DateTime end = checkOutEstimate < checkIn ? checkIn : checkOutEstimate;
            if (end.Date > checkIn.Date) return false;
            return (end - checkIn).TotalHours < 10d;
        }

        private static TimeSpan CalculateDuration(DateTime checkIn, DateTime checkOut)
        {
            DateTime end = checkOut < checkIn ? checkIn : checkOut;
            var span = end - checkIn;
            return span < TimeSpan.Zero ? TimeSpan.Zero : span;
        }

        private static int GetIntTag(string text, string key, int defaultVal)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultVal;
            Match m = Regex.Match(text, @"\b" + Regex.Escape(key) + @"\s*=\s*([^|]+)", RegexOptions.IgnoreCase);
            if (!m.Success) return defaultVal;
            if (int.TryParse(m.Groups[1].Value.Trim(), out int v)) return v;
            return defaultVal;
        }
    }
}
