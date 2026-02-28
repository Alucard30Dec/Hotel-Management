using System;
using System.Collections.Generic;
using HotelManagement.Data;
using HotelManagement.Models;
using MySql.Data.MySqlClient;

namespace HotelManagement.Services
{
    public sealed class CheckoutService
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

        public sealed class SaveHourlyRequest
        {
            public int BookingId { get; set; }
            public int RoomId { get; set; }
            public DateTime StartTime { get; set; }
            public string GuestDisplayName { get; set; }
            public int SoftDrinkQty { get; set; }
            public int WaterBottleQty { get; set; }
            public decimal SoftDrinkUnitPrice { get; set; }
            public decimal WaterBottleUnitPrice { get; set; }
        }

        public sealed class PayHourlyRequest
        {
            public int BookingId { get; set; }
            public int RoomId { get; set; }
            public DateTime PaidAt { get; set; }
            public int SoftDrinkQty { get; set; }
            public int WaterBottleQty { get; set; }
            public decimal SoftDrinkUnitPrice { get; set; }
            public decimal WaterBottleUnitPrice { get; set; }
            public decimal DueAmount { get; set; }
        }

        public sealed class CancelHourlyRequest
        {
            public int BookingId { get; set; }
            public int RoomId { get; set; }
            public DateTime CancelledAt { get; set; }
        }

        public sealed class SaveOvernightRequest
        {
            public int BookingId { get; set; }
            public int RoomId { get; set; }
            public DateTime StartTime { get; set; }
            public string GuestDisplayName { get; set; }
            public int NightCount { get; set; }
            public decimal NightlyRate { get; set; }
            public int SoftDrinkQty { get; set; }
            public int WaterBottleQty { get; set; }
            public decimal SoftDrinkUnitPrice { get; set; }
            public decimal WaterBottleUnitPrice { get; set; }
            public decimal TargetCollectedAmount { get; set; }
        }

        public sealed class CancelOvernightRequest
        {
            public int BookingId { get; set; }
            public int RoomId { get; set; }
            public DateTime CancelledAt { get; set; }
        }

        public sealed class PayOvernightRequest
        {
            public int BookingId { get; set; }
            public int RoomId { get; set; }
            public DateTime PaidAt { get; set; }
            public DateTime StartTime { get; set; }
            public string GuestDisplayName { get; set; }
            public int NightCount { get; set; }
            public decimal NightlyRate { get; set; }
            public int SoftDrinkQty { get; set; }
            public int WaterBottleQty { get; set; }
            public decimal SoftDrinkUnitPrice { get; set; }
            public decimal WaterBottleUnitPrice { get; set; }
            public decimal TargetCollectedAmount { get; set; }
            public decimal TotalCharge { get; set; }
        }

        public sealed class CheckoutResult
        {
            public decimal AddedCollectedAmount { get; set; }
            public decimal SettlementAmount { get; set; }
            public decimal PaidAmountAfterOperation { get; set; }
        }

        public CheckoutResult SaveHourly(SaveHourlyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateBookingAndRoom(request.BookingId, request.RoomId);
            ValidateExtraInput(request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice);
            if (request.StartTime == DateTime.MinValue)
                throw new ValidationException("Thời gian bắt đầu không hợp lệ.");

            return ExecuteInTransaction(
                operation: "CheckoutService.SaveHourly",
                bookingId: request.BookingId,
                roomId: request.RoomId,
                work: (conn, tx, actor, nowUtc) =>
                {
                    LockBookingAndRoom(conn, tx, request.BookingId, request.RoomId);
                    UpsertExtras(conn, tx, request.BookingId, request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice, actor, nowUtc);
                    UpdateBookingCheckinTime(conn, tx, request.BookingId, request.StartTime, actor, nowUtc);
                    UpdateBookingStatus(conn, tx, request.BookingId, (int)BookingStatus.DangO, null, actor, nowUtc);
                    UpdateRoomState(conn, tx, request.RoomId, 1, request.StartTime, 3, request.GuestDisplayName, actor, nowUtc);

                    return new CheckoutResult
                    {
                        AddedCollectedAmount = 0m,
                        SettlementAmount = 0m,
                        PaidAmountAfterOperation = GetPaidAmount(conn, tx, request.BookingId)
                    };
                });
        }

