using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;
using HotelManagement.Models;
using HotelManagement.Services;

namespace HotelManagement.Data
{
    public class BookingDAL                                                         
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

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
            public int BookingType { get; set; }
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
            public int BookingType { get; set; }
            public int WaterBottleCount { get; set; }
            public int SoftDrinkCount { get; set; }
            public decimal TongTien { get; set; }
        }

        public class RoomBookingLink
        {
            public int DatPhongID { get; set; }
            public int PhongID { get; set; }
            public int TrangThai { get; set; }
            public int BookingType { get; set; }
            public DateTime NgayDen { get; set; }
            public DateTime NgayDiDuKien { get; set; }
            public DateTime? NgayDiThucTe { get; set; }
        }

        public class ActiveRoomBillingSnapshot
        {
            public int DatPhongID { get; set; }
            public int PhongID { get; set; }
            public int BookingType { get; set; }
            public DateTime NgayDen { get; set; }
            public decimal NightlyRate { get; set; }
            public int SoDemLuuTru { get; set; }
            public decimal ExtrasAmount { get; set; }
            public string ExtrasReason { get; set; }
            public decimal PaidAmount { get; set; }
        }

        public class StayInfoRecord
        {
            public int DatPhongID { get; set; }
            public string LyDoLuuTru { get; set; }
            public string GioiTinh { get; set; }
            public DateTime? NgaySinh { get; set; }
            public string LoaiGiayTo { get; set; }
            public string SoGiayTo { get; set; }
            public string QuocTich { get; set; }
            public string NoiCuTru { get; set; }
            public bool LaDiaBanCu { get; set; }
            public string MaTinhMoi { get; set; }
            public string MaXaMoi { get; set; }
            public string MaTinhCu { get; set; }
            public string MaHuyenCu { get; set; }
            public string MaXaCu { get; set; }
            public string DiaChiChiTiet { get; set; }
            public decimal GiaPhong { get; set; }
            public int SoDemLuuTru { get; set; }
            public string GuestListJson { get; set; }
        }

        public class BookingExtraRecord
        {
            public int DatPhongID { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }
            public string Note { get; set; }
        }

        public class SaveCheckinRequest
        {
            public int RoomId { get; set; }
            public int BookingType { get; set; }
            public DateTime CheckinAt { get; set; }
            public string GuestDisplayName { get; set; }
            public bool CommitRoomState { get; set; }
            public int? RentalType { get; set; }
            public StayInfoRecord StayInfo { get; set; }
        }

        public List<Booking> GetAll()
        {
            var list = new List<Booking>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc, BookingType 
                                 FROM DATPHONG
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'";
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
                            TienCoc = rd.GetDecimal(7),
                            BookingType = rd.FieldCount > 8 && !rd.IsDBNull(8) ? rd.GetInt32(8) : 2
                        });
                    }
                }
            }
            return list;
        }

        // 0 = Đặt trước, 1 = Đang ở, 2 = Đã trả, 3 = Đã hủy, 4 = No-show
        public int CreateBooking(Booking b)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                var beforeRoom = GetRoomSnapshot(conn, tx, b.PhongID);

                string query = @"INSERT INTO DATPHONG
                                 (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, BookingType, TienCoc,
                                  CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, KenhDat)
                                 VALUES(@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, NULL, @TrangThai, @BookingType, @TienCoc,
                                        @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', @KenhDat);
                                 SELECT LAST_INSERT_ID();";
                MySqlCommand cmd = new MySqlCommand(query, conn, tx);
                cmd.Parameters.AddWithValue("@KhachHangID", b.KhachHangID);
                cmd.Parameters.AddWithValue("@PhongID", b.PhongID);
                cmd.Parameters.AddWithValue("@NgayDen", b.NgayDen);
                cmd.Parameters.AddWithValue("@NgayDiDuKien", b.NgayDiDuKien);
                cmd.Parameters.AddWithValue("@TrangThai", b.TrangThai);
                cmd.Parameters.AddWithValue("@BookingType", (b.BookingType == 1 || b.BookingType == 2) ? b.BookingType : 2);
                cmd.Parameters.AddWithValue("@TienCoc", b.TienCoc);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@KenhDat", "TrucTiep");

                var result = cmd.ExecuteScalar();
                int newId = Convert.ToInt32(result);

                int initialRoomStatus = ResolveRoomStatusFromBookingStatus(b.TrangThai);
                string queryRoom = @"UPDATE PHONG
                                     SET TrangThai = @RoomStatus,
                                         UpdatedAtUtc = @UpdatedAtUtc,
                                         UpdatedBy = @UpdatedBy,
                                         DataStatus = 'active'
                                     WHERE PhongID = @PhongID";
                MySqlCommand cmdRoom = new MySqlCommand(queryRoom, conn, tx);
                cmdRoom.Parameters.AddWithValue("@RoomStatus", initialRoomStatus);
                cmdRoom.Parameters.AddWithValue("@PhongID", b.PhongID);
                cmdRoom.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmdRoom.Parameters.AddWithValue("@UpdatedBy", actor);
                cmdRoom.ExecuteNonQuery();

                var afterBooking = GetBookingSnapshot(conn, tx, newId);
                var afterRoom = GetRoomSnapshot(conn, tx, b.PhongID);

                tx.Commit();

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "DATPHONG",
                    EntityId = newId,
                    RelatedBookingId = newId,
                    RelatedRoomId = b.PhongID,
                    ActionType = "CREATE",
                    Actor = actor,
                    Source = "BookingDAL.CreateBooking",
                    AfterData = AuditLogDAL.SerializeState(afterBooking)
                });

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = b.PhongID,
                    RelatedBookingId = newId,
                    RelatedRoomId = b.PhongID,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "BookingDAL.CreateBooking",
                    BeforeData = AuditLogDAL.SerializeState(beforeRoom),
                    AfterData = AuditLogDAL.SerializeState(afterRoom)
                });

                return newId;
            }
        }

        public void UpdateStatus(int datPhongID, int status, DateTime? ngayDiThucTe)
        {
            if (datPhongID <= 0) throw new ValidationException("Booking không hợp lệ.");
            if (status < (int)BookingStatus.DatTruoc || status > (int)BookingStatus.NoShow)
                throw new ValidationException("Trạng thái booking không hợp lệ.");

            using (MySqlConnection conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                var beforeBooking = GetBookingSnapshot(conn, tx, datPhongID);

                string query = @"UPDATE DATPHONG
                                 SET TrangThai = @TrangThai,
                                     NgayDiThucTe = @NgayDiThucTe,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'active'
                                 WHERE DatPhongID = @DatPhongID";

                MySqlCommand cmd = new MySqlCommand(query, conn, tx);
                cmd.Parameters.AddWithValue("@TrangThai", status);
                if (ngayDiThucTe.HasValue)
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", ngayDiThucTe.Value);
                else
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@DatPhongID", datPhongID);
                int bookingAffected = cmd.ExecuteNonQuery();
                if (bookingAffected <= 0)
                    throw new DomainException("Không thể cập nhật trạng thái booking.");

                int roomStatus = -1;

                if (status == (int)BookingStatus.DatTruoc)
                    roomStatus = (int)RoomStatus.Trong;
                else if (status == (int)BookingStatus.DangO)
                    roomStatus = (int)RoomStatus.CoKhach;
                else if (status == (int)BookingStatus.DaTra)
                    roomStatus = (int)RoomStatus.ChuaDon;
                else if (status == (int)BookingStatus.DaHuy || status == (int)BookingStatus.NoShow)
                    roomStatus = (int)RoomStatus.Trong;

                int? relatedRoomId = null;
                var beforeRoom = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (roomStatus >= 0)
                {
                    relatedRoomId = GetRoomIdByBooking(conn, tx, datPhongID);
                    if (relatedRoomId.HasValue)
                        beforeRoom = GetRoomSnapshot(conn, tx, relatedRoomId.Value);
                }
                if (roomStatus >= 0)
                {
                    if (!relatedRoomId.HasValue)
                        throw new DomainException("Booking không còn liên kết phòng.");

                    string queryRoom;
                    if (status == (int)BookingStatus.DatTruoc ||
                        status == (int)BookingStatus.DaHuy ||
                        status == (int)BookingStatus.NoShow)
                    {
                        queryRoom = @"UPDATE PHONG p
                                      SET p.TrangThai = @RoomStatus,
                                          p.UpdatedAtUtc = @UpdatedAtUtc,
                                          p.UpdatedBy = @UpdatedBy,
                                          p.DataStatus = 'active'
                                      WHERE p.PhongID = @PhongID
                                        AND NOT EXISTS (
                                            SELECT 1
                                            FROM DATPHONG b
                                            WHERE b.PhongID = p.PhongID
                                              AND b.DatPhongID <> @DatPhongID
                                              AND b.TrangThai IN (0, 1)
                                              AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                        )";
                    }
                    else
                    {
                        queryRoom = @"UPDATE PHONG
                                      SET TrangThai = @RoomStatus,
                                          UpdatedAtUtc = @UpdatedAtUtc,
                                          UpdatedBy = @UpdatedBy,
                                          DataStatus = 'active'
                                      WHERE PhongID = @PhongID";
                    }

                    MySqlCommand cmdRoom = new MySqlCommand(queryRoom, conn, tx);
                    cmdRoom.Parameters.AddWithValue("@RoomStatus", roomStatus);
                    cmdRoom.Parameters.AddWithValue("@PhongID", relatedRoomId.Value);
                    cmdRoom.Parameters.AddWithValue("@DatPhongID", datPhongID);
                    cmdRoom.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmdRoom.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmdRoom.ExecuteNonQuery();
                }

                var afterBooking = GetBookingSnapshot(conn, tx, datPhongID);
                var afterRoom = relatedRoomId.HasValue
                    ? GetRoomSnapshot(conn, tx, relatedRoomId.Value)
                    : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                tx.Commit();

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "DATPHONG",
                    EntityId = datPhongID,
                    RelatedBookingId = datPhongID,
                    RelatedRoomId = relatedRoomId,
                    ActionType = "UPDATE_STATUS",
                    Actor = actor,
                    Source = "BookingDAL.UpdateStatus",
                    BeforeData = AuditLogDAL.SerializeState(beforeBooking),
                    AfterData = AuditLogDAL.SerializeState(afterBooking)
                });

                if (relatedRoomId.HasValue)
                {
                    _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                    {
                        EntityName = "PHONG",
                        EntityId = relatedRoomId.Value,
                        RelatedBookingId = datPhongID,
                        RelatedRoomId = relatedRoomId,
                        ActionType = "UPDATE",
                        Actor = actor,
                        Source = "BookingDAL.UpdateStatus",
                        BeforeData = AuditLogDAL.SerializeState(beforeRoom),
                        AfterData = AuditLogDAL.SerializeState(afterRoom)
                    });
                }
            }
        }

        public Booking GetById(int id)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, TienCoc, BookingType
                                 FROM DATPHONG
                                 WHERE DatPhongID = @Id
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";
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
                            TienCoc = rd.GetDecimal(7),
                            BookingType = rd.IsDBNull(8) ? 2 : rd.GetInt32(8)
                        };
                    }
                }
            }
            return null;
        }

        public decimal GetDonGiaNgayByPhong(int phongID)
        {
            var pricing = PricingService.Instance;

            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                // Ưu tiên lấy theo bảng LOAIPHONG nếu có.
                const string queryWithLoaiPhong = @"SELECT lp.DonGiaNgay
                                                    FROM PHONG p
                                                    JOIN LOAIPHONG lp ON p.LoaiPhongID = lp.LoaiPhongID
                                                    WHERE p.PhongID = @PhongID";
                try
                {
                    using (var cmd = new MySqlCommand(queryWithLoaiPhong, conn))
                    {
                        cmd.Parameters.AddWithValue("@PhongID", phongID);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToDecimal(result);
                    }
                }
                catch (MySqlException ex)
                {
                    // 1146: Table doesn't exist, 1054: Unknown column
                    if (ex.Number != 1146 && ex.Number != 1054)
                        throw;
                }

                // Fallback cho schema không có LOAIPHONG: dựa vào LoaiPhongID của PHONG.
                const string queryLoaiPhongId = @"SELECT LoaiPhongID FROM PHONG WHERE PhongID = @PhongID";
                using (var cmd = new MySqlCommand(queryLoaiPhongId, conn))
                {
                    cmd.Parameters.AddWithValue("@PhongID", phongID);
                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return 0m;

                    int loaiPhongId = Convert.ToInt32(result);
                    return pricing.GetDefaultNightlyRate(loaiPhongId);
                }
            }
        }

        public Dictionary<int, ActiveRoomBillingSnapshot> GetActiveRoomBillingSnapshotsByRoom()
        {
            var result = new Dictionary<int, ActiveRoomBillingSnapshot>();
            var pricing = PricingService.Instance.GetCurrentPricing();
            decimal defaultSingle = pricing.DefaultNightlySingle;
            decimal defaultDouble = pricing.DefaultNightlyDouble;

            using (var conn = DbHelper.GetConnection())
            {
                string queryWithLoaiPhong = @"SELECT b.DatPhongID,
                                                     b.PhongID,
                                                     CASE WHEN b.BookingType = 1 THEN 1
                                                          WHEN b.BookingType = 2 THEN 2
                                                          ELSE 2 END AS BookingType,
                                                     b.NgayDen,
                                                     COALESCE(NULLIF(s.GiaPhong, 0), lp.DonGiaNgay,
                                                              CASE WHEN p.LoaiPhongID = 1 THEN @DefaultNightSingle
                                                                   WHEN p.LoaiPhongID = 2 THEN @DefaultNightDouble
                                                                   ELSE @DefaultNightSingle END) AS NightlyRate,
                                                     COALESCE(NULLIF(s.SoDemLuuTru, 0), 1) AS SoDemLuuTru,
                                                     COALESCE((
                                                         SELECT SUM(e.Amount)
                                                         FROM BOOKING_EXTRAS e
                                                         WHERE e.DatPhongID = b.DatPhongID
                                                           AND NOT EXISTS (
                                                               SELECT 1
                                                               FROM BOOKING_EXTRAS e2
                                                               WHERE e2.DatPhongID = e.DatPhongID
                                                                 AND UPPER(TRIM(e2.ItemCode)) = UPPER(TRIM(e.ItemCode))
                                                                 AND e2.BookingExtraID > e.BookingExtraID
                                                           )
                                                     ), 0) AS ExtrasAmount,
                                                     COALESCE((
                                                         SELECT GROUP_CONCAT(CONCAT(TRIM(e.ItemName), ' x', e.Qty) SEPARATOR ', ')
                                                         FROM BOOKING_EXTRAS e
                                                         WHERE e.DatPhongID = b.DatPhongID
                                                           AND e.Qty > 0
                                                           AND COALESCE(e.ItemName, '') <> ''
                                                           AND NOT EXISTS (
                                                               SELECT 1
                                                               FROM BOOKING_EXTRAS e2
                                                               WHERE e2.DatPhongID = e.DatPhongID
                                                                 AND UPPER(TRIM(e2.ItemCode)) = UPPER(TRIM(e.ItemCode))
                                                                 AND e2.BookingExtraID > e.BookingExtraID
                                                           )
                                                     ), '') AS ExtrasReason,
                                                     COALESCE((
                                                         SELECT SUM(i.TongTien)
                                                         FROM HOADON i
                                                         WHERE i.DatPhongID = b.DatPhongID
                                                           AND i.DaThanhToan = 1
                                                           AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                                     ), 0) AS PaidAmount
                                              FROM DATPHONG b
                                              JOIN PHONG p ON p.PhongID = b.PhongID
                                              LEFT JOIN LOAIPHONG lp ON lp.LoaiPhongID = p.LoaiPhongID
                                              LEFT JOIN STAY_INFO s ON s.DatPhongID = b.DatPhongID
                                              WHERE COALESCE(b.DataStatus, 'active') <> 'deleted'
                                                AND b.DatPhongID = (
                                                    SELECT b2.DatPhongID
                                                    FROM DATPHONG b2
                                                    WHERE b2.PhongID = b.PhongID
                                                      AND b2.TrangThai IN (0, 1)
                                                      AND COALESCE(b2.DataStatus, 'active') <> 'deleted'
                                                    ORDER BY CASE WHEN b2.TrangThai = 1 THEN 0 ELSE 1 END,
                                                             b2.DatPhongID DESC
                                                    LIMIT 1
                                                )";

                try
                {
                    LoadActiveRoomBillingSnapshots(conn, queryWithLoaiPhong, result, defaultSingle, defaultDouble);
                    return result;
                }
                catch (MySqlException ex)
                {
                    if (ex.Number != 1146 && ex.Number != 1054)
                        throw;
                }

                // Fallback schema không có LOAIPHONG hoặc thiếu cột.
                string queryFallback = @"SELECT b.DatPhongID,
                                                b.PhongID,
                                                CASE WHEN b.BookingType = 1 THEN 1
                                                     WHEN b.BookingType = 2 THEN 2
                                                     ELSE 2 END AS BookingType,
                                                b.NgayDen,
                                                COALESCE(NULLIF(s.GiaPhong, 0),
                                                         CASE WHEN p.LoaiPhongID = 1 THEN @DefaultNightSingle
                                                              WHEN p.LoaiPhongID = 2 THEN @DefaultNightDouble
                                                              ELSE @DefaultNightSingle END) AS NightlyRate,
                                                COALESCE(NULLIF(s.SoDemLuuTru, 0), 1) AS SoDemLuuTru,
                                                COALESCE((
                                                    SELECT SUM(e.Amount)
                                                    FROM BOOKING_EXTRAS e
                                                    WHERE e.DatPhongID = b.DatPhongID
                                                      AND NOT EXISTS (
                                                          SELECT 1
                                                          FROM BOOKING_EXTRAS e2
                                                          WHERE e2.DatPhongID = e.DatPhongID
                                                            AND UPPER(TRIM(e2.ItemCode)) = UPPER(TRIM(e.ItemCode))
                                                            AND e2.BookingExtraID > e.BookingExtraID
                                                      )
                                                ), 0) AS ExtrasAmount,
                                                COALESCE((
                                                    SELECT GROUP_CONCAT(CONCAT(TRIM(e.ItemName), ' x', e.Qty) SEPARATOR ', ')
                                                    FROM BOOKING_EXTRAS e
                                                    WHERE e.DatPhongID = b.DatPhongID
                                                      AND e.Qty > 0
                                                      AND COALESCE(e.ItemName, '') <> ''
                                                      AND NOT EXISTS (
                                                          SELECT 1
                                                          FROM BOOKING_EXTRAS e2
                                                          WHERE e2.DatPhongID = e.DatPhongID
                                                            AND UPPER(TRIM(e2.ItemCode)) = UPPER(TRIM(e.ItemCode))
                                                            AND e2.BookingExtraID > e.BookingExtraID
                                                      )
                                                ), '') AS ExtrasReason,
                                                COALESCE((
                                                    SELECT SUM(i.TongTien)
                                                    FROM HOADON i
                                                    WHERE i.DatPhongID = b.DatPhongID
                                                      AND i.DaThanhToan = 1
                                                      AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                                ), 0) AS PaidAmount
                                         FROM DATPHONG b
                                         JOIN PHONG p ON p.PhongID = b.PhongID
                                         LEFT JOIN STAY_INFO s ON s.DatPhongID = b.DatPhongID
                                         WHERE COALESCE(b.DataStatus, 'active') <> 'deleted'
                                           AND b.DatPhongID = (
                                                SELECT b2.DatPhongID
                                                FROM DATPHONG b2
                                                WHERE b2.PhongID = b.PhongID
                                                  AND b2.TrangThai IN (0, 1)
                                                  AND COALESCE(b2.DataStatus, 'active') <> 'deleted'
                                                ORDER BY CASE WHEN b2.TrangThai = 1 THEN 0 ELSE 1 END,
                                                         b2.DatPhongID DESC
                                                LIMIT 1
                                           )";

                LoadActiveRoomBillingSnapshots(conn, queryFallback, result, defaultSingle, defaultDouble);
            }

            return result;
        }

        private static void LoadActiveRoomBillingSnapshots(
            MySqlConnection conn,
            string sql,
            Dictionary<int, ActiveRoomBillingSnapshot> buffer,
            decimal defaultSingle,
            decimal defaultDouble)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@DefaultNightSingle", defaultSingle);
                cmd.Parameters.AddWithValue("@DefaultNightDouble", defaultDouble);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int roomId = rd.IsDBNull(1) ? 0 : rd.GetInt32(1);
                        if (roomId <= 0) continue;

                        buffer[roomId] = new ActiveRoomBillingSnapshot
                        {
                            DatPhongID = rd.IsDBNull(0) ? 0 : rd.GetInt32(0),
                            PhongID = roomId,
                            BookingType = rd.IsDBNull(2) ? 2 : rd.GetInt32(2),
                            NgayDen = rd.IsDBNull(3) ? DateTime.Now : rd.GetDateTime(3),
                            NightlyRate = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4),
                            SoDemLuuTru = rd.IsDBNull(5) ? 1 : Math.Max(1, rd.GetInt32(5)),
                            ExtrasAmount = rd.IsDBNull(6) ? 0m : rd.GetDecimal(6),
                            ExtrasReason = rd.IsDBNull(7) ? string.Empty : rd.GetString(7),
                            PaidAmount = rd.IsDBNull(8) ? 0m : rd.GetDecimal(8)
                        };
                    }
                }
            }
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
                string query = @"SELECT MIN(NgayDen), MAX(NgayDen)
                                 FROM DATPHONG
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'";
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

        public string GetBookingStatisticsFingerprint(DateTime fromDate, DateTime toDate, int? bookingType = null)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string bookingSql = @"SELECT COUNT(*) AS BookingCount,
                                             COALESCE(SUM(COALESCE(b.TrangThai, 0)), 0) AS StatusSum,
                                             COALESCE(SUM(COALESCE(b.BookingType, 0)), 0) AS TypeSum,
                                             COALESCE(SUM(COALESCE(b.TienCoc, 0)), 0) AS DepositSum,
                                             COALESCE(MAX(COALESCE(b.UpdatedAtUtc, b.NgayDen)), '1970-01-01 00:00:00') AS BookingMaxUpdated,
                                             COALESCE(
                                                SUM(
                                                    CRC32(
                                                        CONCAT_WS('|',
                                                            b.DatPhongID,
                                                            COALESCE(b.PhongID, 0),
                                                            COALESCE(DATE_FORMAT(b.NgayDen, '%Y%m%d%H%i%s'), ''),
                                                            COALESCE(DATE_FORMAT(b.NgayDiDuKien, '%Y%m%d%H%i%s'), ''),
                                                            COALESCE(DATE_FORMAT(b.NgayDiThucTe, '%Y%m%d%H%i%s'), ''),
                                                            COALESCE(b.TrangThai, 0),
                                                            COALESCE(b.BookingType, 0),
                                                            COALESCE(b.TienCoc, 0)
                                                        )
                                                    )
                                                ),
                                                0
                                             ) AS BookingCrc
                                      FROM DATPHONG b
                                      WHERE b.NgayDen >= @FromDate
                                        AND b.NgayDen < @ToDateExclusive
                                        AND COALESCE(b.DataStatus, 'active') <> 'deleted'";
                if (bookingType.HasValue)
                    bookingSql += " AND b.BookingType = @BookingType";

                string invoiceSql = @"SELECT COUNT(*) AS InvoiceCount,
                                             COALESCE(SUM(COALESCE(i.TongTien, 0)), 0) AS InvoiceTotal,
                                             COALESCE(MAX(COALESCE(i.UpdatedAtUtc, i.NgayLap)), '1970-01-01 00:00:00') AS InvoiceMaxUpdated
                                      FROM HOADON i
                                      INNER JOIN DATPHONG b ON b.DatPhongID = i.DatPhongID
                                      WHERE b.NgayDen >= @FromDate
                                        AND b.NgayDen < @ToDateExclusive
                                        AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                        AND COALESCE(i.DataStatus, 'active') <> 'deleted'";
                if (bookingType.HasValue)
                    invoiceSql += " AND b.BookingType = @BookingType";

                string extraSql = @"SELECT COUNT(*) AS ExtraCount,
                                           COALESCE(SUM(COALESCE(e.Amount, 0)), 0) AS ExtraTotal,
                                           COALESCE(MAX(COALESCE(e.UpdatedAtUtc, '1970-01-01 00:00:00')), '1970-01-01 00:00:00') AS ExtraMaxUpdated
                                    FROM BOOKING_EXTRAS e
                                    INNER JOIN DATPHONG b ON b.DatPhongID = e.DatPhongID
                                    WHERE b.NgayDen >= @FromDate
                                      AND b.NgayDen < @ToDateExclusive
                                      AND COALESCE(b.DataStatus, 'active') <> 'deleted'";
                if (bookingType.HasValue)
                    extraSql += " AND b.BookingType = @BookingType";

                string bookingPart;
                using (var cmd = new MySqlCommand(bookingSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    if (bookingType.HasValue) cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return "0";
                        bookingPart = string.Join("|",
                            rd.IsDBNull(0) ? "0" : Convert.ToString(rd.GetValue(0), CultureInfo.InvariantCulture),
                            rd.IsDBNull(1) ? "0" : Convert.ToString(rd.GetValue(1), CultureInfo.InvariantCulture),
                            rd.IsDBNull(2) ? "0" : Convert.ToString(rd.GetValue(2), CultureInfo.InvariantCulture),
                            rd.IsDBNull(3) ? "0" : Convert.ToString(rd.GetValue(3), CultureInfo.InvariantCulture),
                            rd.IsDBNull(4) ? "19700101000000" : rd.GetDateTime(4).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                            rd.IsDBNull(5) ? "0" : Convert.ToString(rd.GetValue(5), CultureInfo.InvariantCulture));
                    }
                }

                string invoicePart;
                using (var cmd = new MySqlCommand(invoiceSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    if (bookingType.HasValue) cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                        {
                            invoicePart = "0|0|19700101000000";
                        }
                        else
                        {
                            invoicePart = string.Join("|",
                                rd.IsDBNull(0) ? "0" : Convert.ToString(rd.GetValue(0), CultureInfo.InvariantCulture),
                                rd.IsDBNull(1) ? "0" : Convert.ToString(rd.GetValue(1), CultureInfo.InvariantCulture),
                                rd.IsDBNull(2) ? "19700101000000" : rd.GetDateTime(2).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
                        }
                    }
                }

                string extraPart;
                using (var cmd = new MySqlCommand(extraSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    if (bookingType.HasValue) cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                        {
                            extraPart = "0|0|19700101000000";
                        }
                        else
                        {
                            extraPart = string.Join("|",
                                rd.IsDBNull(0) ? "0" : Convert.ToString(rd.GetValue(0), CultureInfo.InvariantCulture),
                                rd.IsDBNull(1) ? "0" : Convert.ToString(rd.GetValue(1), CultureInfo.InvariantCulture),
                                rd.IsDBNull(2) ? "19700101000000" : rd.GetDateTime(2).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
                        }
                    }
                }

                return bookingPart + "||" + invoicePart + "||" + extraPart;
            }
        }

        public BookingStatisticsData GetBookingStatistics(DateTime fromDate, DateTime toDate, int? bookingType = null)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);

            var rows = new List<BookingStatisticsRow>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT b.DatPhongID, b.PhongID, p.MaPhong, b.NgayDen, b.NgayDiDuKien, b.NgayDiThucTe, b.TrangThai,
                                        b.BookingType,
                                        COALESCE((
                                            SELECT SUM(CASE WHEN UPPER(e.ItemCode) = 'NS' THEN e.Qty ELSE 0 END)
                                            FROM BOOKING_EXTRAS e
                                            WHERE e.DatPhongID = b.DatPhongID
                                        ), 0) AS SoNuocSuoi,
                                        COALESCE((
                                            SELECT SUM(CASE WHEN UPPER(e.ItemCode) = 'NN' THEN e.Qty ELSE 0 END)
                                            FROM BOOKING_EXTRAS e
                                            WHERE e.DatPhongID = b.DatPhongID
                                        ), 0) AS SoNuocNgot,
                                        COALESCE((
                                            SELECT SUM(i.TongTien)
                                            FROM HOADON i
                                            WHERE i.DatPhongID = b.DatPhongID
                                              AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                        ), 0) AS TongTien
                                 FROM DATPHONG b
                                 LEFT JOIN PHONG p ON b.PhongID = p.PhongID
                                 WHERE b.NgayDen >= @FromDate AND b.NgayDen < @ToDateExclusive
                                   AND COALESCE(b.DataStatus, 'active') <> 'deleted'";

                if (bookingType.HasValue)
                    query += " AND b.BookingType = @BookingType";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    if (bookingType.HasValue)
                        cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);

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
                                BookingType = rd.IsDBNull(7) ? 2 : rd.GetInt32(7),
                                WaterBottleCount = rd.IsDBNull(8) ? 0 : rd.GetInt32(8),
                                SoftDrinkCount = rd.IsDBNull(9) ? 0 : rd.GetInt32(9),
                                TongTien = rd.IsDBNull(10) ? 0m : rd.GetDecimal(10)
                            });
                        }
                    }
                }
            }

            var details = rows
                .Select(x =>
                {
                    bool isHourly = x.BookingType == 1;

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
                        WaterBottleCount = x.WaterBottleCount,
                        SoftDrinkCount = x.SoftDrinkCount,
                        TotalAmount = x.TongTien,
                        TrangThai = x.TrangThai,
                        BookingType = x.BookingType,
                        IsHourly = isHourly
                    };
                })
                .OrderByDescending(x => x.CheckInTime)
                .ThenByDescending(x => x.DatPhongID)
                .ToList();

            var summary = new BookingSummaryStats();
            using (var conn = DbHelper.GetConnection())
            {
                string summarySql = @"SELECT
                                            COALESCE(SUM(CASE WHEN COALESCE(b.BookingType, 2) = 1 THEN 1 ELSE 0 END), 0) AS HourlyGuests,
                                            COALESCE(SUM(CASE WHEN COALESCE(b.BookingType, 2) <> 1 THEN 1 ELSE 0 END), 0) AS OvernightGuests,
                                            COALESCE(SUM(CASE WHEN COALESCE(b.TrangThai, 0) = 1 THEN 1 ELSE 0 END), 0) AS StayingBookings,
                                            COALESCE(SUM(CASE WHEN COALESCE(b.TrangThai, 0) = 2 THEN 1 ELSE 0 END), 0) AS CompletedBookings,
                                            COALESCE(SUM((
                                                SELECT COALESCE(SUM(i.TongTien), 0)
                                                FROM HOADON i
                                                WHERE i.DatPhongID = b.DatPhongID
                                                  AND COALESCE(i.DataStatus, 'active') <> 'deleted'
                                            )), 0) AS TotalRevenue
                                      FROM DATPHONG b
                                      WHERE b.NgayDen >= @FromDate
                                        AND b.NgayDen < @ToDateExclusive
                                        AND COALESCE(b.DataStatus, 'active') <> 'deleted'";
                if (bookingType.HasValue)
                    summarySql += " AND b.BookingType = @BookingType";

                using (var cmd = new MySqlCommand(summarySql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    if (bookingType.HasValue)
                        cmd.Parameters.AddWithValue("@BookingType", bookingType.Value);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            summary.HourlyGuests = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture);
                            summary.OvernightGuests = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture);
                            summary.StayingBookings = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2), CultureInfo.InvariantCulture);
                            summary.CompletedBookings = rd.IsDBNull(3) ? 0 : Convert.ToInt32(rd.GetValue(3), CultureInfo.InvariantCulture);
                            summary.TotalRevenue = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4);
                        }
                    }
                }
            }

            return new BookingStatisticsData
            {
                Summary = summary,
                Bookings = details
            };
        }

        public BookingSummaryStats GetRoomMapDailySummary(DateTime day)
        {
            DateTime from = day.Date;
            DateTime toExclusive = from.AddDays(1);
            var summary = new BookingSummaryStats();

            using (var conn = DbHelper.GetConnection())
            {
                const string countSql = @"SELECT
                                                COALESCE(SUM(CASE WHEN COALESCE(b.BookingType, 2) = 1 THEN 1 ELSE 0 END), 0) AS HourlyGuests,
                                                COALESCE(SUM(CASE WHEN COALESCE(b.BookingType, 2) <> 1 THEN 1 ELSE 0 END), 0) AS OvernightGuests
                                          FROM DATPHONG b
                                          WHERE b.NgayDen >= @FromDate
                                            AND b.NgayDen < @ToDateExclusive
                                            AND COALESCE(b.DataStatus, 'active') <> 'deleted'";
                using (var countCmd = new MySqlCommand(countSql, conn))
                {
                    countCmd.Parameters.AddWithValue("@FromDate", from);
                    countCmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    using (var rd = countCmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            summary.HourlyGuests = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture);
                            summary.OvernightGuests = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture);
                        }
                    }
                }

                const string revenueSql = @"SELECT COALESCE(SUM(i.TongTien), 0)
                                            FROM HOADON i
                                            WHERE i.NgayLap >= @FromDate
                                              AND i.NgayLap < @ToDateExclusive
                                              AND i.DaThanhToan = 1
                                              AND COALESCE(i.DataStatus, 'active') <> 'deleted'";
                using (var revenueCmd = new MySqlCommand(revenueSql, conn))
                {
                    revenueCmd.Parameters.AddWithValue("@FromDate", from);
                    revenueCmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    object val = revenueCmd.ExecuteScalar();
                    summary.TotalRevenue = val == null || val == DBNull.Value ? 0m : Convert.ToDecimal(val, CultureInfo.InvariantCulture);
                }
            }

            return summary;
        }

        private static TimeSpan CalculateDuration(DateTime checkIn, DateTime checkOut)
        {
            DateTime end = checkOut < checkIn ? checkIn : checkOut;
            var span = end - checkIn;
            return span < TimeSpan.Zero ? TimeSpan.Zero : span;
        }

        private static int ResolveRoomStatusFromBookingStatus(int bookingStatus)
        {
            if (bookingStatus == (int)BookingStatus.DangO) return (int)RoomStatus.CoKhach;
            if (bookingStatus == (int)BookingStatus.DaTra) return (int)RoomStatus.ChuaDon;
            return (int)RoomStatus.Trong;
        }

        public RoomBookingLink GetCurrentBookingByRoom(int phongId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT DatPhongID, PhongID, TrangThai, BookingType, NgayDen, NgayDiDuKien, NgayDiThucTe
                               FROM DATPHONG
                               WHERE PhongID = @PhongID
                                 AND COALESCE(DataStatus, 'active') <> 'deleted'
                                 AND TrangThai IN (0, 1)
                               ORDER BY CASE WHEN TrangThai = 1 THEN 0 ELSE 1 END,
                                        DatPhongID DESC
                               LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PhongID", phongId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return null;
                        return new RoomBookingLink
                        {
                            DatPhongID = rd.GetInt32(0),
                            PhongID = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                            TrangThai = rd.IsDBNull(2) ? 0 : rd.GetInt32(2),
                            BookingType = rd.IsDBNull(3) ? 2 : rd.GetInt32(3),
                            NgayDen = rd.IsDBNull(4) ? DateTime.Now : rd.GetDateTime(4),
                            NgayDiDuKien = rd.IsDBNull(5) ? DateTime.Now.AddHours(1) : rd.GetDateTime(5),
                            NgayDiThucTe = rd.IsDBNull(6) ? (DateTime?)null : rd.GetDateTime(6)
                        };
                    }
                }
            }
        }

        public int EnsureBookingForRoom(Room room, int bookingType)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            int safeBookingType = bookingType == (int)BookingType.Hourly ? (int)BookingType.Hourly : (int)BookingType.Overnight;

            using (var conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;

                int dbRoomStatus = 0;
                DateTime? dbStartTime = null;
                const string lockRoomSql = @"SELECT TrangThai, ThoiGianBatDau
                                             FROM PHONG
                                             WHERE PhongID = @PhongID
                                               AND COALESCE(DataStatus, 'active') <> 'deleted'
                                             FOR UPDATE";
                using (var lockCmd = new MySqlCommand(lockRoomSql, conn, tx))
                {
                    lockCmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    using (var rd = lockCmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new DomainException("Phòng không tồn tại hoặc đã bị xóa.");
                        dbRoomStatus = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                        dbStartTime = rd.IsDBNull(1) ? (DateTime?)null : rd.GetDateTime(1);
                    }
                }

                const string getCurrentSql = @"SELECT DatPhongID, BookingType
                                               FROM DATPHONG
                                               WHERE PhongID = @PhongID
                                                 AND COALESCE(DataStatus, 'active') <> 'deleted'
                                                 AND TrangThai IN (0, 1)
                                               ORDER BY CASE WHEN TrangThai = 1 THEN 0 ELSE 1 END,
                                                        DatPhongID DESC
                                               LIMIT 1
                                               FOR UPDATE";
                using (var currentCmd = new MySqlCommand(getCurrentSql, conn, tx))
                {
                    currentCmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    using (var rd = currentCmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            int currentBookingId = rd.GetInt32(0);
                            int currentBookingType = rd.IsDBNull(1) ? 2 : rd.GetInt32(1);
                            rd.Close();

                            if (currentBookingType != safeBookingType)
                            {
                                const string updateTypeSql = @"UPDATE DATPHONG
                                                               SET BookingType = @BookingType,
                                                                   UpdatedAtUtc = @UpdatedAtUtc,
                                                                   UpdatedBy = @UpdatedBy
                                                               WHERE DatPhongID = @DatPhongID";
                                using (var updateCmd = new MySqlCommand(updateTypeSql, conn, tx))
                                {
                                    updateCmd.Parameters.AddWithValue("@BookingType", safeBookingType);
                                    updateCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                                    updateCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                                    updateCmd.Parameters.AddWithValue("@DatPhongID", currentBookingId);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            return currentBookingId;
                        }
                    }
                }

                if (dbRoomStatus == (int)RoomStatus.ChuaDon)
                    throw new DomainException("Phòng đang ở trạng thái chưa dọn, không thể tạo booking mới.");

                int bookingStatus = dbRoomStatus == (int)RoomStatus.Trong
                    ? (int)BookingStatus.DatTruoc
                    : (int)BookingStatus.DangO;
                DateTime checkin = dbStartTime ?? room.ThoiGianBatDau ?? DateTime.Now;
                int customerId = EnsureDefaultCustomer(conn, tx, actor, nowUtc);
                var beforeRoom = GetRoomSnapshot(conn, tx, room.PhongID);

                const string insertBookingSql = @"INSERT INTO DATPHONG
                                                  (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, BookingType, TienCoc,
                                                   CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, KenhDat)
                                                  VALUES
                                                  (@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, NULL, @TrangThai, @BookingType, @TienCoc,
                                                   @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', @KenhDat);
                                                  SELECT LAST_INSERT_ID();";
                int bookingId;
                using (var insertCmd = new MySqlCommand(insertBookingSql, conn, tx))
                {
                    insertCmd.Parameters.AddWithValue("@KhachHangID", customerId);
                    insertCmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    insertCmd.Parameters.AddWithValue("@NgayDen", checkin);
                    insertCmd.Parameters.AddWithValue("@NgayDiDuKien", checkin.AddHours(safeBookingType == (int)BookingType.Hourly ? 4 : 24));
                    insertCmd.Parameters.AddWithValue("@TrangThai", bookingStatus);
                    insertCmd.Parameters.AddWithValue("@BookingType", safeBookingType);
                    insertCmd.Parameters.AddWithValue("@TienCoc", 0m);
                    insertCmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                    insertCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    insertCmd.Parameters.AddWithValue("@CreatedBy", actor);
                    insertCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    insertCmd.Parameters.AddWithValue("@KenhDat", "TrucTiep");
                    bookingId = Convert.ToInt32(insertCmd.ExecuteScalar());
                }

                const string updateRoomSql = @"UPDATE PHONG
                                               SET TrangThai = @TrangThai,
                                                   UpdatedAtUtc = @UpdatedAtUtc,
                                                   UpdatedBy = @UpdatedBy,
                                                   DataStatus = 'active'
                                               WHERE PhongID = @PhongID";
                using (var updateRoomCmd = new MySqlCommand(updateRoomSql, conn, tx))
                {
                    int roomStatus = bookingStatus == (int)BookingStatus.DangO
                        ? (int)RoomStatus.CoKhach
                        : ResolveRoomStatusFromBookingStatus(bookingStatus);
                    updateRoomCmd.Parameters.AddWithValue("@TrangThai", roomStatus);
                    updateRoomCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    updateRoomCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    updateRoomCmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    updateRoomCmd.ExecuteNonQuery();
                }

                var afterBooking = GetBookingSnapshot(conn, tx, bookingId);
                var afterRoom = GetRoomSnapshot(conn, tx, room.PhongID);
                tx.Commit();

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "DATPHONG",
                    EntityId = bookingId,
                    RelatedBookingId = bookingId,
                    RelatedRoomId = room.PhongID,
                    ActionType = "CREATE",
                    Actor = actor,
                    Source = "BookingDAL.EnsureBookingForRoom",
                    AfterData = AuditLogDAL.SerializeState(afterBooking)
                });

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = room.PhongID,
                    RelatedBookingId = bookingId,
                    RelatedRoomId = room.PhongID,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "BookingDAL.EnsureBookingForRoom",
                    BeforeData = AuditLogDAL.SerializeState(beforeRoom),
                    AfterData = AuditLogDAL.SerializeState(afterRoom)
                });

                return bookingId;
            }
        }

        public int SaveCheckinAtomic(SaveCheckinRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.RoomId <= 0) throw new ValidationException("Phòng không hợp lệ.");
            if (request.CheckinAt == DateTime.MinValue) throw new ValidationException("Thời điểm nhận phòng không hợp lệ.");
            int safeBookingType = request.BookingType == (int)BookingType.Hourly ? (int)BookingType.Hourly : (int)BookingType.Overnight;
            int safeRentalType = request.RentalType ?? (safeBookingType == (int)BookingType.Hourly ? (int)RentalType.Hourly : (int)RentalType.Overnight);
            if (safeRentalType != (int)RentalType.Overnight && safeRentalType != (int)RentalType.Hourly)
                throw new ValidationException("Kiểu thuê không hợp lệ.");

            using (var conn = DbHelper.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;

                var beforeRoom = GetRoomSnapshot(conn, tx, request.RoomId);
                if (beforeRoom.Count == 0)
                    throw new DomainException("Phòng không tồn tại hoặc đã bị xóa.");

                int dbRoomStatus = 0;
                DateTime? dbRoomStartTime = null;
                const string lockRoomSql = @"SELECT TrangThai, ThoiGianBatDau
                                             FROM PHONG
                                             WHERE PhongID = @PhongID
                                               AND COALESCE(DataStatus, 'active') <> 'deleted'
                                             FOR UPDATE";
                using (var lockRoomCmd = new MySqlCommand(lockRoomSql, conn, tx))
                {
                    lockRoomCmd.Parameters.AddWithValue("@PhongID", request.RoomId);
                    using (var rd = lockRoomCmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new DomainException("Phòng không tồn tại hoặc đã bị xóa.");
                        dbRoomStatus = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                        dbRoomStartTime = rd.IsDBNull(1) ? (DateTime?)null : rd.GetDateTime(1);
                    }
                }

                if (dbRoomStatus == (int)RoomStatus.ChuaDon)
                    throw new DomainException("Phòng đang ở trạng thái chưa dọn, không thể nhận phòng.");

                int bookingId = 0;
                bool bookingCreated = false;
                var beforeBooking = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                const string getCurrentBookingSql = @"SELECT DatPhongID
                                                      FROM DATPHONG
                                                      WHERE PhongID = @PhongID
                                                        AND COALESCE(DataStatus, 'active') <> 'deleted'
                                                        AND TrangThai IN (0, 1)
                                                      ORDER BY CASE WHEN TrangThai = 1 THEN 0 ELSE 1 END,
                                                               DatPhongID DESC
                                                      LIMIT 1
                                                      FOR UPDATE";
                using (var currentCmd = new MySqlCommand(getCurrentBookingSql, conn, tx))
                {
                    currentCmd.Parameters.AddWithValue("@PhongID", request.RoomId);
                    object current = currentCmd.ExecuteScalar();
                    if (current != null && current != DBNull.Value)
                        bookingId = Convert.ToInt32(current);
                }

                if (bookingId > 0)
                {
                    beforeBooking = GetBookingSnapshot(conn, tx, bookingId);
                    const string updateBookingSql = @"UPDATE DATPHONG
                                                      SET TrangThai = @TrangThai,
                                                          BookingType = @BookingType,
                                                          UpdatedAtUtc = @UpdatedAtUtc,
                                                          UpdatedBy = @UpdatedBy,
                                                          DataStatus = 'active'
                                                      WHERE DatPhongID = @DatPhongID";
                    using (var updateBookingCmd = new MySqlCommand(updateBookingSql, conn, tx))
                    {
                        updateBookingCmd.Parameters.AddWithValue("@TrangThai", (int)BookingStatus.DangO);
                        updateBookingCmd.Parameters.AddWithValue("@BookingType", safeBookingType);
                        updateBookingCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        updateBookingCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                        updateBookingCmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                        int affected = updateBookingCmd.ExecuteNonQuery();
                        if (affected <= 0)
                            throw new DomainException("Không thể cập nhật trạng thái booking.");
                    }
                }
                else
                {
                    int customerId = EnsureDefaultCustomer(conn, tx, actor, nowUtc);
                    DateTime checkin = dbRoomStartTime ?? request.CheckinAt;
                    const string insertBookingSql = @"INSERT INTO DATPHONG
                                                      (KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe, TrangThai, BookingType, TienCoc,
                                                       CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, KenhDat)
                                                      VALUES
                                                      (@KhachHangID, @PhongID, @NgayDen, @NgayDiDuKien, NULL, @TrangThai, @BookingType, @TienCoc,
                                                       @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', @KenhDat);
                                                      SELECT LAST_INSERT_ID();";
                    using (var insertBookingCmd = new MySqlCommand(insertBookingSql, conn, tx))
                    {
                        insertBookingCmd.Parameters.AddWithValue("@KhachHangID", customerId);
                        insertBookingCmd.Parameters.AddWithValue("@PhongID", request.RoomId);
                        insertBookingCmd.Parameters.AddWithValue("@NgayDen", checkin);
                        insertBookingCmd.Parameters.AddWithValue("@NgayDiDuKien", checkin.AddHours(safeBookingType == (int)BookingType.Hourly ? 4 : 24));
                        insertBookingCmd.Parameters.AddWithValue("@TrangThai", (int)BookingStatus.DangO);
                        insertBookingCmd.Parameters.AddWithValue("@BookingType", safeBookingType);
                        insertBookingCmd.Parameters.AddWithValue("@TienCoc", 0m);
                        insertBookingCmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                        insertBookingCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        insertBookingCmd.Parameters.AddWithValue("@CreatedBy", actor);
                        insertBookingCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                        insertBookingCmd.Parameters.AddWithValue("@KenhDat", "TrucTiep");
                        bookingId = Convert.ToInt32(insertBookingCmd.ExecuteScalar());
                        bookingCreated = true;
                    }
                }

                if (request.StayInfo != null)
                {
                    var stayInfoData = new StayInfoRecord
                    {
                        DatPhongID = bookingId,
                        LyDoLuuTru = request.StayInfo.LyDoLuuTru,
                        GioiTinh = request.StayInfo.GioiTinh,
                        NgaySinh = request.StayInfo.NgaySinh,
                        LoaiGiayTo = request.StayInfo.LoaiGiayTo,
                        SoGiayTo = request.StayInfo.SoGiayTo,
                        QuocTich = request.StayInfo.QuocTich,
                        NoiCuTru = request.StayInfo.NoiCuTru,
                        LaDiaBanCu = request.StayInfo.LaDiaBanCu,
                        MaTinhMoi = request.StayInfo.MaTinhMoi,
                        MaXaMoi = request.StayInfo.MaXaMoi,
                        MaTinhCu = request.StayInfo.MaTinhCu,
                        MaHuyenCu = request.StayInfo.MaHuyenCu,
                        MaXaCu = request.StayInfo.MaXaCu,
                        DiaChiChiTiet = request.StayInfo.DiaChiChiTiet,
                        GiaPhong = request.StayInfo.GiaPhong,
                        SoDemLuuTru = request.StayInfo.SoDemLuuTru,
                        GuestListJson = request.StayInfo.GuestListJson
                    };
                    UpsertStayInfoInternal(conn, tx, stayInfoData, actor, nowUtc);
                }

                if (request.CommitRoomState)
                {
                    const string updateRoomSql = @"UPDATE PHONG
                                                   SET TrangThai = @TrangThai,
                                                       ThoiGianBatDau = @ThoiGianBatDau,
                                                       KieuThue = @KieuThue,
                                                       TenKhachHienThi = @TenKhachHienThi,
                                                       UpdatedAtUtc = @UpdatedAtUtc,
                                                       UpdatedBy = @UpdatedBy,
                                                       DataStatus = 'active'
                                                   WHERE PhongID = @PhongID";
                    using (var updateRoomCmd = new MySqlCommand(updateRoomSql, conn, tx))
                    {
                        updateRoomCmd.Parameters.AddWithValue("@TrangThai", (int)RoomStatus.CoKhach);
                        updateRoomCmd.Parameters.AddWithValue("@ThoiGianBatDau", request.CheckinAt);
                        updateRoomCmd.Parameters.AddWithValue("@KieuThue", safeRentalType);
                        updateRoomCmd.Parameters.AddWithValue("@TenKhachHienThi", string.IsNullOrWhiteSpace(request.GuestDisplayName) ? (object)DBNull.Value : request.GuestDisplayName.Trim());
                        updateRoomCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        updateRoomCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                        updateRoomCmd.Parameters.AddWithValue("@PhongID", request.RoomId);
                        int affected = updateRoomCmd.ExecuteNonQuery();
                        if (affected <= 0)
                            throw new DomainException("Không thể cập nhật trạng thái phòng.");
                    }
                }

                var afterBooking = GetBookingSnapshot(conn, tx, bookingId);
                var afterRoom = GetRoomSnapshot(conn, tx, request.RoomId);
                tx.Commit();

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "DATPHONG",
                    EntityId = bookingId,
                    RelatedBookingId = bookingId,
                    RelatedRoomId = request.RoomId,
                    ActionType = bookingCreated ? "CREATE" : "UPDATE_STATUS",
                    Actor = actor,
                    Source = "BookingDAL.SaveCheckinAtomic",
                    BeforeData = bookingCreated ? null : AuditLogDAL.SerializeState(beforeBooking),
                    AfterData = AuditLogDAL.SerializeState(afterBooking)
                });

                if (request.CommitRoomState)
                {
                    _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                    {
                        EntityName = "PHONG",
                        EntityId = request.RoomId,
                        RelatedBookingId = bookingId,
                        RelatedRoomId = request.RoomId,
                        ActionType = "UPDATE",
                        Actor = actor,
                        Source = "BookingDAL.SaveCheckinAtomic",
                        BeforeData = AuditLogDAL.SerializeState(beforeRoom),
                        AfterData = AuditLogDAL.SerializeState(afterRoom)
                    });
                }

                return bookingId;
            }
        }

        public void UpdateBookingType(int datPhongId, int bookingType)
        {
            int safeBookingType = bookingType == 1 ? 1 : 2;
            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"UPDATE DATPHONG
                               SET BookingType = @BookingType,
                                   UpdatedAtUtc = @UpdatedAtUtc,
                                   UpdatedBy = @UpdatedBy
                               WHERE DatPhongID = @DatPhongID";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BookingType", safeBookingType);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@UpdatedBy", AuditContext.ResolveActor(null));
                    cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpsertStayInfo(StayInfoRecord data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.DatPhongID <= 0) throw new ArgumentOutOfRangeException(nameof(data.DatPhongID));

            string actor = AuditContext.ResolveActor(null);
            DateTime nowUtc = DateTime.UtcNow;
            using (var conn = DbHelper.GetConnection())
            {
                UpsertStayInfoInternal(conn, null, data, actor, nowUtc);
            }
        }

        public StayInfoRecord GetStayInfoByBooking(int datPhongId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT DatPhongID, LyDoLuuTru, GioiTinh, NgaySinh, LoaiGiayTo, SoGiayTo, QuocTich,
                                      NoiCuTru, LaDiaBanCu, MaTinhMoi, MaXaMoi, MaTinhCu, MaHuyenCu, MaXaCu,
                                      DiaChiChiTiet, GiaPhong, SoDemLuuTru, GuestListJson
                               FROM STAY_INFO
                               WHERE DatPhongID = @DatPhongID
                               LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return null;
                        return new StayInfoRecord
                        {
                            DatPhongID = rd.GetInt32(0),
                            LyDoLuuTru = rd.IsDBNull(1) ? null : rd.GetString(1),
                            GioiTinh = rd.IsDBNull(2) ? null : rd.GetString(2),
                            NgaySinh = rd.IsDBNull(3) ? (DateTime?)null : rd.GetDateTime(3),
                            LoaiGiayTo = rd.IsDBNull(4) ? null : rd.GetString(4),
                            SoGiayTo = rd.IsDBNull(5) ? null : rd.GetString(5),
                            QuocTich = rd.IsDBNull(6) ? null : rd.GetString(6),
                            NoiCuTru = rd.IsDBNull(7) ? null : rd.GetString(7),
                            LaDiaBanCu = !rd.IsDBNull(8) && rd.GetBoolean(8),
                            MaTinhMoi = rd.IsDBNull(9) ? null : rd.GetString(9),
                            MaXaMoi = rd.IsDBNull(10) ? null : rd.GetString(10),
                            MaTinhCu = rd.IsDBNull(11) ? null : rd.GetString(11),
                            MaHuyenCu = rd.IsDBNull(12) ? null : rd.GetString(12),
                            MaXaCu = rd.IsDBNull(13) ? null : rd.GetString(13),
                            DiaChiChiTiet = rd.IsDBNull(14) ? null : rd.GetString(14),
                            GiaPhong = rd.IsDBNull(15) ? 0m : rd.GetDecimal(15),
                            SoDemLuuTru = rd.IsDBNull(16) ? 1 : rd.GetInt32(16),
                            GuestListJson = rd.IsDBNull(17) ? null : rd.GetString(17)
                        };
                    }
                }
            }
        }

        public bool HasStayInfo(int datPhongId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                string sql = "SELECT COUNT(1) FROM STAY_INFO WHERE DatPhongID = @DatPhongID";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public List<BookingExtraRecord> GetBookingExtras(int datPhongId)
        {
            var result = new List<BookingExtraRecord>();
            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT e.DatPhongID, e.ItemCode, e.ItemName, e.Qty, e.UnitPrice, e.Amount, e.Note
                               FROM BOOKING_EXTRAS e
                               WHERE e.DatPhongID = @DatPhongID
                                 AND NOT EXISTS (
                                     SELECT 1
                                     FROM BOOKING_EXTRAS e2
                                     WHERE e2.DatPhongID = e.DatPhongID
                                       AND UPPER(TRIM(e2.ItemCode)) = UPPER(TRIM(e.ItemCode))
                                       AND e2.BookingExtraID > e.BookingExtraID
                                 )
                               ORDER BY UPPER(TRIM(e.ItemCode))";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            result.Add(new BookingExtraRecord
                            {
                                DatPhongID = rd.IsDBNull(0) ? datPhongId : rd.GetInt32(0),
                                ItemCode = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                                ItemName = rd.IsDBNull(2) ? string.Empty : rd.GetString(2),
                                Qty = rd.IsDBNull(3) ? 0 : rd.GetInt32(3),
                                UnitPrice = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4),
                                Amount = rd.IsDBNull(5) ? 0m : rd.GetDecimal(5),
                                Note = rd.IsDBNull(6) ? string.Empty : rd.GetString(6)
                            });
                        }
                    }
                }
            }
            return result;
        }

        public void UpsertBookingExtra(BookingExtraRecord line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            if (line.DatPhongID <= 0) throw new ArgumentOutOfRangeException(nameof(line.DatPhongID));
            if (string.IsNullOrWhiteSpace(line.ItemCode)) throw new ArgumentException("ItemCode is required.", nameof(line));

            string actor = AuditContext.ResolveActor(null);
            DateTime nowUtc = DateTime.UtcNow;
            int safeQty = Math.Max(0, line.Qty);
            decimal safeUnitPrice = Math.Max(0m, line.UnitPrice);
            decimal safeAmount = safeQty * safeUnitPrice;

            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"INSERT INTO BOOKING_EXTRAS
                               (DatPhongID, ItemCode, ItemName, Qty, UnitPrice, Amount, Note, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                               VALUES
                               (@DatPhongID, @ItemCode, @ItemName, @Qty, @UnitPrice, @Amount, @Note, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                               ON DUPLICATE KEY UPDATE
                               ItemName = VALUES(ItemName),
                               Qty = VALUES(Qty),
                               UnitPrice = VALUES(UnitPrice),
                               Amount = VALUES(Amount),
                               Note = VALUES(Note),
                               UpdatedAtUtc = VALUES(UpdatedAtUtc),
                               UpdatedBy = VALUES(UpdatedBy)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", line.DatPhongID);
                    cmd.Parameters.AddWithValue("@ItemCode", line.ItemCode.Trim().ToUpperInvariant());
                    cmd.Parameters.AddWithValue("@ItemName", string.IsNullOrWhiteSpace(line.ItemName) ? line.ItemCode.Trim().ToUpperInvariant() : line.ItemName.Trim());
                    cmd.Parameters.AddWithValue("@Qty", safeQty);
                    cmd.Parameters.AddWithValue("@UnitPrice", safeUnitPrice);
                    cmd.Parameters.AddWithValue("@Amount", safeAmount);
                    cmd.Parameters.AddWithValue("@Note", ToDbValue(line.Note));
                    cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@CreatedBy", actor);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public decimal GetExtrasRevenueByBooking(int datPhongId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT COALESCE(SUM(e.Amount), 0)
                               FROM BOOKING_EXTRAS e
                               WHERE e.DatPhongID = @DatPhongID
                                 AND NOT EXISTS (
                                     SELECT 1
                                     FROM BOOKING_EXTRAS e2
                                     WHERE e2.DatPhongID = e.DatPhongID
                                       AND UPPER(TRIM(e2.ItemCode)) = UPPER(TRIM(e.ItemCode))
                                       AND e2.BookingExtraID > e.BookingExtraID
                                 )";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                    object result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }
            }
        }

        public decimal GetPaidAmountByBooking(int datPhongId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT COALESCE(SUM(TongTien), 0)
                               FROM HOADON
                               WHERE DatPhongID = @DatPhongID
                                 AND DaThanhToan = 1
                                 AND COALESCE(DataStatus, 'active') <> 'deleted'";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                    object result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }
            }
        }

        private int EnsureDefaultCustomer()
        {
            using (var conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                return EnsureDefaultCustomer(conn, null, actor, nowUtc);
            }
        }

        private int EnsureDefaultCustomer(MySqlConnection conn, MySqlTransaction tx, string actor, DateTime nowUtc)
        {
            string selectSql = @"SELECT KhachHangID
                                 FROM KHACHHANG
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'
                                 ORDER BY KhachHangID
                                 LIMIT 1";
            using (var cmd = tx == null ? new MySqlCommand(selectSql, conn) : new MySqlCommand(selectSql, conn, tx))
            {
                object existing = cmd.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return Convert.ToInt32(existing);
            }

            string insertSql = @"INSERT INTO KHACHHANG
                                 (HoTen, CCCD, DienThoai, DiaChi, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus)
                                 VALUES
                                 ('Khach le', 'TEMP-BOOKING', '', '', @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active');
                                 SELECT LAST_INSERT_ID();";
            using (var cmd = tx == null ? new MySqlCommand(insertSql, conn) : new MySqlCommand(insertSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static object ToDbValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static void UpsertStayInfoInternal(
            MySqlConnection conn,
            MySqlTransaction tx,
            StayInfoRecord data,
            string actor,
            DateTime nowUtc)
        {
            string sql = @"INSERT INTO STAY_INFO
                           (DatPhongID, LyDoLuuTru, GioiTinh, NgaySinh, LoaiGiayTo, SoGiayTo, QuocTich, NoiCuTru,
                            LaDiaBanCu, MaTinhMoi, MaXaMoi, MaTinhCu, MaHuyenCu, MaXaCu, DiaChiChiTiet,
                            GiaPhong, SoDemLuuTru, GuestListJson, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                           VALUES
                           (@DatPhongID, @LyDoLuuTru, @GioiTinh, @NgaySinh, @LoaiGiayTo, @SoGiayTo, @QuocTich, @NoiCuTru,
                            @LaDiaBanCu, @MaTinhMoi, @MaXaMoi, @MaTinhCu, @MaHuyenCu, @MaXaCu, @DiaChiChiTiet,
                            @GiaPhong, @SoDemLuuTru, @GuestListJson, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                           ON DUPLICATE KEY UPDATE
                            LyDoLuuTru = VALUES(LyDoLuuTru),
                            GioiTinh = VALUES(GioiTinh),
                            NgaySinh = VALUES(NgaySinh),
                            LoaiGiayTo = VALUES(LoaiGiayTo),
                            SoGiayTo = VALUES(SoGiayTo),
                            QuocTich = VALUES(QuocTich),
                            NoiCuTru = VALUES(NoiCuTru),
                            LaDiaBanCu = VALUES(LaDiaBanCu),
                            MaTinhMoi = VALUES(MaTinhMoi),
                            MaXaMoi = VALUES(MaXaMoi),
                            MaTinhCu = VALUES(MaTinhCu),
                            MaHuyenCu = VALUES(MaHuyenCu),
                            MaXaCu = VALUES(MaXaCu),
                            DiaChiChiTiet = VALUES(DiaChiChiTiet),
                            GiaPhong = VALUES(GiaPhong),
                            SoDemLuuTru = VALUES(SoDemLuuTru),
                            GuestListJson = VALUES(GuestListJson),
                            UpdatedAtUtc = VALUES(UpdatedAtUtc),
                            UpdatedBy = VALUES(UpdatedBy)";
            using (var cmd = tx == null ? new MySqlCommand(sql, conn) : new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", data.DatPhongID);
                cmd.Parameters.AddWithValue("@LyDoLuuTru", ToDbValue(data.LyDoLuuTru));
                cmd.Parameters.AddWithValue("@GioiTinh", ToDbValue(data.GioiTinh));
                cmd.Parameters.AddWithValue("@NgaySinh", data.NgaySinh.HasValue ? (object)data.NgaySinh.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@LoaiGiayTo", ToDbValue(data.LoaiGiayTo));
                cmd.Parameters.AddWithValue("@SoGiayTo", ToDbValue(data.SoGiayTo));
                cmd.Parameters.AddWithValue("@QuocTich", ToDbValue(data.QuocTich));
                cmd.Parameters.AddWithValue("@NoiCuTru", ToDbValue(data.NoiCuTru));
                cmd.Parameters.AddWithValue("@LaDiaBanCu", data.LaDiaBanCu ? 1 : 0);
                cmd.Parameters.AddWithValue("@MaTinhMoi", ToDbValue(data.MaTinhMoi));
                cmd.Parameters.AddWithValue("@MaXaMoi", ToDbValue(data.MaXaMoi));
                cmd.Parameters.AddWithValue("@MaTinhCu", ToDbValue(data.MaTinhCu));
                cmd.Parameters.AddWithValue("@MaHuyenCu", ToDbValue(data.MaHuyenCu));
                cmd.Parameters.AddWithValue("@MaXaCu", ToDbValue(data.MaXaCu));
                cmd.Parameters.AddWithValue("@DiaChiChiTiet", ToDbValue(data.DiaChiChiTiet));
                cmd.Parameters.AddWithValue("@GiaPhong", data.GiaPhong);
                cmd.Parameters.AddWithValue("@SoDemLuuTru", data.SoDemLuuTru <= 0 ? 1 : data.SoDemLuuTru);
                cmd.Parameters.AddWithValue("@GuestListJson", ToDbValue(data.GuestListJson));
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.ExecuteNonQuery();
            }
        }

        private static Dictionary<string, object> GetBookingSnapshot(MySqlConnection conn, MySqlTransaction tx, int datPhongId)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            string sql = @"SELECT DatPhongID, KhachHangID, PhongID, NgayDen, NgayDiDuKien, NgayDiThucTe,
                                  TrangThai, BookingType, TienCoc, KenhDat, DataStatus, UpdatedAtUtc, UpdatedBy
                           FROM DATPHONG
                           WHERE DatPhongID = @DatPhongID";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return data;
                    data["DatPhongID"] = rd.IsDBNull(0) ? (object)null : rd.GetInt32(0);
                    data["KhachHangID"] = rd.IsDBNull(1) ? (object)null : rd.GetInt32(1);
                    data["PhongID"] = rd.IsDBNull(2) ? (object)null : rd.GetInt32(2);
                    data["NgayDen"] = rd.IsDBNull(3) ? (object)null : rd.GetDateTime(3);
                    data["NgayDiDuKien"] = rd.IsDBNull(4) ? (object)null : rd.GetDateTime(4);
                    data["NgayDiThucTe"] = rd.IsDBNull(5) ? (object)null : rd.GetDateTime(5);
                    data["TrangThai"] = rd.IsDBNull(6) ? (object)null : rd.GetInt32(6);
                    data["BookingType"] = rd.IsDBNull(7) ? (object)null : rd.GetInt32(7);
                    data["TienCoc"] = rd.IsDBNull(8) ? (object)null : rd.GetDecimal(8);
                    data["KenhDat"] = rd.IsDBNull(9) ? null : rd.GetString(9);
                    data["DataStatus"] = rd.IsDBNull(10) ? null : rd.GetString(10);
                    data["UpdatedAtUtc"] = rd.IsDBNull(11) ? (object)null : rd.GetDateTime(11);
                    data["UpdatedBy"] = rd.IsDBNull(12) ? null : rd.GetString(12);
                }
            }

            return data;
        }

        private static Dictionary<string, object> GetRoomSnapshot(MySqlConnection conn, MySqlTransaction tx, int phongId)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            string sql = @"SELECT PhongID, TrangThai, KieuThue, ThoiGianBatDau, TenKhachHienThi, DataStatus
                           FROM PHONG
                           WHERE PhongID = @PhongID";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@PhongID", phongId);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return data;
                    data["PhongID"] = rd.IsDBNull(0) ? (object)null : rd.GetInt32(0);
                    data["TrangThai"] = rd.IsDBNull(1) ? (object)null : rd.GetInt32(1);
                    data["KieuThue"] = rd.IsDBNull(2) ? (object)null : rd.GetInt32(2);
                    data["ThoiGianBatDau"] = rd.IsDBNull(3) ? (object)null : rd.GetDateTime(3);
                    data["TenKhachHienThi"] = rd.IsDBNull(4) ? null : rd.GetString(4);
                    data["DataStatus"] = rd.IsDBNull(5) ? null : rd.GetString(5);
                }
            }

            return data;
        }

        private static int? GetRoomIdByBooking(MySqlConnection conn, MySqlTransaction tx, int datPhongId)
        {
            string sql = "SELECT PhongID FROM DATPHONG WHERE DatPhongID = @DatPhongID LIMIT 1";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }
    }
}
