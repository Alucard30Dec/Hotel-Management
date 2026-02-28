using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using HotelManagement.Services;

namespace HotelManagement.Data
{
    public class AuditLogDAL
    {
        public class AuditLogEntry
        {
            public int AuditLogID { get; set; }
            public string EntityName { get; set; }
            public int? EntityId { get; set; }
            public int? RelatedBookingId { get; set; }
            public int? RelatedRoomId { get; set; }
            public int? RelatedInvoiceId { get; set; }
            public string ActionType { get; set; }
            public string Actor { get; set; }
            public string Source { get; set; }
            public string CorrelationId { get; set; }
            public string BeforeData { get; set; }
            public string AfterData { get; set; }
            public DateTime OccurredAtUtc { get; set; }
        }

        public class AuditLogPage
        {
            public int TotalCount { get; set; }
            public List<AuditLogEntry> Items { get; set; }
        }

        public class AuditLogWriteModel
        {
            public string EntityName { get; set; }
            public int? EntityId { get; set; }
            public int? RelatedBookingId { get; set; }
            public int? RelatedRoomId { get; set; }
            public int? RelatedInvoiceId { get; set; }
            public string ActionType { get; set; }
            public string Actor { get; set; }
            public string Source { get; set; }
            public string CorrelationId { get; set; }
            public string BeforeData { get; set; }
            public string AfterData { get; set; }
        }