        public CheckoutResult PayHourly(PayHourlyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateBookingAndRoom(request.BookingId, request.RoomId);
            ValidateExtraInput(request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice);
            if (request.PaidAt == DateTime.MinValue)
                throw new ValidationException("Thời điểm thanh toán không hợp lệ.");
            if (request.DueAmount < 0m)
                throw new ValidationException("Số tiền cần thu không hợp lệ.");

            return ExecuteInTransaction(
                operation: "CheckoutService.PayHourly",
                bookingId: request.BookingId,
                roomId: request.RoomId,
                work: (conn, tx, actor, nowUtc) =>
                {
                    LockBookingAndRoom(conn, tx, request.BookingId, request.RoomId);
                    UpsertExtras(conn, tx, request.BookingId, request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice, actor, nowUtc);

                    decimal settlementAmount = Math.Max(0m, request.DueAmount);
                    if (settlementAmount > 0m)
                    {
                        InsertPaidInvoice(conn, tx, request.BookingId, request.PaidAt, settlementAmount, actor, nowUtc);
                    }

                    UpdateBookingStatus(conn, tx, request.BookingId, (int)BookingStatus.DaTra, request.PaidAt, actor, nowUtc);
                    UpdateRoomState(conn, tx, request.RoomId, 2, null, null, null, actor, nowUtc);

                    return new CheckoutResult
                    {
                        AddedCollectedAmount = 0m,
                        SettlementAmount = settlementAmount,
                        PaidAmountAfterOperation = GetPaidAmount(conn, tx, request.BookingId)
                    };
                });
        }

        public CheckoutResult CancelHourly(CancelHourlyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateBookingAndRoom(request.BookingId, request.RoomId);
            if (request.CancelledAt == DateTime.MinValue)
                throw new ValidationException("Thời điểm hủy phòng không hợp lệ.");

            return ExecuteInTransaction(
                operation: "CheckoutService.CancelHourly",
                bookingId: request.BookingId,
                roomId: request.RoomId,
                work: (conn, tx, actor, nowUtc) =>
                {
                    LockBookingAndRoom(conn, tx, request.BookingId, request.RoomId);
                    UpdateBookingStatus(conn, tx, request.BookingId, (int)BookingStatus.DaHuy, request.CancelledAt, actor, nowUtc);
                    UpdateRoomState(conn, tx, request.RoomId, (int)RoomStatus.Trong, null, null, null, actor, nowUtc);

                    return new CheckoutResult
                    {
                        AddedCollectedAmount = 0m,
                        SettlementAmount = 0m,
                        PaidAmountAfterOperation = GetPaidAmount(conn, tx, request.BookingId)
                    };
                });
        }

        public CheckoutResult SaveOvernight(SaveOvernightRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateBookingAndRoom(request.BookingId, request.RoomId);
            ValidateExtraInput(request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice);
            if (request.NightCount <= 0) throw new ValidationException("Số đêm lưu trú phải lớn hơn 0.");
            if (request.NightlyRate <= 0m) throw new ValidationException("Giá phòng đêm phải lớn hơn 0.");
            if (request.TargetCollectedAmount < 0m) throw new ValidationException("Tiền đã thu không hợp lệ.");
            if (request.StartTime == DateTime.MinValue) throw new ValidationException("Thời gian bắt đầu không hợp lệ.");

            return ExecuteInTransaction(
                operation: "CheckoutService.SaveOvernight",
                bookingId: request.BookingId,
                roomId: request.RoomId,
                work: (conn, tx, actor, nowUtc) =>
                {
                    LockBookingAndRoom(conn, tx, request.BookingId, request.RoomId);

                    UpsertExtras(conn, tx, request.BookingId, request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice, actor, nowUtc);
                    UpdateStayInfoRates(conn, tx, request.BookingId, request.NightlyRate, request.NightCount, actor, nowUtc);
                    UpdateStayInfoGuestName(conn, tx, request.BookingId, request.GuestDisplayName, actor, nowUtc);
                    UpdateBookingCheckinTime(conn, tx, request.BookingId, request.StartTime, actor, nowUtc);

                    decimal paidBefore = GetPaidAmount(conn, tx, request.BookingId);
                    decimal targetCollected = Math.Max(0m, request.TargetCollectedAmount);
                    decimal addCollected = targetCollected > paidBefore ? targetCollected - paidBefore : 0m;
                    if (addCollected > 0m)
                    {
                        InsertPaidInvoice(conn, tx, request.BookingId, DateTime.Now, addCollected, actor, nowUtc);
                    }

                    UpdateBookingStatus(conn, tx, request.BookingId, (int)BookingStatus.DangO, null, actor, nowUtc);
                    UpdateRoomState(conn, tx, request.RoomId, 1, request.StartTime, 1, request.GuestDisplayName, actor, nowUtc);

                    return new CheckoutResult
                    {
                        AddedCollectedAmount = addCollected,
                        SettlementAmount = 0m,
                        PaidAmountAfterOperation = GetPaidAmount(conn, tx, request.BookingId)
                    };
                });
        }

