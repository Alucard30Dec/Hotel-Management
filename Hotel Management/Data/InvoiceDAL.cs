using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class InvoiceDAL
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

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
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                string query = @"INSERT INTO HOADON
                                 (DatPhongID, NgayLap, TongTien, DaThanhToan,
                                  CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, PaymentStatus)
                                 VALUES(@DatPhongID, @NgayLap, @TongTien, @DaThanhToan,
                                        @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', @PaymentStatus);
                                 SELECT LAST_INSERT_ID();";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DatPhongID", invoice.DatPhongID);
                cmd.Parameters.AddWithValue("@NgayLap", invoice.NgayLap);
                cmd.Parameters.AddWithValue("@TongTien", invoice.TongTien);
                cmd.Parameters.AddWithValue("@DaThanhToan", invoice.DaThanhToan ? 1 : 0);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@PaymentStatus", invoice.DaThanhToan ? "paid" : "unpaid");
                int invoiceId = Convert.ToInt32(cmd.ExecuteScalar());

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "HOADON",
                    EntityId = invoiceId,
                    RelatedBookingId = invoice.DatPhongID,
                    RelatedInvoiceId = invoiceId,
                    ActionType = "CREATE",
                    Actor = actor,
                    Source = "InvoiceDAL.CreateInvoice",
                    AfterData = AuditLogDAL.SerializeState(new Dictionary<string, object>
                    {
                        { "HoaDonID", invoiceId },
                        { "DatPhongID", invoice.DatPhongID },
                        { "NgayLap", invoice.NgayLap },
                        { "TongTien", invoice.TongTien },
                        { "DaThanhToan", invoice.DaThanhToan ? 1 : 0 },
                        { "PaymentStatus", invoice.DaThanhToan ? "paid" : "unpaid" }
                    })
                });
            }
        }

        public List<Invoice> GetInvoices(DateTime from, DateTime to)
        {
            var list = new List<Invoice>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT HoaDonID, DatPhongID, NgayLap, TongTien, DaThanhToan
                                 FROM HOADON
                                 WHERE NgayLap BETWEEN @From AND @To
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";
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
            var summary = new RevenueSummaryStats();
            var daily = new List<RevenueDailyStats>();
            var byRoom = new List<RevenueRoomStats>();
            var invoices = new List<RevenueInvoiceStats>();

            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                const string baseFrom = @" FROM HOADON i
                                           LEFT JOIN DATPHONG b ON i.DatPhongID = b.DatPhongID
                                           LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                           LEFT JOIN KHACHHANG c ON b.KhachHangID = c.KhachHangID
                                           WHERE i.NgayLap >= @FromDate
                                             AND i.NgayLap < @ToDateExclusive
                                             AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                             AND (b.DataStatus IS NULL OR b.DataStatus <> 'deleted')";

                string summarySql = @"SELECT COUNT(*) AS TotalInvoices,
                                             COALESCE(SUM(CASE WHEN i.DaThanhToan = 1 THEN 1 ELSE 0 END), 0) AS PaidInvoices,
                                             COALESCE(SUM(CASE WHEN COALESCE(i.DaThanhToan, 0) <> 1 THEN 1 ELSE 0 END), 0) AS UnpaidInvoices,
                                             COUNT(DISTINCT CASE WHEN COALESCE(b.PhongID, 0) > 0 THEN b.PhongID END) AS UniqueRooms,
                                             COALESCE(SUM(i.TongTien), 0) AS TotalRevenue,
                                             COALESCE(SUM(CASE WHEN i.DaThanhToan = 1 THEN i.TongTien ELSE 0 END), 0) AS PaidRevenue,
                                             COALESCE(SUM(CASE WHEN COALESCE(i.DaThanhToan, 0) <> 1 THEN i.TongTien ELSE 0 END), 0) AS UnpaidRevenue"
                                  + baseFrom;
                using (var cmd = new MySqlCommand(summarySql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            summary.TotalInvoices = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                            summary.PaidInvoices = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1));
                            summary.UnpaidInvoices = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2));
                            summary.UniqueRooms = rd.IsDBNull(3) ? 0 : Convert.ToInt32(rd.GetValue(3));
                            summary.TotalRevenue = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4);
                            summary.PaidRevenue = rd.IsDBNull(5) ? 0m : rd.GetDecimal(5);
                            summary.UnpaidRevenue = rd.IsDBNull(6) ? 0m : rd.GetDecimal(6);
                        }
                    }
                }

                string dailySql = @"SELECT DATE(i.NgayLap) AS RevenueDate,
                                           COUNT(*) AS InvoiceCount,
                                           COALESCE(SUM(i.TongTien), 0) AS TotalRevenue,
                                           COALESCE(SUM(CASE WHEN i.DaThanhToan = 1 THEN i.TongTien ELSE 0 END), 0) AS PaidRevenue,
                                           COALESCE(SUM(CASE WHEN COALESCE(i.DaThanhToan, 0) <> 1 THEN i.TongTien ELSE 0 END), 0) AS UnpaidRevenue"
                                  + baseFrom + @"
                                   GROUP BY DATE(i.NgayLap)
                                   ORDER BY DATE(i.NgayLap)";
                using (var cmd = new MySqlCommand(dailySql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            daily.Add(new RevenueDailyStats
                            {
                                Date = rd.IsDBNull(0) ? from : rd.GetDateTime(0),
                                InvoiceCount = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1)),
                                TotalRevenue = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2),
                                PaidRevenue = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3),
                                UnpaidRevenue = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4)
                            });
                        }
                    }
                }

                string byRoomSql = @"SELECT COALESCE(b.PhongID, 0) AS PhongID,
                                            COALESCE(NULLIF(p.MaPhong, ''), CONCAT('#', COALESCE(b.PhongID, 0))) AS MaPhong,
                                            COUNT(*) AS InvoiceCount,
                                            COALESCE(SUM(i.TongTien), 0) AS TotalRevenue,
                                            COALESCE(SUM(CASE WHEN i.DaThanhToan = 1 THEN i.TongTien ELSE 0 END), 0) AS PaidRevenue"
                                   + baseFrom + @"
                                    GROUP BY COALESCE(b.PhongID, 0), COALESCE(NULLIF(p.MaPhong, ''), CONCAT('#', COALESCE(b.PhongID, 0)))
                                    ORDER BY TotalRevenue DESC, MaPhong ASC";
                using (var cmd = new MySqlCommand(byRoomSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            byRoom.Add(new RevenueRoomStats
                            {
                                PhongID = rd.IsDBNull(0) ? 0 : rd.GetInt32(0),
                                MaPhong = rd.IsDBNull(1) ? "#0" : rd.GetString(1),
                                InvoiceCount = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2)),
                                TotalRevenue = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3),
                                PaidRevenue = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4)
                            });
                        }
                    }
                }

                string invoiceSql = @"SELECT i.HoaDonID,
                                             COALESCE(i.DatPhongID, 0) AS DatPhongID,
                                             i.NgayLap,
                                             COALESCE(NULLIF(p.MaPhong, ''), CONCAT('#', COALESCE(b.PhongID, 0))) AS MaPhong,
                                             COALESCE(NULLIF(c.HoTen, ''), '(Khong ro)') AS KhachHang,
                                             i.TongTien,
                                             COALESCE(i.DaThanhToan, 0) AS DaThanhToan"
                                    + baseFrom + @"
                                     ORDER BY i.NgayLap DESC, i.HoaDonID DESC";
                using (var cmd = new MySqlCommand(invoiceSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            invoices.Add(new RevenueInvoiceStats
                            {
                                HoaDonID = rd.GetInt32(0),
                                DatPhongID = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                                NgayLap = rd.GetDateTime(2),
                                MaPhong = rd.IsDBNull(3) ? "#0" : rd.GetString(3),
                                KhachHang = rd.IsDBNull(4) ? "(Khong ro)" : rd.GetString(4),
                                TongTien = rd.IsDBNull(5) ? 0m : rd.GetDecimal(5),
                                DaThanhToan = !rd.IsDBNull(6) && rd.GetBoolean(6)
                            });
                        }
                    }
                }
            }

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
                }
                else
                {
                    InsertSampleBookingsAndInvoices(conn, tx, rooms, customers, result);
                }
                EnsureRoomMapScenarioCases(conn, tx, rooms, customers, result);
                tx.Commit();
            }

            return result;
        }

        private static List<RoomSeedItem> LoadRooms(MySqlConnection conn, MySqlTransaction tx)
        {
            var rooms = new List<RoomSeedItem>();
            string query = @"SELECT PhongID, LoaiPhongID, MaPhong
                             FROM PHONG
                             WHERE COALESCE(DataStatus, 'active') <> 'deleted'
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
                new CustomerSeedItem { HoTen = "Khách mẫu 01", CCCD = "990000000001", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 02", CCCD = "990000000002", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 03", CCCD = "990000000003", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 04", CCCD = "990000000004", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 05", CCCD = "990000000005", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 06", CCCD = "990000000006", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 07", CCCD = "990000000007", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 08", CCCD = "990000000008", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 09", CCCD = "990000000009", DienThoai = "", DiaChi = "" },
                new CustomerSeedItem { HoTen = "Khách mẫu 10", CCCD = "990000000010", DienThoai = "", DiaChi = "" }
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
                                AND COALESCE(DataStatus, 'active') <> 'deleted'
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
                             WHERE c.CCCD LIKE '9900000000%'
                               AND COALESCE(b.DataStatus, 'active') <> 'deleted'";
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
            var activeRoomIds = new HashSet<int>();

            int roomIndex = 0;
            int customerIndex = 0;
            DateTime now = DateTime.Now;

            for (DateTime day = startDate; day <= endDate; day = day.AddDays(1))
            {
                int bookingsPerDay = day.Date == today ? 6 : 2;
                for (int i = 0; i < bookingsPerDay; i++)
                {
                    var room = rooms[roomIndex % rooms.Count];
                    var customer = customers[customerIndex % customers.Count];
                    roomIndex++;
                    customerIndex++;

                    bool isTodayScenario = day.Date == today;
                    bool isHourly;
                    int scenarioNightCount = 1;
                    DateTime checkin;
                    DateTime checkoutPlan;
                    if (isTodayScenario)
                    {
                        switch (i)
                        {
                            case 0: // Phòng giờ
                                isHourly = true;
                                checkin = now.AddHours(-2);
                                checkoutPlan = checkin.AddHours(4);
                                break;
                            case 1: // Phòng giờ (thời gian dài để test)
                                isHourly = true;
                                checkin = now.AddHours(-10);
                                checkoutPlan = checkin.AddHours(12);
                                break;
                            case 2: // Phòng đêm (không phụ thu)
                                isHourly = false;
                                checkin = EnsureNightWindowStart(now.AddHours(-3));
                                checkoutPlan = checkin.AddDays(1);
                                break;
                            case 3: // Phòng ngày có phụ thu trả trễ
                                isHourly = false;
                                checkin = EnsureDayWindowStart(now.AddDays(-2).AddHours(10));
                                checkoutPlan = checkin.AddDays(1);
                                break;
                            case 4: // Phòng đêm có phụ thu trả trễ
                                isHourly = false;
                                checkin = EnsureNightWindowStart(now.AddDays(-2).AddHours(22));
                                checkoutPlan = checkin.AddDays(1);
                                break;
                            default: // Nhiều đêm (đêm đầu) => hiển thị phòng ngày
                                isHourly = false;
                                scenarioNightCount = 2;
                                checkin = EnsureNightWindowStart(now.AddDays(-1).AddHours(22));
                                checkoutPlan = checkin.AddDays(2);
                                break;
                        }
                    }
                    else
                    {
                        isHourly = i % 3 == 0;
                        checkin = isHourly
                            ? day.AddHours(7 + ((i * 3) % 12))
                            : day.AddHours(i % 2 == 0 ? 21 : 10);
                        checkoutPlan = isHourly
                            ? checkin.AddHours(3 + (i % 4))
                            : checkin.AddDays(1);
                    }

                    int status;
                    DateTime? checkoutReal = null;
                    if (day.Date > today)
                    {
                        status = 0;
                    }
                    else if (day.Date == today)
                    {
                        status = 1;
                    }
                    else
                    {
                        status = 2;
                        checkoutReal = isHourly
                            ? checkoutPlan.AddMinutes(random.Next(10, 90))
                            : checkoutPlan.AddHours(random.Next(-2, 3));
                        if (checkoutReal <= checkin)
                            checkoutReal = isHourly ? checkin.AddHours(2) : checkin.AddHours(12);
                    }

                    decimal deposit = isHourly
                        ? (room.LoaiPhongID == 1 ? 100000m : 150000m)
                        : (room.LoaiPhongID == 1 ? 200000m : 300000m);
                    int bookingType = isHourly ? 1 : 2;

                    int bookingId;
                    string insertBooking = @"INSERT INTO DATPHONG
                                             (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, BookingType, TienCoc,
                                              CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, KenhDat)
                                             VALUES
                                             (@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, @NgayDiThucTe, @TrangThai, @BookingType, @TienCoc,
                                              @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', @KenhDat);
                                             SELECT LAST_INSERT_ID();";
                    using (var cmd = new MySqlCommand(insertBooking, conn, tx))
                    {
                        DateTime nowUtc = DateTime.UtcNow;
                        cmd.Parameters.AddWithValue("@KhachHangID", customer.KhachHangID);
                        cmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                        cmd.Parameters.AddWithValue("@NgayDen", checkin);
                        cmd.Parameters.AddWithValue("@NgayDiDuKien", checkoutPlan);
                        if (checkoutReal.HasValue)
                            cmd.Parameters.AddWithValue("@NgayDiThucTe", checkoutReal.Value);
                        else
                            cmd.Parameters.AddWithValue("@NgayDiThucTe", DBNull.Value);
                        cmd.Parameters.AddWithValue("@TrangThai", status);
                        cmd.Parameters.AddWithValue("@BookingType", bookingType);
                        cmd.Parameters.AddWithValue("@TienCoc", deposit);
                        cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                        cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        cmd.Parameters.AddWithValue("@CreatedBy", "seed-system");
                        cmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                        cmd.Parameters.AddWithValue("@KenhDat", "TrucTiep");
                        bookingId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    result.AddedBookings++;

                    string guestName = string.IsNullOrWhiteSpace(customer.HoTen)
                        ? ("Khách " + customerIndex)
                        : customer.HoTen.Trim();

                    if (bookingType == 2)
                    {
                        decimal nightRate = room.LoaiPhongID == 2 ? 300000m : 200000m;
                        int nightCount = Math.Max(1, scenarioNightCount);
                        UpsertSampleStayInfo(conn, tx, bookingId, nightRate, nightCount, guestName);
                    }

                    int softQty = status == 0 ? 0 : random.Next(0, 3);
                    int waterQty = status == 0 ? 0 : random.Next(0, 3);
                    UpsertSampleExtra(conn, tx, bookingId, "NN", "Nước ngọt", softQty, 20000m);
                    UpsertSampleExtra(conn, tx, bookingId, "NS", "Nước suối", waterQty, 10000m);

                    if (status == 1 && activeRoomIds.Add(room.PhongID))
                    {
                        UpsertSampleRoomState(
                            conn,
                            tx,
                            room.PhongID,
                            checkin,
                            bookingType == 1 ? 3 : 1,
                            guestName);
                    }

                    bool shouldCreateInvoice = status == 2 || status == 1;
                    if (!shouldCreateInvoice) continue;

                    decimal roomCharge;
                    if (bookingType == 1)
                    {
                        int stayedHours = status == 2
                            ? Math.Max(1, (int)Math.Ceiling((checkoutReal.Value - checkin).TotalHours))
                            : Math.Max(1, (int)Math.Ceiling((DateTime.Now - checkin).TotalHours));
                        decimal firstHour = 60000m;
                        decimal nextHour = 20000m;
                        roomCharge = stayedHours <= 1 ? firstHour : firstHour + (stayedHours - 1) * nextHour;
                    }
                    else
                    {
                        bool firstSegmentIsNight = IsNightWindow(checkin);
                        decimal nightRate = room.LoaiPhongID == 2 ? 300000m : 200000m;
                        decimal dayRate = room.LoaiPhongID == 2 ? 350000m : 250000m;
                        roomCharge = firstSegmentIsNight ? nightRate : dayRate;
                    }

                    decimal drinkCharge = softQty * 20000m + waterQty * 10000m;
                    decimal total = roomCharge + drinkCharge;
                    bool paid = status == 2 ? (i % 3 != 0) : (i == 0 || i == 3 || i == 4);
                    if (status == 1 && paid)
                        total = Math.Max(50000m, Math.Round(total * 0.45m, MidpointRounding.AwayFromZero));

                    DateTime invoiceDate = status == 2
                        ? checkoutReal.Value
                        : DateTime.Now.AddMinutes(-(i * 11 + roomIndex));
                    if (invoiceDate.Date < startDate) invoiceDate = startDate.AddHours(14);

                    string insertInvoice = @"INSERT INTO HOADON (DatPhongID, NgayLap, TongTien, DaThanhToan)
                                             VALUES (@DatPhongID, @NgayLap, @TongTien, @DaThanhToan)";
                    using (var cmd = new MySqlCommand(insertInvoice, conn, tx))
                    {
                        DateTime nowUtc = DateTime.UtcNow;
                        cmd.CommandText = @"INSERT INTO HOADON
                                            (DatPhongID, NgayLap, TongTien, DaThanhToan,
                                             CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, PaymentStatus)
                                            VALUES
                                            (@DatPhongID, @NgayLap, @TongTien, @DaThanhToan,
                                             @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', @PaymentStatus)";
                        cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        cmd.Parameters.AddWithValue("@NgayLap", invoiceDate);
                        cmd.Parameters.AddWithValue("@TongTien", total);
                        cmd.Parameters.AddWithValue("@DaThanhToan", paid ? 1 : 0);
                        cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                        cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        cmd.Parameters.AddWithValue("@CreatedBy", "seed-system");
                        cmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                        cmd.Parameters.AddWithValue("@PaymentStatus", paid ? "paid" : "unpaid");
                        cmd.ExecuteNonQuery();
                    }
                    result.AddedInvoices++;
                }
            }
        }

        private static void EnsureRoomMapScenarioCases(
            MySqlConnection conn,
            MySqlTransaction tx,
            List<RoomSeedItem> rooms,
            List<CustomerSeedItem> customers,
            SampleSeedResult result)
        {
            if (rooms == null || customers == null || rooms.Count == 0 || customers.Count == 0) return;

            string closeOldSeedSql = @"UPDATE DATPHONG
                                       SET TrangThai = 2,
                                           NgayDiThucTe = NOW(),
                                           UpdatedAtUtc = UTC_TIMESTAMP(),
                                           UpdatedBy = 'seed-system'
                                       WHERE KenhDat = 'SeedRoomMapCase'
                                         AND TrangThai = 1
                                         AND COALESCE(DataStatus, 'active') <> 'deleted'";
            using (var closeCmd = new MySqlCommand(closeOldSeedSql, conn, tx))
            {
                closeCmd.ExecuteNonQuery();
            }

            var scenarioRooms = rooms.OrderBy(r => r.PhongID).Take(Math.Min(6, rooms.Count)).ToList();
            DateTime now = DateTime.Now;
            for (int i = 0; i < scenarioRooms.Count; i++)
            {
                var room = scenarioRooms[i];
                var customer = customers[i % customers.Count];
                string guestName = string.IsNullOrWhiteSpace(customer.HoTen)
                    ? ("Khách test " + (i + 1))
                    : customer.HoTen.Trim();

                bool isHourly;
                DateTime checkin;
                DateTime checkoutPlan;
                int nightCount = 1;
                switch (i)
                {
                    case 0:
                        isHourly = true;
                        checkin = now.AddHours(-2);
                        checkoutPlan = checkin.AddHours(4);
                        break;
                    case 1:
                        isHourly = true;
                        checkin = now.AddHours(-10);
                        checkoutPlan = checkin.AddHours(12);
                        break;
                    case 2:
                        isHourly = false;
                        checkin = EnsureNightWindowStart(now.AddHours(-3));
                        checkoutPlan = checkin.AddDays(1);
                        break;
                    case 3:
                        isHourly = false;
                        checkin = EnsureDayWindowStart(now.AddDays(-2).AddHours(10));
                        checkoutPlan = checkin.AddDays(1);
                        break;
                    case 4:
                        isHourly = false;
                        checkin = EnsureNightWindowStart(now.AddDays(-2).AddHours(22));
                        checkoutPlan = checkin.AddDays(1);
                        break;
                    default:
                        isHourly = false;
                        nightCount = 2;
                        checkin = EnsureNightWindowStart(now.AddDays(-1).AddHours(22));
                        checkoutPlan = checkin.AddDays(2);
                        break;
                }

                int bookingType = isHourly ? 1 : 2;
                decimal deposit = isHourly
                    ? (room.LoaiPhongID == 1 ? 100000m : 150000m)
                    : (room.LoaiPhongID == 1 ? 200000m : 300000m);

                int bookingId;
                string insertBookingSql = @"INSERT INTO DATPHONG
                                            (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, BookingType, TienCoc,
                                             CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, KenhDat)
                                            VALUES
                                            (@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, NULL, 1, @BookingType, @TienCoc,
                                             @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', 'SeedRoomMapCase');
                                            SELECT LAST_INSERT_ID();";
                using (var cmd = new MySqlCommand(insertBookingSql, conn, tx))
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    cmd.Parameters.AddWithValue("@KhachHangID", customer.KhachHangID);
                    cmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    cmd.Parameters.AddWithValue("@NgayDen", checkin);
                    cmd.Parameters.AddWithValue("@NgayDiDuKien", checkoutPlan);
                    cmd.Parameters.AddWithValue("@BookingType", bookingType);
                    cmd.Parameters.AddWithValue("@TienCoc", deposit);
                    cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@CreatedBy", "seed-system");
                    cmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                    bookingId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                result.AddedBookings++;

                if (!isHourly)
                {
                    decimal nightRate = room.LoaiPhongID == 2 ? 300000m : 200000m;
                    UpsertSampleStayInfo(conn, tx, bookingId, nightRate, Math.Max(1, nightCount), guestName);
                }

                int softQty = i % 3;
                int waterQty = (i + 1) % 3;
                UpsertSampleExtra(conn, tx, bookingId, "NN", "Nước ngọt", softQty, 20000m);
                UpsertSampleExtra(conn, tx, bookingId, "NS", "Nước suối", waterQty, 10000m);
                UpsertSampleRoomState(conn, tx, room.PhongID, checkin, isHourly ? 3 : 1, guestName);

                decimal roomCharge;
                if (isHourly)
                {
                    int stayedHours = Math.Max(1, (int)Math.Ceiling((now - checkin).TotalHours));
                    roomCharge = stayedHours <= 1 ? 60000m : 60000m + (stayedHours - 1) * 20000m;
                }
                else
                {
                    bool firstSegmentIsNight = IsNightWindow(checkin);
                    decimal nightRate = room.LoaiPhongID == 2 ? 300000m : 200000m;
                    decimal dayRate = room.LoaiPhongID == 2 ? 350000m : 250000m;
                    roomCharge = firstSegmentIsNight ? nightRate : dayRate;
                    if (nightCount > 1)
                        roomCharge = nightRate + (nightCount - 1) * dayRate;
                }

                decimal drinkCharge = softQty * 20000m + waterQty * 10000m;
                decimal total = roomCharge + drinkCharge;
                bool hasPartialPaid = (i % 2 == 0);
                if (hasPartialPaid)
                {
                    decimal paidAmount = Math.Max(50000m, Math.Round(total * 0.4m, MidpointRounding.AwayFromZero));
                    string invoiceSql = @"INSERT INTO HOADON
                                          (DatPhongID, NgayLap, TongTien, DaThanhToan,
                                           CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, PaymentStatus)
                                          VALUES
                                          (@DatPhongID, @NgayLap, @TongTien, 1,
                                           @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', 'paid')";
                    using (var invoiceCmd = new MySqlCommand(invoiceSql, conn, tx))
                    {
                        DateTime nowUtc = DateTime.UtcNow;
                        invoiceCmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        invoiceCmd.Parameters.AddWithValue("@NgayLap", now);
                        invoiceCmd.Parameters.AddWithValue("@TongTien", paidAmount);
                        invoiceCmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                        invoiceCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        invoiceCmd.Parameters.AddWithValue("@CreatedBy", "seed-system");
                        invoiceCmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                        invoiceCmd.ExecuteNonQuery();
                    }
                    result.AddedInvoices++;
                }
            }
        }

        private static bool IsNightWindow(DateTime checkin)
        {
            TimeSpan at = checkin.TimeOfDay;
            return at >= TimeSpan.FromHours(20) || at < TimeSpan.FromHours(12);
        }

        private static DateTime EnsureNightWindowStart(DateTime value)
        {
            if (IsNightWindow(value)) return value;
            return value.Date.AddHours(22);
        }

        private static DateTime EnsureDayWindowStart(DateTime value)
        {
            if (!IsNightWindow(value)) return value;
            return value.Date.AddHours(14);
        }

        private static void UpsertSampleStayInfo(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            decimal nightlyRate,
            int nightCount,
            string guestName)
        {
            string sql = @"INSERT INTO STAY_INFO
                           (DatPhongID, LyDoLuuTru, GiaPhong, SoDemLuuTru, GuestListJson,
                            LaDiaBanCu, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                           VALUES
                           (@DatPhongID, @LyDoLuuTru, @GiaPhong, @SoDemLuuTru, @GuestListJson,
                            0, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                           ON DUPLICATE KEY UPDATE
                           LyDoLuuTru = VALUES(LyDoLuuTru),
                           GiaPhong = VALUES(GiaPhong),
                           SoDemLuuTru = VALUES(SoDemLuuTru),
                           GuestListJson = VALUES(GuestListJson),
                           UpdatedAtUtc = VALUES(UpdatedAtUtc),
                           UpdatedBy = VALUES(UpdatedBy)";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                DateTime nowUtc = DateTime.UtcNow;
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                cmd.Parameters.AddWithValue("@LyDoLuuTru", "Dữ liệu mẫu");
                cmd.Parameters.AddWithValue("@GiaPhong", nightlyRate);
                cmd.Parameters.AddWithValue("@SoDemLuuTru", Math.Max(1, nightCount));
                cmd.Parameters.AddWithValue("@GuestListJson", string.IsNullOrWhiteSpace(guestName) ? (object)DBNull.Value : guestName.Trim());
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", "seed-system");
                cmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpsertSampleExtra(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            string itemCode,
            string itemName,
            int qty,
            decimal unitPrice)
        {
            int safeQty = Math.Max(0, qty);
            decimal safeUnitPrice = Math.Max(0m, unitPrice);
            decimal amount = safeQty * safeUnitPrice;

            string sql = @"INSERT INTO BOOKING_EXTRAS
                           (DatPhongID, ItemCode, ItemName, Qty, UnitPrice, Amount,
                            CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                           VALUES
                           (@DatPhongID, @ItemCode, @ItemName, @Qty, @UnitPrice, @Amount,
                            @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                           ON DUPLICATE KEY UPDATE
                           ItemName = VALUES(ItemName),
                           Qty = VALUES(Qty),
                           UnitPrice = VALUES(UnitPrice),
                           Amount = VALUES(Amount),
                           UpdatedAtUtc = VALUES(UpdatedAtUtc),
                           UpdatedBy = VALUES(UpdatedBy)";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                DateTime nowUtc = DateTime.UtcNow;
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                cmd.Parameters.AddWithValue("@ItemName", itemName);
                cmd.Parameters.AddWithValue("@Qty", safeQty);
                cmd.Parameters.AddWithValue("@UnitPrice", safeUnitPrice);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", "seed-system");
                cmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpsertSampleRoomState(
            MySqlConnection conn,
            MySqlTransaction tx,
            int roomId,
            DateTime checkin,
            int rentalType,
            string guestName)
        {
            string sql = @"UPDATE PHONG
                           SET TrangThai = 1,
                               KieuThue = @KieuThue,
                               ThoiGianBatDau = @ThoiGianBatDau,
                               TenKhachHienThi = @TenKhachHienThi,
                               UpdatedAtUtc = @UpdatedAtUtc,
                               UpdatedBy = @UpdatedBy,
                               DataStatus = 'active'
                           WHERE PhongID = @PhongID";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                DateTime nowUtc = DateTime.UtcNow;
                cmd.Parameters.AddWithValue("@KieuThue", rentalType);
                cmd.Parameters.AddWithValue("@ThoiGianBatDau", checkin);
                cmd.Parameters.AddWithValue("@TenKhachHienThi", string.IsNullOrWhiteSpace(guestName) ? (object)DBNull.Value : guestName.Trim());
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedBy", "seed-system");
                cmd.Parameters.AddWithValue("@PhongID", roomId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
