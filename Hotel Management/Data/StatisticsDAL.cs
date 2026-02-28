using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace HotelManagement.Data
{
    public class StatisticsDAL
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

        public class RevenuePoint
        {
            public DateTime Date { get; set; }
            public decimal Revenue { get; set; }
        }

        public class DistributionPoint
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public decimal Revenue { get; set; }
        }

        public class HourDistributionPoint
        {
            public int Hour { get; set; }
            public int Count { get; set; }
        }

        public class KpiDashboardData
        {
            public int TotalBookings { get; set; }
            public decimal TotalRevenue { get; set; }
            public int HourlyBookings { get; set; }
            public int OvernightBookings { get; set; }
            public decimal ExtrasRevenue { get; set; }
            public int CancelCount { get; set; }
            public int NoShowCount { get; set; }
            public List<RevenuePoint> RevenueByDay { get; set; }
            public List<DistributionPoint> TopChannels { get; set; }
            public List<DistributionPoint> TopRoomTypes { get; set; }
            public List<HourDistributionPoint> CheckInByHour { get; set; }
            public List<HourDistributionPoint> CheckOutByHour { get; set; }
        }

        public class ExplorerQuery
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string Keyword { get; set; }
            public int? BookingStatus { get; set; }
            public int? BookingType { get; set; }
            public bool? IsFullyPaid { get; set; }
            public int? RoomTypeId { get; set; }
            public string Channel { get; set; }
            public string SortBy { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        public class ExplorerRow
        {
            public int DatPhongID { get; set; }
            public int KhachHangID { get; set; }
            public int PhongID { get; set; }
            public string MaPhong { get; set; }
            public int LoaiPhongID { get; set; }
            public string LoaiPhong { get; set; }
            public string KhachHang { get; set; }
            public string CCCD { get; set; }
            public string DienThoai { get; set; }
            public DateTime NgayDen { get; set; }
            public DateTime NgayDiDuKien { get; set; }
            public DateTime? NgayDiThucTe { get; set; }
            public int TrangThai { get; set; }
            public string TrangThaiText { get; set; }
            public int BookingType { get; set; }
            public string BookingTypeText { get; set; }
            public string KenhDat { get; set; }
            public decimal TienCoc { get; set; }
            public decimal TongHoaDon { get; set; }
            public decimal ExtrasRevenue { get; set; }
            public int SoHoaDon { get; set; }
            public int SoHoaDonDaThanhToan { get; set; }
            public bool DaThanhToanDayDu { get; set; }
            public DateTime? HoaDonGanNhat { get; set; }
            public DateTime? CreatedAtUtc { get; set; }
            public DateTime? UpdatedAtUtc { get; set; }
            public string CreatedBy { get; set; }
            public string UpdatedBy { get; set; }
        }

        public class ExplorerResult
        {
            public int TotalCount { get; set; }
            public List<ExplorerRow> Rows { get; set; }
        }

        public class ExplorerInvoiceLine
        {
            public int HoaDonID { get; set; }
            public DateTime NgayLap { get; set; }
            public decimal TongTien { get; set; }
            public bool DaThanhToan { get; set; }
            public string PaymentStatus { get; set; }
            public DateTime? UpdatedAtUtc { get; set; }
            public string UpdatedBy { get; set; }
        }

        public class ExplorerStayLine
        {
            public string Field { get; set; }
            public string Value { get; set; }
        }

        public class ExplorerExtraLine
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }
            public string Note { get; set; }
        }

        public class ExplorerDocumentData
        {
            public ExplorerRow Booking { get; set; }
            public List<ExplorerStayLine> StayInfo { get; set; }
            public List<ExplorerExtraLine> Extras { get; set; }
            public List<ExplorerInvoiceLine> Invoices { get; set; }
            public List<AuditLogDAL.AuditLogEntry> Timeline { get; set; }
        }

        public class DataQualityAlert
        {
            public string Severity { get; set; }
            public string Code { get; set; }
            public string Message { get; set; }
            public string Reference { get; set; }
            public DateTime? EventTime { get; set; }
        }

        public KpiDashboardData GetKpiDashboard(DateTime fromDate, DateTime toDate, int? bookingType = null)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);
            if (toExclusive <= from) toExclusive = from.AddDays(1);

            var data = new KpiDashboardData
            {
                RevenueByDay = new List<RevenuePoint>(),
                TopChannels = new List<DistributionPoint>(),
                TopRoomTypes = new List<DistributionPoint>(),
                CheckInByHour = new List<HourDistributionPoint>(),
                CheckOutByHour = new List<HourDistributionPoint>()
            };
            using (var conn = DbHelper.GetConnection())
            {
                FillKpiSummary(conn, from, toExclusive, bookingType, data);
                data.RevenueByDay = GetRevenueByDay(conn, from, toExclusive, bookingType);
                data.TopChannels = GetKpiTopChannels(conn, from, toExclusive, bookingType);
                data.TopRoomTypes = GetKpiTopRoomTypes(conn, from, toExclusive, bookingType);
                data.CheckInByHour = GetKpiCheckInByHour(conn, from, toExclusive, bookingType);
                data.CheckOutByHour = GetKpiCheckOutByHour(conn, from, toExclusive, bookingType);
            }

            return data;
        }

        public ExplorerResult GetExplorerRows(ExplorerQuery query)
        {
            DateTime from = query.FromDate.Date;
            DateTime toExclusive = query.ToDate.Date.AddDays(1);
            int page = query.Page < 1 ? 1 : query.Page;
            int pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
            int offset = (page - 1) * pageSize;

            var where = new StringBuilder(@" WHERE b.NgayDen >= @FromDate
                                            AND b.NgayDen < @ToDateExclusive
                                            AND COALESCE(b.DataStatus, 'active') <> 'deleted'");
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@FromDate", from),
                new MySqlParameter("@ToDateExclusive", toExclusive)
            };

            if (query.BookingStatus.HasValue)
            {
                where.Append(" AND b.TrangThai = @BookingStatus");
                parameters.Add(new MySqlParameter("@BookingStatus", query.BookingStatus.Value));
            }

            if (query.BookingType.HasValue)
            {
                where.Append(" AND b.BookingType = @BookingType");
                parameters.Add(new MySqlParameter("@BookingType", query.BookingType.Value));
            }

            if (query.RoomTypeId.HasValue)
            {
                where.Append(" AND p.LoaiPhongID = @RoomTypeId");
                parameters.Add(new MySqlParameter("@RoomTypeId", query.RoomTypeId.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Channel) && !string.Equals(query.Channel, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND COALESCE(NULLIF(b.KenhDat, ''), 'TrucTiep') = @KenhDat");
                parameters.Add(new MySqlParameter("@KenhDat", query.Channel.Trim()));
            }

            if (query.IsFullyPaid.HasValue)
            {
                if (query.IsFullyPaid.Value)
                {
                    where.Append(@" AND EXISTS (
                                        SELECT 1
                                        FROM HOADON i
                                        WHERE i.DatPhongID = b.DatPhongID
                                          AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                    )
                                   AND NOT EXISTS (
                                        SELECT 1
                                        FROM HOADON i
                                        WHERE i.DatPhongID = b.DatPhongID
                                          AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                          AND COALESCE(i.DaThanhToan, 0) <> 1
                                    )");
                }
                else
                {
                    where.Append(@" AND (
                                        NOT EXISTS (
                                            SELECT 1
                                            FROM HOADON i
                                            WHERE i.DatPhongID = b.DatPhongID
                                              AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                        )
                                        OR EXISTS (
                                            SELECT 1
                                            FROM HOADON i
                                            WHERE i.DatPhongID = b.DatPhongID
                                              AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                              AND COALESCE(i.DaThanhToan, 0) <> 1
                                        )
                                   )");
                }
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                where.Append(@" AND (
                                CAST(b.DatPhongID AS CHAR) LIKE @Keyword
                                OR COALESCE(p.MaPhong, '') LIKE @Keyword
                                OR COALESCE(c.HoTen, '') LIKE @Keyword
                                OR COALESCE(c.CCCD, '') LIKE @Keyword
                                OR COALESCE(c.DienThoai, '') LIKE @Keyword
                                OR COALESCE(b.CreatedBy, '') LIKE @Keyword
                                OR COALESCE(b.UpdatedBy, '') LIKE @Keyword
                                OR COALESCE(b.KenhDat, '') LIKE @Keyword
                               )");
                parameters.Add(new MySqlParameter("@Keyword", "%" + query.Keyword.Trim() + "%"));
            }

            string orderBy = BuildExplorerOrderBy(query.SortBy);
            string fromClause = @"
                FROM DATPHONG b
                LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                LEFT JOIN KHACHHANG c ON b.KhachHangID = c.KhachHangID";

            var result = new ExplorerResult
            {
                TotalCount = 0,
                Rows = new List<ExplorerRow>()
            };

            using (var conn = DbHelper.GetConnection())
            {
                string countSql = "SELECT COUNT(1)" + fromClause + where;
                using (var countCmd = new MySqlCommand(countSql, conn))
                {
                    ApplyParameters(countCmd, parameters);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                string dataSql = @"SELECT b.DatPhongID, b.KhachHangID, b.PhongID,
                                          p.MaPhong, p.LoaiPhongID,
                                          c.HoTen, c.CCCD, c.DienThoai,
                                          b.NgayDen, b.NgayDiDuKien, b.NgayDiThucTe,
                                          b.TrangThai, b.BookingType, b.TienCoc,
                                          COALESCE(NULLIF(b.KenhDat, ''), 'TrucTiep') AS KenhDat,
                                          (
                                              SELECT COALESCE(SUM(i.TongTien), 0)
                                              FROM HOADON i
                                              WHERE i.DatPhongID = b.DatPhongID
                                                AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                          ) AS TongHoaDon,
                                          (
                                              SELECT COALESCE(SUM(e.Amount), 0)
                                              FROM BOOKING_EXTRAS e
                                              WHERE e.DatPhongID = b.DatPhongID
                                          ) AS ExtrasRevenue,
                                          (
                                              SELECT COUNT(*)
                                              FROM HOADON i
                                              WHERE i.DatPhongID = b.DatPhongID
                                                AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                          ) AS SoHoaDon,
                                          (
                                              SELECT COALESCE(SUM(CASE WHEN i.DaThanhToan = 1 THEN 1 ELSE 0 END), 0)
                                              FROM HOADON i
                                              WHERE i.DatPhongID = b.DatPhongID
                                                AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                          ) AS SoHoaDonDaThanhToan,
                                          (
                                              SELECT MAX(i.NgayLap)
                                              FROM HOADON i
                                              WHERE i.DatPhongID = b.DatPhongID
                                                AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                          ) AS LastInvoiceDate,
                                          b.CreatedAtUtc, b.UpdatedAtUtc, b.CreatedBy, b.UpdatedBy
                                   " + fromClause + where + @"
                                   ORDER BY " + orderBy + @"
                                   LIMIT @Limit OFFSET @Offset";

                using (var cmd = new MySqlCommand(dataSql, conn))
                {
                    ApplyParameters(cmd, parameters);
                    cmd.Parameters.AddWithValue("@Limit", pageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int status = rd.IsDBNull(11) ? 0 : rd.GetInt32(11);
                            int type = rd.IsDBNull(12) ? 2 : rd.GetInt32(12);
                            int invoiceCount = rd.IsDBNull(17) ? 0 : rd.GetInt32(17);
                            int paidCount = rd.IsDBNull(18) ? 0 : rd.GetInt32(18);

                            result.Rows.Add(new ExplorerRow
                            {
                                DatPhongID = rd.GetInt32(0),
                                KhachHangID = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                                PhongID = rd.IsDBNull(2) ? 0 : rd.GetInt32(2),
                                MaPhong = rd.IsDBNull(3) ? string.Empty : rd.GetString(3),
                                LoaiPhongID = rd.IsDBNull(4) ? 0 : rd.GetInt32(4),
                                LoaiPhong = FormatRoomType(rd.IsDBNull(4) ? 0 : rd.GetInt32(4)),
                                KhachHang = rd.IsDBNull(5) ? string.Empty : rd.GetString(5),
                                CCCD = rd.IsDBNull(6) ? string.Empty : rd.GetString(6),
                                DienThoai = rd.IsDBNull(7) ? string.Empty : rd.GetString(7),
                                NgayDen = rd.GetDateTime(8),
                                NgayDiDuKien = rd.GetDateTime(9),
                                NgayDiThucTe = rd.IsDBNull(10) ? (DateTime?)null : rd.GetDateTime(10),
                                TrangThai = status,
                                TrangThaiText = FormatBookingStatus(status),
                                BookingType = type,
                                BookingTypeText = FormatBookingType(type),
                                TienCoc = rd.IsDBNull(13) ? 0m : rd.GetDecimal(13),
                                KenhDat = rd.IsDBNull(14) ? "TrucTiep" : rd.GetString(14),
                                TongHoaDon = rd.IsDBNull(15) ? 0m : rd.GetDecimal(15),
                                ExtrasRevenue = rd.IsDBNull(16) ? 0m : rd.GetDecimal(16),
                                SoHoaDon = invoiceCount,
                                SoHoaDonDaThanhToan = paidCount,
                                DaThanhToanDayDu = invoiceCount > 0 && paidCount == invoiceCount,
                                HoaDonGanNhat = rd.IsDBNull(19) ? (DateTime?)null : rd.GetDateTime(19),
                                CreatedAtUtc = rd.IsDBNull(20) ? (DateTime?)null : rd.GetDateTime(20),
                                UpdatedAtUtc = rd.IsDBNull(21) ? (DateTime?)null : rd.GetDateTime(21),
                                CreatedBy = rd.IsDBNull(22) ? string.Empty : rd.GetString(22),
                                UpdatedBy = rd.IsDBNull(23) ? string.Empty : rd.GetString(23)
                            });
                        }
                    }
                }
            }

            return result;
        }

        public ExplorerDocumentData GetBookingDocumentData(int bookingId, int maxTimelineItems)
        {
            ExplorerRow booking = null;
            var invoices = new List<ExplorerInvoiceLine>();
            var stayInfo = new List<ExplorerStayLine>();
            var extras = new List<ExplorerExtraLine>();

            using (var conn = DbHelper.GetConnection())
            {
                string bookingSql = @"SELECT b.DatPhongID, b.KhachHangID, b.PhongID,
                                             p.MaPhong, p.LoaiPhongID,
                                             c.HoTen, c.CCCD, c.DienThoai,
                                             b.NgayDen, b.NgayDiDuKien, b.NgayDiThucTe,
                                             b.TrangThai, b.BookingType, b.TienCoc,
                                             COALESCE(NULLIF(b.KenhDat, ''), 'TrucTiep') AS KenhDat,
                                             (
                                                 SELECT COALESCE(SUM(i.TongTien), 0)
                                                 FROM HOADON i
                                                 WHERE i.DatPhongID = b.DatPhongID
                                                   AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                             ) AS TongHoaDon,
                                             (
                                                 SELECT COALESCE(SUM(e.Amount), 0)
                                                 FROM BOOKING_EXTRAS e
                                                 WHERE e.DatPhongID = b.DatPhongID
                                             ) AS ExtrasRevenue,
                                             (
                                                 SELECT COUNT(*)
                                                 FROM HOADON i
                                                 WHERE i.DatPhongID = b.DatPhongID
                                                   AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                             ) AS SoHoaDon,
                                             (
                                                 SELECT COALESCE(SUM(CASE WHEN i.DaThanhToan = 1 THEN 1 ELSE 0 END), 0)
                                                 FROM HOADON i
                                                 WHERE i.DatPhongID = b.DatPhongID
                                                   AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                             ) AS SoHoaDonDaThanhToan,
                                             (
                                                 SELECT MAX(i.NgayLap)
                                                 FROM HOADON i
                                                 WHERE i.DatPhongID = b.DatPhongID
                                                   AND (i.DataStatus IS NULL OR i.DataStatus <> 'deleted')
                                             ) AS LastInvoiceDate,
                                             b.CreatedAtUtc, b.UpdatedAtUtc, b.CreatedBy, b.UpdatedBy
                                      FROM DATPHONG b
                                      LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                      LEFT JOIN KHACHHANG c ON b.KhachHangID = c.KhachHangID
                                      WHERE b.DatPhongID = @DatPhongID
                                        AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                      LIMIT 1";

                using (var cmd = new MySqlCommand(bookingSql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            int status = rd.IsDBNull(11) ? 0 : rd.GetInt32(11);
                            int type = rd.IsDBNull(12) ? 2 : rd.GetInt32(12);
                            int invoiceCount = rd.IsDBNull(17) ? 0 : rd.GetInt32(17);
                            int paidCount = rd.IsDBNull(18) ? 0 : rd.GetInt32(18);

                            booking = new ExplorerRow
                            {
                                DatPhongID = rd.GetInt32(0),
                                KhachHangID = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                                PhongID = rd.IsDBNull(2) ? 0 : rd.GetInt32(2),
                                MaPhong = rd.IsDBNull(3) ? string.Empty : rd.GetString(3),
                                LoaiPhongID = rd.IsDBNull(4) ? 0 : rd.GetInt32(4),
                                LoaiPhong = FormatRoomType(rd.IsDBNull(4) ? 0 : rd.GetInt32(4)),
                                KhachHang = rd.IsDBNull(5) ? string.Empty : rd.GetString(5),
                                CCCD = rd.IsDBNull(6) ? string.Empty : rd.GetString(6),
                                DienThoai = rd.IsDBNull(7) ? string.Empty : rd.GetString(7),
                                NgayDen = rd.GetDateTime(8),
                                NgayDiDuKien = rd.GetDateTime(9),
                                NgayDiThucTe = rd.IsDBNull(10) ? (DateTime?)null : rd.GetDateTime(10),
                                TrangThai = status,
                                TrangThaiText = FormatBookingStatus(status),
                                BookingType = type,
                                BookingTypeText = FormatBookingType(type),
                                TienCoc = rd.IsDBNull(13) ? 0m : rd.GetDecimal(13),
                                KenhDat = rd.IsDBNull(14) ? "TrucTiep" : rd.GetString(14),
                                TongHoaDon = rd.IsDBNull(15) ? 0m : rd.GetDecimal(15),
                                ExtrasRevenue = rd.IsDBNull(16) ? 0m : rd.GetDecimal(16),
                                SoHoaDon = invoiceCount,
                                SoHoaDonDaThanhToan = paidCount,
                                DaThanhToanDayDu = invoiceCount > 0 && paidCount == invoiceCount,
                                HoaDonGanNhat = rd.IsDBNull(19) ? (DateTime?)null : rd.GetDateTime(19),
                                CreatedAtUtc = rd.IsDBNull(20) ? (DateTime?)null : rd.GetDateTime(20),
                                UpdatedAtUtc = rd.IsDBNull(21) ? (DateTime?)null : rd.GetDateTime(21),
                                CreatedBy = rd.IsDBNull(22) ? string.Empty : rd.GetString(22),
                                UpdatedBy = rd.IsDBNull(23) ? string.Empty : rd.GetString(23)
                            };
                        }
                    }
                }

                if (booking != null)
                {
                    string invoiceSql = @"SELECT HoaDonID, NgayLap, TongTien, DaThanhToan,
                                                 COALESCE(NULLIF(PaymentStatus, ''), IF(DaThanhToan = 1, 'paid', 'unpaid')) AS PaymentStatus,
                                                 UpdatedAtUtc, UpdatedBy
                                          FROM HOADON
                                          WHERE DatPhongID = @DatPhongID
                                            AND COALESCE(DataStatus, 'active') <> 'deleted'
                                          ORDER BY NgayLap DESC, HoaDonID DESC";
                    using (var cmd = new MySqlCommand(invoiceSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                invoices.Add(new ExplorerInvoiceLine
                                {
                                    HoaDonID = rd.GetInt32(0),
                                    NgayLap = rd.GetDateTime(1),
                                    TongTien = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2),
                                    DaThanhToan = !rd.IsDBNull(3) && rd.GetBoolean(3),
                                    PaymentStatus = rd.IsDBNull(4) ? string.Empty : rd.GetString(4),
                                    UpdatedAtUtc = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                                    UpdatedBy = rd.IsDBNull(6) ? string.Empty : rd.GetString(6)
                                });
                            }
                        }
                    }

                    string staySql = @"SELECT LyDoLuuTru, GioiTinh, NgaySinh, LoaiGiayTo, SoGiayTo, QuocTich,
                                              NoiCuTru, LaDiaBanCu, MaTinhMoi, MaXaMoi, MaTinhCu, MaHuyenCu, MaXaCu,
                                              DiaChiChiTiet, GiaPhong, SoDemLuuTru, GuestListJson
                                       FROM STAY_INFO
                                       WHERE DatPhongID = @DatPhongID
                                       LIMIT 1";
                    using (var cmd = new MySqlCommand(staySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                stayInfo.Add(new ExplorerStayLine { Field = "Lý do lưu trú", Value = rd.IsDBNull(0) ? string.Empty : rd.GetString(0) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Giới tính", Value = rd.IsDBNull(1) ? string.Empty : rd.GetString(1) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Ngày sinh", Value = rd.IsDBNull(2) ? string.Empty : rd.GetDateTime(2).ToString("dd/MM/yyyy") });
                                stayInfo.Add(new ExplorerStayLine { Field = "Loại giấy tờ", Value = rd.IsDBNull(3) ? string.Empty : rd.GetString(3) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Số giấy tờ", Value = rd.IsDBNull(4) ? string.Empty : rd.GetString(4) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Quốc tịch", Value = rd.IsDBNull(5) ? string.Empty : rd.GetString(5) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Nơi cư trú", Value = rd.IsDBNull(6) ? string.Empty : rd.GetString(6) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Loại địa bàn", Value = rd.IsDBNull(7) ? string.Empty : (rd.GetBoolean(7) ? "Địa bàn cũ" : "Địa bàn mới") });
                                stayInfo.Add(new ExplorerStayLine { Field = "Mã tỉnh mới", Value = rd.IsDBNull(8) ? string.Empty : rd.GetString(8) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Mã xã mới", Value = rd.IsDBNull(9) ? string.Empty : rd.GetString(9) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Mã tỉnh cũ", Value = rd.IsDBNull(10) ? string.Empty : rd.GetString(10) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Mã huyện cũ", Value = rd.IsDBNull(11) ? string.Empty : rd.GetString(11) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Mã xã cũ", Value = rd.IsDBNull(12) ? string.Empty : rd.GetString(12) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Địa chỉ chi tiết", Value = rd.IsDBNull(13) ? string.Empty : rd.GetString(13) });
                                stayInfo.Add(new ExplorerStayLine { Field = "Giá phòng", Value = rd.IsDBNull(14) ? "0" : rd.GetDecimal(14).ToString("N0") + " đ" });
                                stayInfo.Add(new ExplorerStayLine { Field = "Số đêm", Value = rd.IsDBNull(15) ? "1" : rd.GetInt32(15).ToString(CultureInfo.InvariantCulture) });
                                AppendGuestListStayInfo(stayInfo, rd.IsDBNull(16) ? string.Empty : rd.GetString(16));
                            }
                        }
                    }

                    string extrasSql = @"SELECT ItemCode, ItemName, Qty, UnitPrice, Amount, Note
                                       FROM BOOKING_EXTRAS
                                       WHERE DatPhongID = @DatPhongID
                                       ORDER BY ItemCode";
                    using (var cmd = new MySqlCommand(extrasSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                extras.Add(new ExplorerExtraLine
                                {
                                    ItemCode = rd.IsDBNull(0) ? string.Empty : rd.GetString(0),
                                    ItemName = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                                    Qty = rd.IsDBNull(2) ? 0 : rd.GetInt32(2),
                                    UnitPrice = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3),
                                    Amount = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4),
                                    Note = rd.IsDBNull(5) ? string.Empty : rd.GetString(5)
                                });
                            }
                        }
                    }
                }
            }

            var timeline = booking == null
                ? new List<AuditLogDAL.AuditLogEntry>()
                : _auditLogDal.GetTimelineByBooking(bookingId, booking.PhongID, maxTimelineItems);

            return new ExplorerDocumentData
            {
                Booking = booking,
                StayInfo = stayInfo,
                Extras = extras,
                Invoices = invoices,
                Timeline = timeline
            };
        }

        public List<DataQualityAlert> GetDataQualityAlerts(DateTime fromDate, DateTime toDate, int maxItems)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);
            int safeMax = maxItems <= 0 ? 200 : Math.Min(maxItems, 2000);
            var alerts = new List<DataQualityAlert>();

            using (var conn = DbHelper.GetConnection())
            {
                string missingInvoiceSql = @"SELECT b.DatPhongID, b.NgayDiThucTe, p.MaPhong
                                            FROM DATPHONG b
                                            LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                            LEFT JOIN HOADON i ON i.DatPhongID = b.DatPhongID
                                                AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                            WHERE b.NgayDen >= @FromDate
                                              AND b.NgayDen < @ToDateExclusive
                                              AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                              AND b.TrangThai = 2
                                            GROUP BY b.DatPhongID, b.NgayDiThucTe, p.MaPhong
                                            HAVING COUNT(i.HoaDonID) = 0
                                            LIMIT 200";
                using (var cmd = new MySqlCommand(missingInvoiceSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int bookingId = rd.GetInt32(0);
                            alerts.Add(new DataQualityAlert
                            {
                                Severity = "High",
                                Code = "BOOKING_NO_INVOICE",
                                Message = "Dat phong da tra phong nhung chua co hoa don.",
                                Reference = "DatPhongID=" + bookingId + ", Phong=" + (rd.IsDBNull(2) ? "(khong ro)" : rd.GetString(2)),
                                EventTime = rd.IsDBNull(1) ? (DateTime?)null : rd.GetDateTime(1)
                            });
                        }
                    }
                }

                string orphanInvoiceSql = @"SELECT i.HoaDonID, i.DatPhongID, i.NgayLap
                                            FROM HOADON i
                                            LEFT JOIN DATPHONG b ON i.DatPhongID = b.DatPhongID
                                            WHERE i.NgayLap >= @FromDate
                                              AND i.NgayLap < @ToDateExclusive
                                              AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                              AND (b.DatPhongID IS NULL OR COALESCE(b.DataStatus, 'active') = 'deleted')
                                            ORDER BY i.NgayLap DESC
                                            LIMIT 200";
                using (var cmd = new MySqlCommand(orphanInvoiceSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            alerts.Add(new DataQualityAlert
                            {
                                Severity = "High",
                                Code = "ORPHAN_INVOICE",
                                Message = "Hoa don khong gan voi dat phong hop le.",
                                Reference = "HoaDonID=" + rd.GetInt32(0) + ", DatPhongID=" + (rd.IsDBNull(1) ? "null" : rd.GetInt32(1).ToString(CultureInfo.InvariantCulture)),
                                EventTime = rd.IsDBNull(2) ? (DateTime?)null : rd.GetDateTime(2)
                            });
                        }
                    }
                }

                string roomWithoutBookingSql = @"SELECT p.PhongID, p.MaPhong, p.UpdatedAtUtc
                                                 FROM PHONG p
                                                 LEFT JOIN DATPHONG b ON b.PhongID = p.PhongID
                                                    AND b.TrangThai = 1
                                                    AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                                 WHERE p.TrangThai = 1
                                                   AND COALESCE(p.DataStatus, 'active') <> 'deleted'
                                                 GROUP BY p.PhongID, p.MaPhong, p.UpdatedAtUtc
                                                 HAVING COUNT(b.DatPhongID) = 0
                                                 LIMIT 200";
                using (var cmd = new MySqlCommand(roomWithoutBookingSql, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        alerts.Add(new DataQualityAlert
                        {
                            Severity = "Medium",
                            Code = "ROOM_WITHOUT_ACTIVE_BOOKING",
                            Message = "Phong o trang thai dang o nhung khong co dat phong dang o.",
                            Reference = "PhongID=" + rd.GetInt32(0) + ", MaPhong=" + (rd.IsDBNull(1) ? string.Empty : rd.GetString(1)),
                            EventTime = rd.IsDBNull(2) ? (DateTime?)null : rd.GetDateTime(2)
                        });
                    }
                }

                string missingCheckoutSql = @"SELECT DatPhongID, NgayDiDuKien
                                              FROM DATPHONG
                                              WHERE NgayDen >= @FromDate
                                                AND NgayDen < @ToDateExclusive
                                                AND COALESCE(DataStatus, 'active') <> 'deleted'
                                                AND TrangThai = 2
                                                AND NgayDiThucTe IS NULL
                                              ORDER BY NgayDiDuKien DESC
                                              LIMIT 200";
                using (var cmd = new MySqlCommand(missingCheckoutSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            alerts.Add(new DataQualityAlert
                            {
                                Severity = "Medium",
                                Code = "MISSING_ACTUAL_CHECKOUT",
                                Message = "Dat phong da tra nhung thieu NgayDiThucTe.",
                                Reference = "DatPhongID=" + rd.GetInt32(0),
                                EventTime = rd.IsDBNull(1) ? (DateTime?)null : rd.GetDateTime(1)
                            });
                        }
                    }
                }
            }

            return alerts
                .OrderByDescending(x => x.EventTime ?? DateTime.MinValue)
                .Take(safeMax)
                .ToList();
        }

        private static void FillKpiSummary(MySqlConnection conn, DateTime from, DateTime toExclusive, int? bookingType, KpiDashboardData target)
        {
            var sql = new StringBuilder(@"SELECT
                                                COUNT(*) AS TotalBookings,
                                                COALESCE(SUM(CASE WHEN COALESCE(b.BookingType, 2) = 1 THEN 1 ELSE 0 END), 0) AS HourlyBookings,
                                                COALESCE(SUM(CASE WHEN COALESCE(b.BookingType, 2) = 2 THEN 1 ELSE 0 END), 0) AS OvernightBookings,
                                                COALESCE(SUM(CASE WHEN COALESCE(b.TrangThai, 0) = 3 THEN 1 ELSE 0 END), 0) AS CancelCount,
                                                COALESCE(SUM(CASE WHEN COALESCE(b.TrangThai, 0) = 4 THEN 1 ELSE 0 END), 0) AS NoShowCount,
                                                COALESCE(SUM((
                                                    SELECT COALESCE(SUM(i.TongTien), 0)
                                                    FROM HOADON i
                                                    WHERE i.DatPhongID = b.DatPhongID
                                                      AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                                )), 0) AS TotalRevenue,
                                                COALESCE(SUM((
                                                    SELECT COALESCE(SUM(e.Amount), 0)
                                                    FROM BOOKING_EXTRAS e
                                                    WHERE e.DatPhongID = b.DatPhongID
                                                )), 0) AS ExtrasRevenue
                                         FROM DATPHONG b
                                         WHERE b.NgayDen >= @FromDate
                                           AND b.NgayDen < @ToDateExclusive
                                           AND COALESCE(b.DataStatus, 'active') <> 'deleted'");
            if (bookingType.HasValue)
                sql.Append(" AND b.BookingType = @BookingType");

            using (var cmd = new MySqlCommand(sql.ToString(), conn))
            {
                AddKpiBaseParameters(cmd, from, toExclusive, bookingType);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return;
                    target.TotalBookings = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture);
                    target.HourlyBookings = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture);
                    target.OvernightBookings = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2), CultureInfo.InvariantCulture);
                    target.CancelCount = rd.IsDBNull(3) ? 0 : Convert.ToInt32(rd.GetValue(3), CultureInfo.InvariantCulture);
                    target.NoShowCount = rd.IsDBNull(4) ? 0 : Convert.ToInt32(rd.GetValue(4), CultureInfo.InvariantCulture);
                    target.TotalRevenue = rd.IsDBNull(5) ? 0m : rd.GetDecimal(5);
                    target.ExtrasRevenue = rd.IsDBNull(6) ? 0m : rd.GetDecimal(6);
                }
            }
        }

        private static List<DistributionPoint> GetKpiTopChannels(MySqlConnection conn, DateTime from, DateTime toExclusive, int? bookingType)
        {
            var list = new List<DistributionPoint>();
            var sql = new StringBuilder(@"SELECT
                                                COALESCE(NULLIF(b.KenhDat, ''), 'TrucTiep') AS KenhDat,
                                                COUNT(*) AS BookingCount,
                                                COALESCE(SUM((
                                                    SELECT COALESCE(SUM(i.TongTien), 0)
                                                    FROM HOADON i
                                                    WHERE i.DatPhongID = b.DatPhongID
                                                      AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                                )), 0) AS Revenue
                                         FROM DATPHONG b
                                         WHERE b.NgayDen >= @FromDate
                                           AND b.NgayDen < @ToDateExclusive
                                           AND COALESCE(b.DataStatus, 'active') <> 'deleted'");
            if (bookingType.HasValue)
                sql.Append(" AND b.BookingType = @BookingType");
            sql.Append(" GROUP BY COALESCE(NULLIF(b.KenhDat, ''), 'TrucTiep') ORDER BY BookingCount DESC, Revenue DESC LIMIT 10");

            using (var cmd = new MySqlCommand(sql.ToString(), conn))
            {
                AddKpiBaseParameters(cmd, from, toExclusive, bookingType);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DistributionPoint
                        {
                            Name = rd.IsDBNull(0) ? "TrucTiep" : rd.GetString(0),
                            Count = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture),
                            Revenue = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2)
                        });
                    }
                }
            }

            return list;
        }

        private static List<DistributionPoint> GetKpiTopRoomTypes(MySqlConnection conn, DateTime from, DateTime toExclusive, int? bookingType)
        {
            var list = new List<DistributionPoint>();
            var sql = new StringBuilder(@"SELECT
                                                COALESCE(p.LoaiPhongID, 0) AS LoaiPhongID,
                                                COUNT(*) AS BookingCount,
                                                COALESCE(SUM((
                                                    SELECT COALESCE(SUM(i.TongTien), 0)
                                                    FROM HOADON i
                                                    WHERE i.DatPhongID = b.DatPhongID
                                                      AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                                )), 0) AS Revenue
                                         FROM DATPHONG b
                                         LEFT JOIN PHONG p ON p.PhongID = b.PhongID
                                         WHERE b.NgayDen >= @FromDate
                                           AND b.NgayDen < @ToDateExclusive
                                           AND COALESCE(b.DataStatus, 'active') <> 'deleted'");
            if (bookingType.HasValue)
                sql.Append(" AND b.BookingType = @BookingType");
            sql.Append(" GROUP BY COALESCE(p.LoaiPhongID, 0) ORDER BY Revenue DESC, BookingCount DESC LIMIT 10");

            using (var cmd = new MySqlCommand(sql.ToString(), conn))
            {
                AddKpiBaseParameters(cmd, from, toExclusive, bookingType);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int roomTypeId = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture);
                        list.Add(new DistributionPoint
                        {
                            Name = FormatRoomType(roomTypeId),
                            Count = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture),
                            Revenue = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2)
                        });
                    }
                }
            }

            return list;
        }

        private static List<HourDistributionPoint> GetKpiCheckInByHour(MySqlConnection conn, DateTime from, DateTime toExclusive, int? bookingType)
        {
            var list = new List<HourDistributionPoint>();
            var sql = new StringBuilder(@"SELECT HOUR(b.NgayDen) AS CheckInHour, COUNT(*) AS HitCount
                                         FROM DATPHONG b
                                         WHERE b.NgayDen >= @FromDate
                                           AND b.NgayDen < @ToDateExclusive
                                           AND COALESCE(b.DataStatus, 'active') <> 'deleted'");
            if (bookingType.HasValue)
                sql.Append(" AND b.BookingType = @BookingType");
            sql.Append(" GROUP BY HOUR(b.NgayDen) ORDER BY CheckInHour");

            using (var cmd = new MySqlCommand(sql.ToString(), conn))
            {
                AddKpiBaseParameters(cmd, from, toExclusive, bookingType);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new HourDistributionPoint
                        {
                            Hour = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture),
                            Count = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture)
                        });
                    }
                }
            }

            return list;
        }

        private static List<HourDistributionPoint> GetKpiCheckOutByHour(MySqlConnection conn, DateTime from, DateTime toExclusive, int? bookingType)
        {
            var list = new List<HourDistributionPoint>();
            var sql = new StringBuilder(@"SELECT HOUR(COALESCE(b.NgayDiThucTe, b.NgayDiDuKien)) AS CheckOutHour, COUNT(*) AS HitCount
                                         FROM DATPHONG b
                                         WHERE b.NgayDen >= @FromDate
                                           AND b.NgayDen < @ToDateExclusive
                                           AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                           AND (b.NgayDiThucTe IS NOT NULL OR b.TrangThai = 2)");
            if (bookingType.HasValue)
                sql.Append(" AND b.BookingType = @BookingType");
            sql.Append(" GROUP BY HOUR(COALESCE(b.NgayDiThucTe, b.NgayDiDuKien)) ORDER BY CheckOutHour");

            using (var cmd = new MySqlCommand(sql.ToString(), conn))
            {
                AddKpiBaseParameters(cmd, from, toExclusive, bookingType);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new HourDistributionPoint
                        {
                            Hour = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture),
                            Count = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture)
                        });
                    }
                }
            }

            return list;
        }

        private static void AddKpiBaseParameters(MySqlCommand cmd, DateTime from, DateTime toExclusive, int? bookingType)
        {
            cmd.Parameters.AddWithValue("@FromDate", from);
            cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
            if (bookingType.HasValue)
                cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);
        }

        private static List<RevenuePoint> GetRevenueByDay(MySqlConnection conn, DateTime from, DateTime toExclusive, int? bookingType)
        {
            var list = new List<RevenuePoint>();
            var sql = new StringBuilder(@"SELECT DATE(i.NgayLap) AS Ngay, SUM(i.TongTien) AS TongDoanhThu
                                         FROM HOADON i
                                         INNER JOIN DATPHONG b ON i.DatPhongID = b.DatPhongID
                                         WHERE i.NgayLap >= @FromDate
                                           AND i.NgayLap < @ToDateExclusive
                                           AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                           AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                           AND b.NgayDen >= @FromDate
                                           AND b.NgayDen < @ToDateExclusive");
            if (bookingType.HasValue)
                sql.Append(" AND b.BookingType = @BookingType");
            sql.Append(" GROUP BY DATE(i.NgayLap) ORDER BY DATE(i.NgayLap)");

            using (var cmd = new MySqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                if (bookingType.HasValue)
                    cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new RevenuePoint
                        {
                            Date = rd.IsDBNull(0) ? from : rd.GetDateTime(0),
                            Revenue = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1)
                        });
                    }
                }
            }

            return list;
        }

        private static void ApplyParameters(MySqlCommand command, IEnumerable<MySqlParameter> parameters)
        {
            foreach (var p in parameters)
            {
                command.Parameters.AddWithValue(p.ParameterName, p.Value);
            }
        }

        private static string BuildExplorerOrderBy(string sortBy)
        {
            string key = (sortBy ?? string.Empty).Trim().ToLowerInvariant();
            if (key == "checkin_asc") return "b.NgayDen ASC, b.DatPhongID ASC";
            if (key == "revenue_desc") return "TongHoaDon DESC, b.NgayDen DESC, b.DatPhongID DESC";
            if (key == "revenue_asc") return "TongHoaDon ASC, b.NgayDen DESC, b.DatPhongID DESC";
            if (key == "updated_desc") return "b.UpdatedAtUtc DESC, b.DatPhongID DESC";
            if (key == "room_asc") return "p.MaPhong ASC, b.NgayDen DESC";
            return "b.NgayDen DESC, b.DatPhongID DESC";
        }

        private static string FormatRoomType(int loaiPhongId)
        {
            if (loaiPhongId == 1) return "Phong Don";
            if (loaiPhongId == 2) return "Phong Doi";
            return "Khac";
        }

        private static string FormatBookingStatus(int status)
        {
            if (status == 0) return "Dat truoc";
            if (status == 1) return "Dang o";
            if (status == 2) return "Da tra";
            if (status == 3) return "Da huy";
            if (status == 4) return "No-show";
            return "Khong xac dinh";
        }

        private static string FormatBookingType(int bookingType)
        {
            if (bookingType == 1) return "Phòng giờ";
            if (bookingType == 2) return "Phòng ngày/đêm";
            return "Không xác định";
        }

        private static void AppendGuestListStayInfo(List<ExplorerStayLine> stayInfo, string guestListRaw)
        {
            if (stayInfo == null || string.IsNullOrWhiteSpace(guestListRaw)) return;

            var guests = guestListRaw
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            if (guests.Count == 0) return;

            stayInfo.Add(new ExplorerStayLine
            {
                Field = "Số người ở",
                Value = guests.Count.ToString(CultureInfo.InvariantCulture)
            });

            for (int i = 0; i < guests.Count; i++)
            {
                stayInfo.Add(new ExplorerStayLine
                {
                    Field = "Người ở " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    Value = FormatGuestStorageLine(guests[i])
                });
            }
        }

        private static string FormatGuestStorageLine(string rawValue)
        {
            string raw = (rawValue ?? string.Empty).Trim();
            if (raw.Length == 0) return string.Empty;

            const string separator = " - ";
            int idx = raw.IndexOf(separator, StringComparison.Ordinal);
            if (idx <= 0) return raw;

            string fullName = raw.Substring(0, idx).Trim();
            string documentNumber = raw.Substring(idx + separator.Length).Trim();
            if (fullName.Length == 0) return raw;
            if (documentNumber.Length == 0) return fullName;
            return fullName + " (Giấy tờ: " + documentNumber + ")";
        }
    }
}
