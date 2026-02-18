using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class InvoiceDAL
    {
        public class RevenueSummaryStats
        {
            public int TotalInvoices { get; set; }
            public int PaidInvoices { get; set; }
            public int UnpaidInvoices { get; set; }
            public int UniqueRooms { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal PaidRevenue { get; set; }
            public decimal UnpaidRevenue { get; set; }
        }

        public class RevenueDailyStats
        {
            public DateTime Date { get; set; }
            public int InvoiceCount { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal PaidRevenue { get; set; }
            public decimal UnpaidRevenue { get; set; }
        }

        public class RevenueRoomStats
        {
            public int PhongID { get; set; }
            public string MaPhong { get; set; }
            public int InvoiceCount { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal PaidRevenue { get; set; }
        }

        public class RevenueInvoiceStats
        {
            public int HoaDonID { get; set; }
            public int DatPhongID { get; set; }
            public DateTime NgayLap { get; set; }
            public string MaPhong { get; set; }
            public string KhachHang { get; set; }
            public decimal TongTien { get; set; }
            public bool DaThanhToan { get; set; }
        }

        public class RevenueReportData
        {
            public RevenueSummaryStats Summary { get; set; }
            public List<RevenueDailyStats> Daily { get; set; }
            public List<RevenueRoomStats> ByRoom { get; set; }
            public List<RevenueInvoiceStats> Invoices { get; set; }
        }

        public class SampleSeedResult
        {
            public int AddedCustomers { get; set; }
            public int AddedBookings { get; set; }
            public int AddedInvoices { get; set; }
            public bool AlreadySeeded { get; set; }
        }

        private sealed class RevenueRow
        {
            public int HoaDonID { get; set; }
            public int DatPhongID { get; set; }
            public DateTime NgayLap { get; set; }
            public decimal TongTien { get; set; }
            public bool DaThanhToan { get; set; }
            public int PhongID { get; set; }
            public string MaPhong { get; set; }
            public string KhachHang { get; set; }
        }

        private sealed class RoomSeedItem
        {
            public int PhongID { get; set; }
            public int LoaiPhongID { get; set; }
            public string MaPhong { get; set; }
        }

        private sealed class CustomerSeedItem
        {
            public int KhachHangID { get; set; }
            public string HoTen { get; set; }
            public string CCCD { get; set; }
            public string DienThoai { get; set; }
            public string DiaChi { get; set; }
        }

        public void CreateInvoice(Invoice invoice)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"INSERT INTO HOADON (DatPhongID, NgayLap, TongTien, DaThanhToan)
                                 VALUES(@DatPhongID, @NgayLap, @TongTien, @DaThanhToan)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DatPhongID", invoice.DatPhongID);
                cmd.Parameters.AddWithValue("@NgayLap", invoice.NgayLap);
                cmd.Parameters.AddWithValue("@TongTien", invoice.TongTien);
                cmd.Parameters.AddWithValue("@DaThanhToan", invoice.DaThanhToan ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Invoice> GetInvoices(DateTime from, DateTime to)
        {
            var list = new List<Invoice>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT HoaDonID, DatPhongID, NgayLap, TongTien, DaThanhToan
                                 FROM HOADON
                                 WHERE NgayLap BETWEEN @From AND @To";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Invoice
                        {
                            HoaDonID = rd.GetInt32(0),
                            DatPhongID = rd.GetInt32(1),
                            NgayLap = rd.GetDateTime(2),
                            TongTien = rd.GetDecimal(3),
                            DaThanhToan = rd.GetBoolean(4)
                        });
                    }
                }
            }
            return list;
        }

        public RevenueReportData GetRevenueReport(DateTime fromDate, DateTime toDate)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);

            var rows = new List<RevenueRow>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT i.HoaDonID, i.DatPhongID, i.NgayLap, i.TongTien, i.DaThanhToan,
                                        b.PhongID, p.MaPhong, c.HoTen
                                 FROM HOADON i
                                 LEFT JOIN DATPHONG b ON i.DatPhongID = b.DatPhongID
                                 LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                 LEFT JOIN KHACHHANG c ON b.KhachHangID = c.KhachHangID
                                 WHERE i.NgayLap >= @FromDate AND i.NgayLap < @ToDateExclusive";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            rows.Add(new RevenueRow
                            {
                                HoaDonID = rd.GetInt32(0),
                                DatPhongID = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                                NgayLap = rd.GetDateTime(2),
                                TongTien = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3),
                                DaThanhToan = !rd.IsDBNull(4) && rd.GetBoolean(4),
                                PhongID = rd.IsDBNull(5) ? 0 : rd.GetInt32(5),
                                MaPhong = rd.IsDBNull(6) ? null : rd.GetString(6),
                                KhachHang = rd.IsDBNull(7) ? null : rd.GetString(7)
                            });
                        }
                    }
                }
            }

            var summary = new RevenueSummaryStats
            {
                TotalInvoices = rows.Count,
                PaidInvoices = rows.Count(x => x.DaThanhToan),
                UnpaidInvoices = rows.Count(x => !x.DaThanhToan),
                UniqueRooms = rows.Where(x => x.PhongID > 0).Select(x => x.PhongID).Distinct().Count(),
                TotalRevenue = rows.Sum(x => x.TongTien),
                PaidRevenue = rows.Where(x => x.DaThanhToan).Sum(x => x.TongTien),
                UnpaidRevenue = rows.Where(x => !x.DaThanhToan).Sum(x => x.TongTien)
            };

            var daily = rows
                .GroupBy(x => x.NgayLap.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueDailyStats
                {
                    Date = g.Key,
                    InvoiceCount = g.Count(),
                    TotalRevenue = g.Sum(x => x.TongTien),
                    PaidRevenue = g.Where(x => x.DaThanhToan).Sum(x => x.TongTien),
                    UnpaidRevenue = g.Where(x => !x.DaThanhToan).Sum(x => x.TongTien)
                })
                .ToList();

            var byRoom = rows
                .GroupBy(x => new { x.PhongID, x.MaPhong })
                .Select(g => new RevenueRoomStats
                {
                    PhongID = g.Key.PhongID,
                    MaPhong = string.IsNullOrWhiteSpace(g.Key.MaPhong) ? ("#" + g.Key.PhongID) : g.Key.MaPhong,
                    InvoiceCount = g.Count(),
                    TotalRevenue = g.Sum(x => x.TongTien),
                    PaidRevenue = g.Where(x => x.DaThanhToan).Sum(x => x.TongTien)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ThenBy(x => x.MaPhong)
                .ToList();

            var invoices = rows
                .OrderByDescending(x => x.NgayLap)
                .ThenByDescending(x => x.HoaDonID)
                .Select(x => new RevenueInvoiceStats
                {
                    HoaDonID = x.HoaDonID,
                    DatPhongID = x.DatPhongID,
                    NgayLap = x.NgayLap,
                    MaPhong = string.IsNullOrWhiteSpace(x.MaPhong) ? ("#" + x.PhongID) : x.MaPhong,
                    KhachHang = string.IsNullOrWhiteSpace(x.KhachHang) ? "(Khong ro)" : x.KhachHang,
                    TongTien = x.TongTien,
                    DaThanhToan = x.DaThanhToan
                })
                .ToList();

            return new RevenueReportData
            {
                Summary = summary,
                Daily = daily,
                ByRoom = byRoom,
                Invoices = invoices
            };
        }

        public SampleSeedResult SeedSampleReportData()
        {
            var result = new SampleSeedResult();

            using (MySqlConnection conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                var rooms = LoadRooms(conn, tx);
                if (rooms.Count == 0)
                {
                    tx.Commit();
                    return result;
                }

                var customers = EnsureSampleCustomers(conn, tx, result);
                if (customers.Count == 0)
                {
                    tx.Commit();
                    return result;
                }

                if (HasSampleBookings(conn, tx))
                {
                    result.AlreadySeeded = true;
                    tx.Commit();
                    return result;
                }

                InsertSampleBookingsAndInvoices(conn, tx, rooms, customers, result);
                tx.Commit();
            }

            return result;
        }

        private static List<RoomSeedItem> LoadRooms(MySqlConnection conn, MySqlTransaction tx)
        {
            var rooms = new List<RoomSeedItem>();
            string query = @"SELECT PhongID, LoaiPhongID, MaPhong
                             FROM PHONG
                             ORDER BY PhongID";
            using (var cmd = new MySqlCommand(query, conn, tx))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    rooms.Add(new RoomSeedItem
                    {
                        PhongID = rd.GetInt32(0),
                        LoaiPhongID = rd.IsDBNull(1) ? 1 : rd.GetInt32(1),
                        MaPhong = rd.IsDBNull(2) ? null : rd.GetString(2)
                    });
                }
            }
            return rooms;
        }

        private static List<CustomerSeedItem> EnsureSampleCustomers(MySqlConnection conn, MySqlTransaction tx, SampleSeedResult result)
        {
            var samples = new[]
            {
                new CustomerSeedItem { HoTen = "Nguyen Van An", CCCD = "990000000001", DienThoai = "0900000001", DiaChi = "Quan 1, TP.HCM" },
                new CustomerSeedItem { HoTen = "Tran Thi Bich", CCCD = "990000000002", DienThoai = "0900000002", DiaChi = "Quan 3, TP.HCM" },
                new CustomerSeedItem { HoTen = "Le Minh Chau", CCCD = "990000000003", DienThoai = "0900000003", DiaChi = "Hai Chau, Da Nang" },
                new CustomerSeedItem { HoTen = "Pham Quoc Dung", CCCD = "990000000004", DienThoai = "0900000004", DiaChi = "Ninh Kieu, Can Tho" },
                new CustomerSeedItem { HoTen = "Doan Thi Ha", CCCD = "990000000005", DienThoai = "0900000005", DiaChi = "Ba Dinh, Ha Noi" },
                new CustomerSeedItem { HoTen = "Vo Thanh Huy", CCCD = "990000000006", DienThoai = "0900000006", DiaChi = "Go Vap, TP.HCM" },
                new CustomerSeedItem { HoTen = "Bui Ngoc Kha", CCCD = "990000000007", DienThoai = "0900000007", DiaChi = "Bien Hoa, Dong Nai" },
                new CustomerSeedItem { HoTen = "Dang Gia Linh", CCCD = "990000000008", DienThoai = "0900000008", DiaChi = "Da Lat, Lam Dong" },
                new CustomerSeedItem { HoTen = "Hoang Anh Minh", CCCD = "990000000009", DienThoai = "0900000009", DiaChi = "Thu Duc, TP.HCM" },
                new CustomerSeedItem { HoTen = "Nguyen Thu Nhi", CCCD = "990000000010", DienThoai = "0900000010", DiaChi = "Quy Nhon, Binh Dinh" }
            };

            foreach (var sample in samples)
            {
                if (CustomerExists(conn, tx, sample.CCCD)) continue;

                string insert = @"INSERT INTO KHACHHANG (HoTen, CCCD, DienThoai, DiaChi)
                                  VALUES (@HoTen, @CCCD, @DienThoai, @DiaChi)";
                using (var cmd = new MySqlCommand(insert, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@HoTen", sample.HoTen);
                    cmd.Parameters.AddWithValue("@CCCD", sample.CCCD);
                    cmd.Parameters.AddWithValue("@DienThoai", sample.DienThoai);
                    cmd.Parameters.AddWithValue("@DiaChi", sample.DiaChi);
                    cmd.ExecuteNonQuery();
                }
                result.AddedCustomers++;
            }

            var customers = new List<CustomerSeedItem>();
            string select = @"SELECT KhachHangID, HoTen, CCCD, DienThoai, DiaChi
                              FROM KHACHHANG
                              WHERE CCCD LIKE '9900000000%'
                              ORDER BY KhachHangID";
            using (var cmd = new MySqlCommand(select, conn, tx))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    customers.Add(new CustomerSeedItem
                    {
                        KhachHangID = rd.GetInt32(0),
                        HoTen = rd.IsDBNull(1) ? null : rd.GetString(1),
                        CCCD = rd.IsDBNull(2) ? null : rd.GetString(2),
                        DienThoai = rd.IsDBNull(3) ? null : rd.GetString(3),
                        DiaChi = rd.IsDBNull(4) ? null : rd.GetString(4)
                    });
                }
            }

            return customers;
        }

        private static bool CustomerExists(MySqlConnection conn, MySqlTransaction tx, string cccd)
        {
            string query = "SELECT COUNT(1) FROM KHACHHANG WHERE CCCD = @CCCD";
            using (var cmd = new MySqlCommand(query, conn, tx))
            {
                cmd.Parameters.AddWithValue("@CCCD", cccd);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static bool HasSampleBookings(MySqlConnection conn, MySqlTransaction tx)
        {
            string query = @"SELECT COUNT(1)
                             FROM DATPHONG b
                             INNER JOIN KHACHHANG c ON b.KhachHangID = c.KhachHangID
                             WHERE c.CCCD LIKE '9900000000%'";
            using (var cmd = new MySqlCommand(query, conn, tx))
            {
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void InsertSampleBookingsAndInvoices(
            MySqlConnection conn,
            MySqlTransaction tx,
            List<RoomSeedItem> rooms,
            List<CustomerSeedItem> customers,
            SampleSeedResult result)
        {
            var random = new Random(20260218);
            var today = DateTime.Today;
            var startDate = today.AddDays(-20);
            var endDate = today.AddDays(2);

            int roomIndex = 0;
            int customerIndex = 0;

            for (DateTime day = startDate; day <= endDate; day = day.AddDays(1))
            {
                int bookingsPerDay = day.Date == today ? 3 : 2;
                for (int i = 0; i < bookingsPerDay; i++)
                {
                    var room = rooms[roomIndex % rooms.Count];
                    var customer = customers[customerIndex % customers.Count];
                    roomIndex++;
                    customerIndex++;

                    DateTime checkin = day.AddHours(11 + (i % 7));
                    DateTime checkoutPlan = checkin.AddDays(1);

                    int status;
                    DateTime? checkoutReal = null;
                    if (day.Date > today)
                    {
                        status = 0;
                    }
                    else if (day.Date >= today.AddDays(-1))
                    {
                        status = 1;
                    }
                    else
                    {
                        status = 2;
                        checkoutReal = checkoutPlan.AddHours(random.Next(0, 4));
                    }

                    decimal deposit = room.LoaiPhongID == 1 ? 200000m : 300000m;

                    int bookingId;
                    string insertBooking = @"INSERT INTO DATPHONG
                                             (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc)
                                             VALUES
                                             (@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, @NgayDiThucTe, @TrangThai, @TienCoc);
                                             SELECT LAST_INSERT_ID();";
                    using (var cmd = new MySqlCommand(insertBooking, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@KhachHangID", customer.KhachHangID);
                        cmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                        cmd.Parameters.AddWithValue("@NgayDen", checkin);
                        cmd.Parameters.AddWithValue("@NgayDiDuKien", checkoutPlan);
                        if (checkoutReal.HasValue)
                            cmd.Parameters.AddWithValue("@NgayDiThucTe", checkoutReal.Value);
                        else
                            cmd.Parameters.AddWithValue("@NgayDiThucTe", DBNull.Value);
                        cmd.Parameters.AddWithValue("@TrangThai", status);
                        cmd.Parameters.AddWithValue("@TienCoc", deposit);
                        bookingId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    result.AddedBookings++;

                    bool shouldCreateInvoice = status == 2 || (status == 1 && (i % 2 == 0));
                    if (!shouldCreateInvoice) continue;

                    int stayedHours = status == 2
                        ? Math.Max(1, (int)Math.Ceiling((checkoutReal.Value - checkin).TotalHours))
                        : Math.Max(1, (int)Math.Ceiling((DateTime.Now - checkin).TotalHours));

                    decimal firstHour = room.LoaiPhongID == 1 ? 70000m : 120000m;
                    decimal nextHour = room.LoaiPhongID == 1 ? 20000m : 30000m;
                    decimal roomCharge = stayedHours <= 1 ? firstHour : firstHour + (stayedHours - 1) * nextHour;
                    decimal drinkCharge = random.Next(0, 4) * 10000m;
                    decimal total = roomCharge + drinkCharge;
                    bool paid = status == 2;

                    DateTime invoiceDate = status == 2
                        ? checkoutReal.Value
                        : DateTime.Now.AddMinutes(-(i * 11 + roomIndex));
                    if (invoiceDate.Date < startDate) invoiceDate = startDate.AddHours(14);

                    string insertInvoice = @"INSERT INTO HOADON (DatPhongID, NgayLap, TongTien, DaThanhToan)
                                             VALUES (@DatPhongID, @NgayLap, @TongTien, @DaThanhToan)";
                    using (var cmd = new MySqlCommand(insertInvoice, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        cmd.Parameters.AddWithValue("@NgayLap", invoiceDate);
                        cmd.Parameters.AddWithValue("@TongTien", total);
                        cmd.Parameters.AddWithValue("@DaThanhToan", paid ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                    result.AddedInvoices++;
                }
            }
        }
    }
}