        public CheckoutResult CancelOvernight(CancelOvernightRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateBookingAndRoom(request.BookingId, request.RoomId);
            if (request.CancelledAt == DateTime.MinValue)
                throw new ValidationException("Thời điểm hủy phòng không hợp lệ.");

            return ExecuteInTransaction(
                operation: "CheckoutService.CancelOvernight",
                bookingId: request.BookingId,
                roomId: request.RoomId,
                work: (conn, tx, actor, nowUtc) =>
                {
                    LockBookingAndRoom(conn, tx, request.BookingId, request.RoomId);
                    UpdateBookingStatus(conn, tx, request.BookingId, (int)BookingStatus.DaHuy, request.CancelledAt, actor, nowUtc);
                    UpdateRoomState(conn, tx, request.RoomId, (int)RoomStatus.Trong, null, null, null, actor, nowUtc);

                    return new CheckoutResult
                    {
                        AddedCollectedAmount = 0m,
                        SettlementAmount = 0m,
                        PaidAmountAfterOperation = GetPaidAmount(conn, tx, request.BookingId)
                    };
                });
        }

        public CheckoutResult PayOvernight(PayOvernightRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateBookingAndRoom(request.BookingId, request.RoomId);
            ValidateExtraInput(request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice);
            if (request.NightCount <= 0) throw new ValidationException("Số đêm lưu trú phải lớn hơn 0.");
            if (request.NightlyRate <= 0m) throw new ValidationException("Giá phòng đêm phải lớn hơn 0.");
            if (request.TargetCollectedAmount < 0m) throw new ValidationException("Tiền đã thu không hợp lệ.");
            if (request.TotalCharge < 0m) throw new ValidationException("Tổng tiền không hợp lệ.");
            if (request.PaidAt == DateTime.MinValue) throw new ValidationException("Thời điểm trả phòng không hợp lệ.");
            if (request.StartTime == DateTime.MinValue) throw new ValidationException("Thời gian bắt đầu không hợp lệ.");

            return ExecuteInTransaction(
                operation: "CheckoutService.PayOvernight",
                bookingId: request.BookingId,
                roomId: request.RoomId,
                work: (conn, tx, actor, nowUtc) =>
                {
                    LockBookingAndRoom(conn, tx, request.BookingId, request.RoomId);

                    UpsertExtras(conn, tx, request.BookingId, request.SoftDrinkQty, request.WaterBottleQty, request.SoftDrinkUnitPrice, request.WaterBottleUnitPrice, actor, nowUtc);
                    UpdateStayInfoRates(conn, tx, request.BookingId, request.NightlyRate, request.NightCount, actor, nowUtc);
                    UpdateStayInfoGuestName(conn, tx, request.BookingId, request.GuestDisplayName, actor, nowUtc);
                    UpdateBookingCheckinTime(conn, tx, request.BookingId, request.StartTime, actor, nowUtc);

                    decimal paidBefore = GetPaidAmount(conn, tx, request.BookingId);
                    decimal targetCollected = Math.Max(0m, request.TargetCollectedAmount);
                    decimal addCollected = targetCollected > paidBefore ? targetCollected - paidBefore : 0m;
                    if (addCollected > 0m)
                    {
                        InsertPaidInvoice(conn, tx, request.BookingId, request.PaidAt, addCollected, actor, nowUtc);
                    }

                    decimal paidAfterCollected = paidBefore + addCollected;
                    decimal settlementAmount = request.TotalCharge > paidAfterCollected
                        ? request.TotalCharge - paidAfterCollected
                        : 0m;
                    if (settlementAmount > 0m)
                    {
                        InsertPaidInvoice(conn, tx, request.BookingId, request.PaidAt, settlementAmount, actor, nowUtc);
                    }

                    UpdateBookingStatus(conn, tx, request.BookingId, (int)BookingStatus.DaTra, request.PaidAt, actor, nowUtc);
                    UpdateRoomState(conn, tx, request.RoomId, 2, null, null, null, actor, nowUtc);

                    return new CheckoutResult
                    {
                        AddedCollectedAmount = addCollected,
                        SettlementAmount = settlementAmount,
                        PaidAmountAfterOperation = GetPaidAmount(conn, tx, request.BookingId)
                    };
                });
        }

