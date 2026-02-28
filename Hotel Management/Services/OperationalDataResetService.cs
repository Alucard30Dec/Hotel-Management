using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using HotelManagement.Data;
using MySql.Data.MySqlClient;

namespace HotelManagement.Services
{
    public sealed class OperationalDataResetService
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

        public sealed class ResetResult
        {
            public int RoomsReset { get; set; }
            public int BookingsDeleted { get; set; }
            public int InvoicesDeleted { get; set; }
            public int StayInfoDeleted { get; set; }
            public int ExtrasDeleted { get; set; }

            public string ToSummaryText()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Đã reset dữ liệu vận hành thành công.");
                sb.AppendLine();
                sb.AppendLine("Phòng đã đưa về trạng thái trống: " + RoomsReset.ToString("N0"));
                sb.AppendLine("Booking đã xóa: " + BookingsDeleted.ToString("N0"));
                sb.AppendLine("Hóa đơn đã xóa: " + InvoicesDeleted.ToString("N0"));
                sb.AppendLine("STAY_INFO đã xóa: " + StayInfoDeleted.ToString("N0"));
                sb.AppendLine("BOOKING_EXTRAS đã xóa: " + ExtrasDeleted.ToString("N0"));
                return sb.ToString().TrimEnd();
            }
        }

        public ResetResult ResetRoomStateAndBookingHistory()
        {
            var result = new ResetResult();
            string actor = AuditContext.ResolveActor(null);

            using (PerformanceTracker.Measure("OperationalDataResetService.ResetRoomStateAndBookingHistory"))
            using (var db = new HotelDbContext())
            {
                db.Database.Initialize(false);
                using (var tx = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        var conn = db.Database.Connection as MySqlConnection;
                        if (conn == null)
                            throw new InfrastructureException("Không thể khởi tạo MySqlConnection từ DbContext.");
                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        var mysqlTx = tx.UnderlyingTransaction as MySqlTransaction;

                        result.ExtrasDeleted = DeleteAllRows(conn, mysqlTx, "BOOKING_EXTRAS");
                        result.StayInfoDeleted = DeleteAllRows(conn, mysqlTx, "STAY_INFO");
                        result.InvoicesDeleted = DeleteAllRows(conn, mysqlTx, "HOADON");
                        result.BookingsDeleted = DeleteAllRows(conn, mysqlTx, "DATPHONG");
                        result.RoomsReset = ResetRoomsToEmpty(conn, mysqlTx, actor);

                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        throw new InfrastructureException("Reset dữ liệu thất bại.", ex);
                    }
                }
            }

            _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
            {
                EntityName = "SYSTEM",
                ActionType = "RESET_OPERATIONAL_DATA",
                Actor = actor,
                Source = "OperationalDataResetService.ResetRoomStateAndBookingHistory",
                AfterData = "RoomsReset=" + result.RoomsReset + ";BookingsDeleted=" + result.BookingsDeleted + ";InvoicesDeleted=" + result.InvoicesDeleted
            });

            AppLogger.Info("Operational data reset completed.", new Dictionary<string, object>
            {
                ["RoomsReset"] = result.RoomsReset,
                ["BookingsDeleted"] = result.BookingsDeleted,
                ["InvoicesDeleted"] = result.InvoicesDeleted,
                ["StayInfoDeleted"] = result.StayInfoDeleted,
                ["ExtrasDeleted"] = result.ExtrasDeleted
            });

            return result;
        }

        private static int DeleteAllRows(MySqlConnection conn, MySqlTransaction tx, string tableName)
        {
            if (!TableExists(conn, tx, tableName)) return 0;

            string safeTable = QuoteIdentifier(tableName);
            int affected;
            using (var cmd = new MySqlCommand("DELETE FROM " + safeTable, conn, tx))
            {
                affected = cmd.ExecuteNonQuery();
            }

            return affected;
        }

        private static int ResetRoomsToEmpty(MySqlConnection conn, MySqlTransaction tx, string actor)
        {
            const string tableName = "PHONG";
            if (!TableExists(conn, tx, tableName)) return 0;

            bool hasRoomStatus = ColumnExists(conn, tx, tableName, "TrangThai");
            bool hasStartTime = ColumnExists(conn, tx, tableName, "ThoiGianBatDau");
            bool hasRentalType = ColumnExists(conn, tx, tableName, "KieuThue");
            bool hasGuestName = ColumnExists(conn, tx, tableName, "TenKhachHienThi");
            bool hasDataStatus = ColumnExists(conn, tx, tableName, "DataStatus");
            bool hasUpdatedAt = ColumnExists(conn, tx, tableName, "UpdatedAtUtc");
            bool hasUpdatedBy = ColumnExists(conn, tx, tableName, "UpdatedBy");

            var updates = new List<string>();
            if (hasRoomStatus) updates.Add("`TrangThai` = 0");
            if (hasStartTime) updates.Add("`ThoiGianBatDau` = NULL");
            if (hasRentalType) updates.Add("`KieuThue` = NULL");
            if (hasGuestName) updates.Add("`TenKhachHienThi` = NULL");
            if (hasUpdatedAt) updates.Add("`UpdatedAtUtc` = UTC_TIMESTAMP()");
            if (hasUpdatedBy) updates.Add("`UpdatedBy` = @UpdatedBy");

            if (updates.Count == 0) return 0;

            string sql = "UPDATE `PHONG` SET " + string.Join(", ", updates);
            if (hasDataStatus)
                sql += " WHERE COALESCE(`DataStatus`, 'active') <> 'deleted'";

            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                if (hasUpdatedBy)
                    cmd.Parameters.AddWithValue("@UpdatedBy", string.IsNullOrWhiteSpace(actor) ? "system:reset" : actor);
                return cmd.ExecuteNonQuery();
            }
        }

        private static bool TableExists(MySqlConnection conn, MySqlTransaction tx, string tableName)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM INFORMATION_SCHEMA.TABLES
                                 WHERE TABLE_SCHEMA = DATABASE()
                                   AND TABLE_NAME = @TableName";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static bool ColumnExists(MySqlConnection conn, MySqlTransaction tx, string tableName, string columnName)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM INFORMATION_SCHEMA.COLUMNS
                                 WHERE TABLE_SCHEMA = DATABASE()
                                   AND TABLE_NAME = @TableName
                                   AND COLUMN_NAME = @ColumnName";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static string QuoteIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Tên bảng không hợp lệ.");
            if (!name.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
                throw new ValidationException("Tên bảng chứa ký tự không hợp lệ: " + name);
            return "`" + name + "`";
        }
    }
}