        public void Write(AuditLogWriteModel model)
        {
            if (model == null) return;
            if (string.IsNullOrWhiteSpace(model.EntityName) || string.IsNullOrWhiteSpace(model.ActionType)) return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    string sql = @"INSERT INTO AUDIT_LOG
                                   (EntityName, EntityId, RelatedBookingId, RelatedRoomId, RelatedInvoiceId, ActionType,
                                    Actor, Source, CorrelationId, BeforeData, AfterData, OccurredAtUtc)
                                   VALUES
                                   (@EntityName, @EntityId, @RelatedBookingId, @RelatedRoomId, @RelatedInvoiceId, @ActionType,
                                    @Actor, @Source, @CorrelationId, @BeforeData, @AfterData, @OccurredAtUtc)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@EntityName", Normalize(model.EntityName, 50));
                        cmd.Parameters.AddWithValue("@EntityId", ToDb(model.EntityId));
                        cmd.Parameters.AddWithValue("@RelatedBookingId", ToDb(model.RelatedBookingId));
                        cmd.Parameters.AddWithValue("@RelatedRoomId", ToDb(model.RelatedRoomId));
                        cmd.Parameters.AddWithValue("@RelatedInvoiceId", ToDb(model.RelatedInvoiceId));
                        cmd.Parameters.AddWithValue("@ActionType", Normalize(model.ActionType, 30));
                        cmd.Parameters.AddWithValue("@Actor", Normalize(AuditContext.ResolveActor(model.Actor), 80));
                        cmd.Parameters.AddWithValue("@Source", Normalize(model.Source, 120));
                        cmd.Parameters.AddWithValue("@CorrelationId", Normalize(AuditContext.ResolveCorrelationId(model.CorrelationId), 100));
                        cmd.Parameters.AddWithValue("@BeforeData", (object)(model.BeforeData ?? string.Empty));
                        cmd.Parameters.AddWithValue("@AfterData", (object)(model.AfterData ?? string.Empty));
                        cmd.Parameters.AddWithValue("@OccurredAtUtc", DateTime.UtcNow);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Cannot write audit log.", new Dictionary<string, object>
                {
                    ["EntityName"] = model.EntityName,
                    ["EntityId"] = model.EntityId,
                    ["ActionType"] = model.ActionType,
                    ["Source"] = model.Source
                });
            }
        }

        public AuditLogPage GetAuditLogs(DateTime fromDate, DateTime toDate, string entityName, string actor, string keyword, int page, int pageSize)
        {
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);
            int safePage = page < 1 ? 1 : page;
            int safePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 200);
            int offset = (safePage - 1) * safePageSize;

            var where = new StringBuilder(" WHERE OccurredAtUtc >= @FromDate AND OccurredAtUtc < @ToDateExclusive");
            var extraParams = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(entityName) && !string.Equals(entityName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND EntityName = @EntityName");
                extraParams.Add(new MySqlParameter("@EntityName", entityName.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(actor))
            {
                where.Append(" AND Actor = @Actor");
                extraParams.Add(new MySqlParameter("@Actor", actor.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where.Append(" AND (BeforeData LIKE @Keyword OR AfterData LIKE @Keyword OR Source LIKE @Keyword OR CorrelationId LIKE @Keyword)");
                extraParams.Add(new MySqlParameter("@Keyword", "%" + keyword.Trim() + "%"));
            }

            var result = new AuditLogPage
            {
                TotalCount = 0,
                Items = new List<AuditLogEntry>()
            };

            using (var conn = DbHelper.GetConnection())
            {
                string countSql = "SELECT COUNT(1) FROM AUDIT_LOG" + where;
                using (var countCmd = new MySqlCommand(countSql, conn))
                {
                    countCmd.Parameters.AddWithValue("@FromDate", from);
                    countCmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    foreach (var p in extraParams)
                        countCmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                string dataSql = @"SELECT AuditLogID, EntityName, EntityId, RelatedBookingId, RelatedRoomId, RelatedInvoiceId,
                                          ActionType, Actor, Source, CorrelationId, BeforeData, AfterData, OccurredAtUtc
                                   FROM AUDIT_LOG" + where + @"
                                   ORDER BY OccurredAtUtc DESC, AuditLogID DESC
                                   LIMIT @Limit OFFSET @Offset";

                using (var cmd = new MySqlCommand(dataSql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", from);
                    cmd.Parameters.AddWithValue("@ToDateExclusive", toExclusive);
                    foreach (var p in extraParams)
                        cmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                    cmd.Parameters.AddWithValue("@Limit", safePageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            result.Items.Add(new AuditLogEntry
                            {
                                AuditLogID = rd.GetInt32(0),
                                EntityName = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                                EntityId = rd.IsDBNull(2) ? (int?)null : rd.GetInt32(2),
                                RelatedBookingId = rd.IsDBNull(3) ? (int?)null : rd.GetInt32(3),
                                RelatedRoomId = rd.IsDBNull(4) ? (int?)null : rd.GetInt32(4),
                                RelatedInvoiceId = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5),
                                ActionType = rd.IsDBNull(6) ? string.Empty : rd.GetString(6),
                                Actor = rd.IsDBNull(7) ? string.Empty : rd.GetString(7),
                                Source = rd.IsDBNull(8) ? string.Empty : rd.GetString(8),
                                CorrelationId = rd.IsDBNull(9) ? string.Empty : rd.GetString(9),
                                BeforeData = rd.IsDBNull(10) ? string.Empty : rd.GetString(10),
                                AfterData = rd.IsDBNull(11) ? string.Empty : rd.GetString(11),
                                OccurredAtUtc = rd.IsDBNull(12) ? DateTime.UtcNow : rd.GetDateTime(12)
                            });
                        }
                    }
                }
            }

            return result;
        }

        public List<AuditLogEntry> GetTimelineByBooking(int bookingId, int? roomId, int maxItems)
        {
            int safeMax = maxItems <= 0 ? 200 : Math.Min(maxItems, 1000);
            var list = new List<AuditLogEntry>();

            using (var conn = DbHelper.GetConnection())
            {
                string sql = @"SELECT AuditLogID, EntityName, EntityId, RelatedBookingId, RelatedRoomId, RelatedInvoiceId,
                                      ActionType, Actor, Source, CorrelationId, BeforeData, AfterData, OccurredAtUtc
                               FROM AUDIT_LOG
                               WHERE RelatedBookingId = @BookingId
                                  OR (EntityName = 'DATPHONG' AND EntityId = @BookingId)
                                  OR (@RoomId IS NOT NULL AND RelatedRoomId = @RoomId)
                               ORDER BY OccurredAtUtc DESC, AuditLogID DESC
                               LIMIT @Limit";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BookingId", bookingId);
                    cmd.Parameters.AddWithValue("@RoomId", ToDb(roomId));
                    cmd.Parameters.AddWithValue("@Limit", safeMax);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new AuditLogEntry
                            {
                                AuditLogID = rd.GetInt32(0),
                                EntityName = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                                EntityId = rd.IsDBNull(2) ? (int?)null : rd.GetInt32(2),
                                RelatedBookingId = rd.IsDBNull(3) ? (int?)null : rd.GetInt32(3),
                                RelatedRoomId = rd.IsDBNull(4) ? (int?)null : rd.GetInt32(4),
                                RelatedInvoiceId = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5),
                                ActionType = rd.IsDBNull(6) ? string.Empty : rd.GetString(6),
                                Actor = rd.IsDBNull(7) ? string.Empty : rd.GetString(7),
                                Source = rd.IsDBNull(8) ? string.Empty : rd.GetString(8),
                                CorrelationId = rd.IsDBNull(9) ? string.Empty : rd.GetString(9),
                                BeforeData = rd.IsDBNull(10) ? string.Empty : rd.GetString(10),
                                AfterData = rd.IsDBNull(11) ? string.Empty : rd.GetString(11),
                                OccurredAtUtc = rd.IsDBNull(12) ? DateTime.UtcNow : rd.GetDateTime(12)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public static string SerializeState(IDictionary<string, object> data)
        {
            if (data == null || data.Count == 0) return string.Empty;

            var ordered = data
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sb = new StringBuilder(256);
            foreach (var kv in ordered)
            {
                string value = FormatValue(kv.Value);
                sb.Append(kv.Key).Append('=').Append(value).Append("; ");
            }

            return sb.ToString().Trim();
        }

        private static string FormatValue(object value)
        {
            if (value == null || value == DBNull.Value) return "null";

            if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (value is bool)
                return (bool)value ? "1" : "0";

            if (value is decimal)
                return ((decimal)value).ToString("0.##", CultureInfo.InvariantCulture);

            if (value is double)
                return ((double)value).ToString("0.##", CultureInfo.InvariantCulture);

            if (value is float)
                return ((float)value).ToString("0.##", CultureInfo.InvariantCulture);

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string Normalize(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static object ToDb(object value)
        {
            return value ?? DBNull.Value;
        }
    }
}