        private CheckoutResult ExecuteInTransaction(
            string operation,
            int bookingId,
            int roomId,
            Func<MySqlConnection, MySqlTransaction, string, DateTime, CheckoutResult> work)
        {
            using (AuditContext.BeginCorrelationScope())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                string correlationId = AuditContext.ResolveCorrelationId(null);

                try
                {
                    using (var conn = DbHelper.GetConnection())
                    using (var tx = conn.BeginTransaction())
                    {
                        CheckoutResult result = work(conn, tx, actor, nowUtc);
                        tx.Commit();

                        _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                        {
                            EntityName = "CHECKOUT_FLOW",
                            EntityId = bookingId,
                            RelatedBookingId = bookingId,
                            RelatedRoomId = roomId,
                            ActionType = "EXECUTE",
                            Actor = actor,
                            Source = operation,
                            CorrelationId = correlationId,
                            AfterData = AuditLogDAL.SerializeState(new Dictionary<string, object>
                            {
                                { "BookingId", bookingId },
                                { "RoomId", roomId },
                                { "AddedCollectedAmount", result == null ? 0m : result.AddedCollectedAmount },
                                { "SettlementAmount", result == null ? 0m : result.SettlementAmount },
                                { "PaidAmountAfterOperation", result == null ? 0m : result.PaidAmountAfterOperation }
                            })
                        });

                        return result ?? new CheckoutResult();
                    }
                }
                catch (ValidationException)
                {
                    throw;
                }
                catch (DomainException)
                {
                    throw;
                }
                catch (MySqlException ex)
                {
                    throw new InfrastructureException("Lỗi cơ sở dữ liệu khi xử lý checkout.", ex);
                }
                catch (InfrastructureException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InfrastructureException("Không thể hoàn tất giao dịch checkout.", ex);
                }
            }
        }

        private static void ValidateBookingAndRoom(int bookingId, int roomId)
        {
            if (bookingId <= 0) throw new ValidationException("Booking không hợp lệ.");
            if (roomId <= 0) throw new ValidationException("Phòng không hợp lệ.");
        }

        private static void ValidateExtraInput(int softDrinkQty, int waterBottleQty, decimal softDrinkUnitPrice, decimal waterBottleUnitPrice)
        {
            if (softDrinkQty < 0 || waterBottleQty < 0)
                throw new ValidationException("Số lượng nước không được âm.");
            if (softDrinkUnitPrice < 0m || waterBottleUnitPrice < 0m)
                throw new ValidationException("Đơn giá nước không được âm.");
        }

        private static void LockBookingAndRoom(MySqlConnection conn, MySqlTransaction tx, int bookingId, int roomId)
        {
            const string lockBookingSql = @"SELECT PhongID, TrangThai
                                            FROM DATPHONG
                                            WHERE DatPhongID = @DatPhongID
                                              AND COALESCE(DataStatus, 'active') <> 'deleted'
                                            FOR UPDATE";
            using (var cmd = new MySqlCommand(lockBookingSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        throw new DomainException("Booking không tồn tại hoặc đã bị xóa.");

                    int dbRoomId = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                    if (dbRoomId != roomId)
                        throw new DomainException("Booking không thuộc phòng đang xử lý.");

                    int bookingStatus = rd.IsDBNull(1) ? 0 : rd.GetInt32(1);
                    if (bookingStatus == (int)BookingStatus.DaTra || bookingStatus == (int)BookingStatus.DaHuy || bookingStatus == (int)BookingStatus.NoShow)
                        throw new DomainException("Booking đã kết thúc, không thể thao tác tiếp.");
                }
            }

            const string lockRoomSql = @"SELECT PhongID
                                         FROM PHONG
                                         WHERE PhongID = @PhongID
                                           AND COALESCE(DataStatus, 'active') <> 'deleted'
                                         FOR UPDATE";
            using (var cmd = new MySqlCommand(lockRoomSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@PhongID", roomId);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new DomainException("Phòng không tồn tại hoặc đã bị xóa.");
            }
        }

        private static void UpsertExtras(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            int softDrinkQty,
            int waterBottleQty,
            decimal softDrinkUnitPrice,
            decimal waterBottleUnitPrice,
            string actor,
            DateTime nowUtc)
        {
            UpsertOneExtra(conn, tx, bookingId, "NN", "Nước ngọt", softDrinkQty, softDrinkUnitPrice, actor, nowUtc);
            UpsertOneExtra(conn, tx, bookingId, "NS", "Nước suối", waterBottleQty, waterBottleUnitPrice, actor, nowUtc);
        }

        private static void UpsertOneExtra(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            string itemCode,
            string itemName,
            int qty,
            decimal unitPrice,
            string actor,
            DateTime nowUtc)
        {
            int safeQty = Math.Max(0, qty);
            decimal safeUnitPrice = Math.Max(0m, unitPrice);
            decimal amount = safeQty * safeUnitPrice;
            const string sql = @"INSERT INTO BOOKING_EXTRAS
                                 (DatPhongID, ItemCode, ItemName, Qty, UnitPrice, Amount, Note, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                                 VALUES
                                 (@DatPhongID, @ItemCode, @ItemName, @Qty, @UnitPrice, @Amount, NULL, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                                 ON DUPLICATE KEY UPDATE
                                 ItemName = VALUES(ItemName),
                                 Qty = VALUES(Qty),
                                 UnitPrice = VALUES(UnitPrice),
                                 Amount = VALUES(Amount),
                                 UpdatedAtUtc = VALUES(UpdatedAtUtc),
                                 UpdatedBy = VALUES(UpdatedBy)";

            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                cmd.Parameters.AddWithValue("@ItemName", itemName);
                cmd.Parameters.AddWithValue("@Qty", safeQty);
                cmd.Parameters.AddWithValue("@UnitPrice", safeUnitPrice);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateStayInfoRates(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            decimal nightlyRate,
            int nightCount,
            string actor,
            DateTime nowUtc)
        {
            const string sql = @"INSERT INTO STAY_INFO
                                 (DatPhongID, GiaPhong, SoDemLuuTru, LaDiaBanCu, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                                 VALUES
                                 (@DatPhongID, @GiaPhong, @SoDemLuuTru, @LaDiaBanCu, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                                 ON DUPLICATE KEY UPDATE
                                 GiaPhong = VALUES(GiaPhong),
                                 SoDemLuuTru = VALUES(SoDemLuuTru),
                                 UpdatedAtUtc = VALUES(UpdatedAtUtc),
                                 UpdatedBy = VALUES(UpdatedBy)";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                cmd.Parameters.AddWithValue("@GiaPhong", Math.Max(0m, nightlyRate));
                cmd.Parameters.AddWithValue("@SoDemLuuTru", Math.Max(1, nightCount));
                // Keep nightly checkout path independent from guest-detail flow.
                // Existing rows keep their value on update; new rows default to non-legacy area.
                cmd.Parameters.AddWithValue("@LaDiaBanCu", 0);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateStayInfoGuestName(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            string guestDisplayName,
            string actor,
            DateTime nowUtc)
        {
            string name = string.IsNullOrWhiteSpace(guestDisplayName) ? string.Empty : guestDisplayName.Trim();
            if (name.Length == 0) return;

            const string updateSql = @"UPDATE STAY_INFO
                                       SET GuestListJson = @GuestListJson,
                                           UpdatedAtUtc = @UpdatedAtUtc,
                                           UpdatedBy = @UpdatedBy
                                       WHERE DatPhongID = @DatPhongID";
            using (var updateCmd = new MySqlCommand(updateSql, conn, tx))
            {
                updateCmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                updateCmd.Parameters.AddWithValue("@GuestListJson", name);
                updateCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                updateCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                int affected = updateCmd.ExecuteNonQuery();
                if (affected > 0) return;
            }

            const string insertSql = @"INSERT INTO STAY_INFO
                                       (DatPhongID, GuestListJson, GiaPhong, SoDemLuuTru, LaDiaBanCu, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                                       VALUES
                                       (@DatPhongID, @GuestListJson, @GiaPhong, @SoDemLuuTru, 0, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)";
            using (var insertCmd = new MySqlCommand(insertSql, conn, tx))
            {
                insertCmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                insertCmd.Parameters.AddWithValue("@GuestListJson", name);
                insertCmd.Parameters.AddWithValue("@GiaPhong", 0m);
                insertCmd.Parameters.AddWithValue("@SoDemLuuTru", 1);
                insertCmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                insertCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                insertCmd.Parameters.AddWithValue("@CreatedBy", actor);
                insertCmd.Parameters.AddWithValue("@UpdatedBy", actor);
                insertCmd.ExecuteNonQuery();
            }
        }

        private static void InsertPaidInvoice(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            DateTime paidAt,
            decimal amount,
            string actor,
            DateTime nowUtc)
        {
            decimal safeAmount = Math.Max(0m, amount);
            if (safeAmount <= 0m) return;

            const string sql = @"INSERT INTO HOADON
                                 (DatPhongID, NgayLap, TongTien, DaThanhToan,
                                  CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus, PaymentStatus)
                                 VALUES
                                 (@DatPhongID, @NgayLap, @TongTien, 1,
                                  @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active', 'paid')";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                cmd.Parameters.AddWithValue("@NgayLap", paidAt == DateTime.MinValue ? DateTime.Now : paidAt);
                cmd.Parameters.AddWithValue("@TongTien", safeAmount);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateBookingStatus(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            int bookingStatus,
            DateTime? checkoutAt,
            string actor,
            DateTime nowUtc)
        {
            const string sql = @"UPDATE DATPHONG
                                 SET TrangThai = @TrangThai,
                                     NgayDiThucTe = @NgayDiThucTe,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'active'
                                 WHERE DatPhongID = @DatPhongID";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TrangThai", bookingStatus);
                if (checkoutAt.HasValue)
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", checkoutAt.Value);
                else
                    cmd.Parameters.AddWithValue("@NgayDiThucTe", DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                int affected = cmd.ExecuteNonQuery();
                if (affected <= 0)
                    throw new DomainException("Không thể cập nhật trạng thái booking.");
            }
        }

        private static void UpdateBookingCheckinTime(
            MySqlConnection conn,
            MySqlTransaction tx,
            int bookingId,
            DateTime checkinAt,
            string actor,
            DateTime nowUtc)
        {
            if (checkinAt == DateTime.MinValue)
                throw new ValidationException("Thời gian check-in không hợp lệ.");

            const string sql = @"UPDATE DATPHONG
                                 SET NgayDen = @NgayDen,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'active'
                                 WHERE DatPhongID = @DatPhongID";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@NgayDen", checkinAt);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                int affected = cmd.ExecuteNonQuery();
                if (affected <= 0)
                    throw new DomainException("Không thể cập nhật thời gian check-in booking.");
            }
        }

        private static void UpdateRoomState(
            MySqlConnection conn,
            MySqlTransaction tx,
            int roomId,
            int roomStatus,
            DateTime? startTime,
            int? rentalType,
            string guestDisplayName,
            string actor,
            DateTime nowUtc)
        {
            const string sql = @"UPDATE PHONG
                                 SET TrangThai = @TrangThai,
                                     ThoiGianBatDau = @ThoiGianBatDau,
                                     KieuThue = @KieuThue,
                                     TenKhachHienThi = @TenKhachHienThi,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'active'
                                 WHERE PhongID = @PhongID";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TrangThai", roomStatus);
                cmd.Parameters.AddWithValue("@ThoiGianBatDau", startTime.HasValue ? (object)startTime.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@KieuThue", rentalType.HasValue ? (object)rentalType.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@TenKhachHienThi", string.IsNullOrWhiteSpace(guestDisplayName) ? (object)DBNull.Value : guestDisplayName.Trim());
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@PhongID", roomId);
                int affected = cmd.ExecuteNonQuery();
                if (affected <= 0)
                    throw new DomainException("Không thể cập nhật trạng thái phòng.");
            }
        }

        private static decimal GetPaidAmount(MySqlConnection conn, MySqlTransaction tx, int bookingId)
        {
            const string sql = @"SELECT COALESCE(SUM(TongTien), 0)
                                 FROM HOADON
                                 WHERE DatPhongID = @DatPhongID
                                   AND DaThanhToan = 1
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DatPhongID", bookingId);
                object value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
            }
        }
    }
}
