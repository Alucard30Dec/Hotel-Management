using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class RoomDAL
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

        public string GetRoomStateFingerprint()
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT COUNT(*) AS RoomCount,
                                        COALESCE(SUM(TrangThai), 0) AS StatusSum,
                                        COALESCE(SUM(COALESCE(KieuThue, 0)), 0) AS RentalTypeSum,
                                        COALESCE(
                                            SUM(CASE
                                                    WHEN ThoiGianBatDau IS NULL THEN 0
                                                    ELSE UNIX_TIMESTAMP(ThoiGianBatDau)
                                                END),
                                            0
                                        ) AS CheckinUnixSum,
                                        COALESCE(MAX(COALESCE(UpdatedAtUtc, ThoiGianBatDau)), '1970-01-01 00:00:00') AS MaxUpdatedAtUtc
                                 FROM PHONG
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'";

                using (var cmd = new MySqlCommand(query, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return "0";

                    string roomCount = rd.IsDBNull(0) ? "0" : Convert.ToString(rd.GetValue(0));
                    string statusSum = rd.IsDBNull(1) ? "0" : Convert.ToString(rd.GetValue(1));
                    string rentalTypeSum = rd.IsDBNull(2) ? "0" : Convert.ToString(rd.GetValue(2));
                    string checkinSum = rd.IsDBNull(3) ? "0" : Convert.ToString(rd.GetValue(3));
                    string maxUpdated = rd.IsDBNull(4) ? "19700101000000" : rd.GetDateTime(4).ToString("yyyyMMddHHmmss");

                    return string.Join("|", roomCount, statusSum, rentalTypeSum, checkinSum, maxUpdated);
                }
            }
        }

        // Lấy tất cả phòng
        public List<Room> GetAll()
        {
            var list = new List<Room>();

            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT PhongID, MaPhong, LoaiPhongID, Tang, TrangThai,
                                        ThoiGianBatDau, KieuThue, TenKhachHienThi
                                 FROM PHONG
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader rd = cmd.ExecuteReader())
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
                            ThoiGianBatDau = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                            KieuThue = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6),
                            TenKhachHienThi = rd.IsDBNull(7) ? null : rd.GetString(7)
                        };

                        list.Add(room);
                    }
                }
            }

            return list;
        }

        // Lấy 1 phòng theo ID
        public Room GetById(int id)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT PhongID, MaPhong, LoaiPhongID, Tang, TrangThai,
                                        ThoiGianBatDau, KieuThue, TenKhachHienThi
                                 FROM PHONG
                                 WHERE PhongID = @Id
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (MySqlDataReader rd = cmd.ExecuteReader())
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
                                ThoiGianBatDau = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
                                KieuThue = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6),
                                TenKhachHienThi = rd.IsDBNull(7) ? null : rd.GetString(7)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public void Insert(Room room)
        {
            CreateRoom(room);
        }

        public void Update(Room room)
        {
            UpdateRoom(room);
        }

        public void Delete(int phongId)
        {
            SoftDeleteRoom(phongId);
        }

        public int CreateRoom(Room room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            string maPhong = (room.MaPhong ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("Mã phòng không được để trống.", nameof(room));
            if (room.LoaiPhongID != 1 && room.LoaiPhongID != 2)
                throw new ArgumentException("LoaiPhongID chỉ nhận 1 hoặc 2.", nameof(room));
            if (room.Tang < 0)
                throw new ArgumentException("Tầng phải >= 0.", nameof(room));
            if (ExistsRoomCode(maPhong, null))
                throw new InvalidOperationException("Mã phòng đã tồn tại.");

            using (var conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                int safeStatus = room.TrangThai < 0 || room.TrangThai > 2 ? 0 : room.TrangThai;
                string sql = @"INSERT INTO PHONG
                               (MaPhong, LoaiPhongID, Tang, TrangThai,
                                ThoiGianBatDau, KieuThue, TenKhachHienThi,
                                CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus)
                               VALUES
                               (@MaPhong, @LoaiPhongID, @Tang, @TrangThai,
                                @ThoiGianBatDau, @KieuThue, @TenKhachHienThi,
                                @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active');
                               SELECT LAST_INSERT_ID();";

                int newId;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong);
                    cmd.Parameters.AddWithValue("@LoaiPhongID", room.LoaiPhongID);
                    cmd.Parameters.AddWithValue("@Tang", room.Tang);
                    cmd.Parameters.AddWithValue("@TrangThai", safeStatus);
                    cmd.Parameters.AddWithValue("@ThoiGianBatDau", room.ThoiGianBatDau.HasValue ? (object)room.ThoiGianBatDau.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@KieuThue", room.KieuThue.HasValue ? (object)room.KieuThue.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TenKhachHienThi", string.IsNullOrWhiteSpace(room.TenKhachHienThi) ? (object)DBNull.Value : room.TenKhachHienThi.Trim());
                    cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@CreatedBy", actor);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                var after = GetRoomSnapshot(conn, newId);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = newId,
                    RelatedRoomId = newId,
                    ActionType = "CREATE",
                    Actor = actor,
                    Source = "RoomDAL.CreateRoom",
                    AfterData = AuditLogDAL.SerializeState(after)
                });

                return newId;
            }
        }

        public void UpdateRoom(Room room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (room.PhongID <= 0) throw new ArgumentException("PhongID không hợp lệ.", nameof(room));

            string maPhong = (room.MaPhong ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("Mã phòng không được để trống.", nameof(room));
            if (room.LoaiPhongID != 1 && room.LoaiPhongID != 2)
                throw new ArgumentException("LoaiPhongID chỉ nhận 1 hoặc 2.", nameof(room));
            if (room.Tang < 0)
                throw new ArgumentException("Tầng phải >= 0.", nameof(room));
            if (ExistsRoomCode(maPhong, room.PhongID))
                throw new InvalidOperationException("Mã phòng đã tồn tại.");

            using (var conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                var before = GetRoomSnapshot(conn, room.PhongID);
                if (before.Count == 0)
                    throw new InvalidOperationException("Phòng không tồn tại.");
                int safeStatus = room.TrangThai < 0 || room.TrangThai > 2 ? 0 : room.TrangThai;

                string sql = @"UPDATE PHONG
                               SET MaPhong = @MaPhong,
                               LoaiPhongID = @LoaiPhongID,
                                   Tang = @Tang,
                                   TrangThai = @TrangThai,
                                   UpdatedAtUtc = @UpdatedAtUtc,
                                   UpdatedBy = @UpdatedBy,
                                   DataStatus = 'active'
                               WHERE PhongID = @PhongID";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong);
                    cmd.Parameters.AddWithValue("@LoaiPhongID", room.LoaiPhongID);
                    cmd.Parameters.AddWithValue("@Tang", room.Tang);
                    cmd.Parameters.AddWithValue("@TrangThai", safeStatus);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.Parameters.AddWithValue("@PhongID", room.PhongID);
                    cmd.ExecuteNonQuery();
                }

                var after = GetRoomSnapshot(conn, room.PhongID);
                int? relatedBookingId = GetLatestBookingIdByRoom(conn, room.PhongID);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = room.PhongID,
                    RelatedBookingId = relatedBookingId,
                    RelatedRoomId = room.PhongID,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "RoomDAL.UpdateRoom",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });
            }
        }

        public void SoftDeleteRoom(int phongId)
        {
            if (phongId <= 0) throw new ArgumentException("PhongID không hợp lệ.", nameof(phongId));

            using (var conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                var before = GetRoomSnapshot(conn, phongId);
                if (before.Count == 0)
                    throw new InvalidOperationException("Phòng không tồn tại.");

                int trangThai = before.TryGetValue("TrangThai", out var statusObj) && statusObj != null
                    ? Convert.ToInt32(statusObj)
                    : 0;
                if (trangThai == 1)
                    throw new InvalidOperationException("Không thể xóa phòng đang có khách.");

                if (HasActiveBooking(conn, phongId))
                    throw new InvalidOperationException("Không thể xóa phòng đang có booking active.");

                string sql = @"UPDATE PHONG
                               SET DeletedAtUtc = @DeletedAtUtc,
                                   DeletedBy = @DeletedBy,
                                   UpdatedAtUtc = @UpdatedAtUtc,
                                   UpdatedBy = @UpdatedBy,
                                   DataStatus = 'deleted'
                               WHERE PhongID = @PhongID";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    cmd.Parameters.AddWithValue("@DeletedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@DeletedBy", actor);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.Parameters.AddWithValue("@PhongID", phongId);
                    cmd.ExecuteNonQuery();
                }

                var after = GetRoomSnapshot(conn, phongId);
                int? relatedBookingId = GetLatestBookingIdByRoom(conn, phongId);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = phongId,
                    RelatedBookingId = relatedBookingId,
                    RelatedRoomId = phongId,
                    ActionType = "SOFT_DELETE",
                    Actor = actor,
                    Source = "RoomDAL.SoftDeleteRoom",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });
            }
        }

        public bool ExistsRoomCode(string maPhong, int? excludePhongId = null)
        {
            if (string.IsNullOrWhiteSpace(maPhong)) return false;

            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT COUNT(1)
                               FROM PHONG
                               WHERE UPPER(TRIM(MaPhong)) = UPPER(TRIM(@MaPhong))
                                 AND COALESCE(DataStatus, 'active') <> 'deleted'";
                if (excludePhongId.HasValue)
                    sql += " AND PhongID <> @ExcludePhongID";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong.Trim());
                    if (excludePhongId.HasValue)
                        cmd.Parameters.AddWithValue("@ExcludePhongID", excludePhongId.Value);

                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // Cập nhật trạng thái + thời gian + kiểu thuê + tên khách
        public void UpdateTrangThaiFull(int phongId,
                                        int trangThai,
                                        DateTime? thoiGianBatDau,
                                        int? kieuThue,
                                        string tenKhachHienThi)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                var before = GetRoomSnapshot(conn, phongId);
                string query = @"UPDATE PHONG SET
                                    TrangThai       = @TrangThai,
                                    ThoiGianBatDau  = @ThoiGianBatDau,
                                    KieuThue        = @KieuThue,
                                    TenKhachHienThi = @TenKhachHienThi,
                                    UpdatedAtUtc    = @UpdatedAtUtc,
                                    UpdatedBy       = @UpdatedBy,
                                    DataStatus      = 'active'
                                 WHERE PhongID = @PhongID";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    string actor = AuditContext.ResolveActor(null);
                    cmd.Parameters.AddWithValue("@TrangThai", trangThai);

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

                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.Parameters.AddWithValue("@PhongID", phongId);

                    cmd.ExecuteNonQuery();
                }

                var after = GetRoomSnapshot(conn, phongId);
                int? relatedBookingId = GetLatestBookingIdByRoom(conn, phongId);

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = phongId,
                    RelatedBookingId = relatedBookingId,
                    RelatedRoomId = phongId,
                    ActionType = "UPDATE",
                    Actor = AuditContext.ResolveActor(null),
                    Source = "RoomDAL.UpdateTrangThaiFull",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });
            }
        }

        public bool TryStartOccupancyFromEmpty(int phongId, int kieuThue, DateTime thoiGianBatDau, string tenKhachHienThi)
        {
            if (phongId <= 0) throw new ArgumentOutOfRangeException(nameof(phongId));
            if (kieuThue != (int)RentalType.Overnight && kieuThue != (int)RentalType.Hourly)
                throw new ArgumentException("KieuThue chỉ nhận 1 (đêm) hoặc 3 (giờ).", nameof(kieuThue));

            using (var conn = DbHelper.GetConnection())
            {
                var before = GetRoomSnapshot(conn, phongId);
                if (before.Count == 0)
                    throw new InvalidOperationException("Phòng không tồn tại.");

                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                const string sql = @"UPDATE PHONG p
                                     SET p.TrangThai = @TargetStatus,
                                         p.ThoiGianBatDau = @ThoiGianBatDau,
                                         p.KieuThue = @KieuThue,
                                         p.TenKhachHienThi = @TenKhachHienThi,
                                         p.UpdatedAtUtc = @UpdatedAtUtc,
                                         p.UpdatedBy = @UpdatedBy,
                                         p.DataStatus = 'active'
                                     WHERE p.PhongID = @PhongID
                                       AND p.TrangThai = @ExpectedStatus
                                       AND COALESCE(p.DataStatus, 'active') <> 'deleted'
                                       AND NOT EXISTS (
                                           SELECT 1
                                           FROM DATPHONG b
                                           WHERE b.PhongID = p.PhongID
                                             AND b.TrangThai IN (0, 1)
                                             AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                       )";
                int affected;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ThoiGianBatDau", thoiGianBatDau);
                    cmd.Parameters.AddWithValue("@KieuThue", kieuThue);
                    cmd.Parameters.AddWithValue("@TenKhachHienThi", string.IsNullOrWhiteSpace(tenKhachHienThi) ? (object)DBNull.Value : tenKhachHienThi.Trim());
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.Parameters.AddWithValue("@PhongID", phongId);
                    cmd.Parameters.AddWithValue("@TargetStatus", (int)RoomStatus.CoKhach);
                    cmd.Parameters.AddWithValue("@ExpectedStatus", (int)RoomStatus.Trong);
                    affected = cmd.ExecuteNonQuery();
                }

                if (affected <= 0)
                    return false;

                var after = GetRoomSnapshot(conn, phongId);
                int? relatedBookingId = GetLatestBookingIdByRoom(conn, phongId);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = phongId,
                    RelatedBookingId = relatedBookingId,
                    RelatedRoomId = phongId,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "RoomDAL.TryStartOccupancyFromEmpty",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });

                return true;
            }
        }

        public bool TrySetDirtyToEmpty(int phongId)
        {
            if (phongId <= 0) throw new ArgumentOutOfRangeException(nameof(phongId));

            using (var conn = DbHelper.GetConnection())
            {
                var before = GetRoomSnapshot(conn, phongId);
                if (before.Count == 0)
                    throw new InvalidOperationException("Phòng không tồn tại.");

                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                const string sql = @"UPDATE PHONG p
                                     SET p.TrangThai = @TargetStatus,
                                         p.ThoiGianBatDau = NULL,
                                         p.KieuThue = NULL,
                                         p.TenKhachHienThi = NULL,
                                         p.UpdatedAtUtc = @UpdatedAtUtc,
                                         p.UpdatedBy = @UpdatedBy,
                                         p.DataStatus = 'active'
                                     WHERE p.PhongID = @PhongID
                                       AND p.TrangThai = @ExpectedStatus
                                       AND COALESCE(p.DataStatus, 'active') <> 'deleted'
                                       AND NOT EXISTS (
                                           SELECT 1
                                           FROM DATPHONG b
                                           WHERE b.PhongID = p.PhongID
                                             AND b.TrangThai IN (0, 1)
                                             AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                       )";
                int affected;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.Parameters.AddWithValue("@PhongID", phongId);
                    cmd.Parameters.AddWithValue("@TargetStatus", (int)RoomStatus.Trong);
                    cmd.Parameters.AddWithValue("@ExpectedStatus", (int)RoomStatus.ChuaDon);
                    affected = cmd.ExecuteNonQuery();
                }

                if (affected <= 0)
                    return false;

                var after = GetRoomSnapshot(conn, phongId);
                int? relatedBookingId = GetLatestBookingIdByRoom(conn, phongId);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = phongId,
                    RelatedBookingId = relatedBookingId,
                    RelatedRoomId = phongId,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "RoomDAL.TrySetDirtyToEmpty",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });

                return true;
            }
        }

        public bool TryRollbackStartedOccupancyWithoutBooking(int phongId)
        {
            if (phongId <= 0) throw new ArgumentOutOfRangeException(nameof(phongId));

            using (var conn = DbHelper.GetConnection())
            {
                var before = GetRoomSnapshot(conn, phongId);
                if (before.Count == 0)
                    return false;

                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                const string sql = @"UPDATE PHONG p
                                     SET p.TrangThai = @TargetStatus,
                                         p.ThoiGianBatDau = NULL,
                                         p.KieuThue = NULL,
                                         p.TenKhachHienThi = NULL,
                                         p.UpdatedAtUtc = @UpdatedAtUtc,
                                         p.UpdatedBy = @UpdatedBy,
                                         p.DataStatus = 'active'
                                     WHERE p.PhongID = @PhongID
                                       AND p.TrangThai = @ExpectedStatus
                                       AND COALESCE(p.DataStatus, 'active') <> 'deleted'
                                       AND NOT EXISTS (
                                           SELECT 1
                                           FROM DATPHONG b
                                           WHERE b.PhongID = p.PhongID
                                             AND b.TrangThai IN (0, 1)
                                             AND COALESCE(b.DataStatus, 'active') <> 'deleted'
                                       )";
                int affected;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                    cmd.Parameters.AddWithValue("@PhongID", phongId);
                    cmd.Parameters.AddWithValue("@TargetStatus", (int)RoomStatus.Trong);
                    cmd.Parameters.AddWithValue("@ExpectedStatus", (int)RoomStatus.CoKhach);
                    affected = cmd.ExecuteNonQuery();
                }

                if (affected <= 0)
                    return false;

                var after = GetRoomSnapshot(conn, phongId);
                int? relatedBookingId = GetLatestBookingIdByRoom(conn, phongId);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "PHONG",
                    EntityId = phongId,
                    RelatedBookingId = relatedBookingId,
                    RelatedRoomId = phongId,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "RoomDAL.TryRollbackStartedOccupancyWithoutBooking",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });

                return true;
            }
        }

        private static Dictionary<string, object> GetRoomSnapshot(MySqlConnection conn, int phongId)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            string query = @"SELECT PhongID, MaPhong, LoaiPhongID, Tang, TrangThai,
                                    ThoiGianBatDau, KieuThue, TenKhachHienThi, DataStatus,
                                    UpdatedAtUtc, UpdatedBy
                             FROM PHONG
                             WHERE PhongID = @PhongID";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@PhongID", phongId);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return data;

                    data["PhongID"] = rd.IsDBNull(0) ? (object)null : rd.GetInt32(0);
                    data["MaPhong"] = rd.IsDBNull(1) ? null : rd.GetString(1);
                    data["LoaiPhongID"] = rd.IsDBNull(2) ? (object)null : rd.GetInt32(2);
                    data["Tang"] = rd.IsDBNull(3) ? (object)null : rd.GetInt32(3);
                    data["TrangThai"] = rd.IsDBNull(4) ? (object)null : rd.GetInt32(4);
                    data["ThoiGianBatDau"] = rd.IsDBNull(5) ? (object)null : rd.GetDateTime(5);
                    data["KieuThue"] = rd.IsDBNull(6) ? (object)null : rd.GetInt32(6);
                    data["TenKhachHienThi"] = rd.IsDBNull(7) ? null : rd.GetString(7);
                    data["DataStatus"] = rd.IsDBNull(8) ? null : rd.GetString(8);
                    data["UpdatedAtUtc"] = rd.IsDBNull(9) ? (object)null : rd.GetDateTime(9);
                    data["UpdatedBy"] = rd.IsDBNull(10) ? null : rd.GetString(10);
                }
            }

            return data;
        }

        private static bool HasActiveBooking(MySqlConnection conn, int phongId)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM DATPHONG
                                 WHERE PhongID = @PhongID
                                   AND TrangThai IN (0, 1)
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PhongID", phongId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static int? GetLatestBookingIdByRoom(MySqlConnection conn, int phongId)
        {
            const string sql = @"SELECT DatPhongID
                                 FROM DATPHONG
                                 WHERE PhongID = @PhongID
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'
                                 ORDER BY DatPhongID DESC
                                 LIMIT 1";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PhongID", phongId);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }
    }
}
