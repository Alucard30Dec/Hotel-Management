using System;
using System.Collections.Generic;
using System.Globalization;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class SettingsDAL
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

        public List<HotelSetting> GetAll()
        {
            var list = new List<HotelSetting>();
            const string sql = @"SELECT `Key`, `Value`, UpdatedAtUtc, UpdatedBy, DataStatus
                                 FROM HOTEL_SETTINGS
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'
                                 ORDER BY `Key`";

            using (var conn = DbHelper.GetConnection())
            using (var cmd = new MySqlCommand(sql, conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    list.Add(new HotelSetting
                    {
                        Key = rd.IsDBNull(0) ? string.Empty : rd.GetString(0),
                        Value = rd.IsDBNull(1) ? null : rd.GetString(1),
                        UpdatedAtUtc = rd.IsDBNull(2) ? DateTime.UtcNow : rd.GetDateTime(2),
                        UpdatedBy = rd.IsDBNull(3) ? null : rd.GetString(3),
                        DataStatus = rd.IsDBNull(4) ? null : rd.GetString(4)
                    });
                }
            }

            return list;
        }

        public decimal GetDecimal(string key, decimal defaultValue)
        {
            string raw = GetRawValue(key);
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

            if (decimal.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;

            if (decimal.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;

            return defaultValue;
        }

        public int GetInt(string key, int defaultValue)
        {
            string raw = GetRawValue(key);
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

            if (int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;

            if (int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;

            return defaultValue;
        }

        public string GetRawValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            const string sql = @"SELECT `Value`
                                 FROM HOTEL_SETTINGS
                                 WHERE `Key` = @Key
                                   AND COALESCE(DataStatus, 'active') <> 'deleted'
                                 LIMIT 1";

            using (var conn = DbHelper.GetConnection())
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Key", key.Trim());
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToString(result, CultureInfo.InvariantCulture);
            }
        }

        public void Upsert(string key, string value, string actor, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Setting key is required.", nameof(key));

            string safeKey = key.Trim();
            string safeActor = AuditContext.ResolveActor(actor);

            using (var conn = DbHelper.GetConnection())
            {
                var before = GetSettingSnapshot(conn, safeKey);

                const string sql = @"INSERT INTO HOTEL_SETTINGS (`Key`, `Value`, UpdatedAtUtc, UpdatedBy, DataStatus)
                                     VALUES (@Key, @Value, @UpdatedAtUtc, @UpdatedBy, 'active')
                                     ON DUPLICATE KEY UPDATE
                                        `Value` = VALUES(`Value`),
                                        UpdatedAtUtc = VALUES(UpdatedAtUtc),
                                        UpdatedBy = VALUES(UpdatedBy),
                                        DataStatus = 'active'";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", safeKey);
                    cmd.Parameters.AddWithValue("@Value", (object)value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                    cmd.Parameters.AddWithValue("@UpdatedBy", safeActor);
                    cmd.ExecuteNonQuery();
                }

                var after = GetSettingSnapshot(conn, safeKey);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "HOTEL_SETTINGS",
                    EntityId = null,
                    ActionType = before.Count == 0 ? "CREATE" : "UPDATE",
                    Actor = safeActor,
                    Source = "SettingsDAL.Upsert",
                    CorrelationId = safeKey,
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });
            }
        }

        public void Upsert(string key, decimal value, string actor, DateTime nowUtc)
        {
            Upsert(key, value.ToString("0.##", CultureInfo.InvariantCulture), actor, nowUtc);
        }

        public void Upsert(string key, int value, string actor, DateTime nowUtc)
        {
            Upsert(key, value.ToString(CultureInfo.InvariantCulture), actor, nowUtc);
        }

        private static Dictionary<string, object> GetSettingSnapshot(MySqlConnection conn, string key)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"SELECT `Key`, `Value`, UpdatedAtUtc, UpdatedBy, DataStatus
                                 FROM HOTEL_SETTINGS
                                 WHERE `Key` = @Key
                                 LIMIT 1";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Key", key);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return data;

                    data["Key"] = rd.IsDBNull(0) ? null : rd.GetString(0);
                    data["Value"] = rd.IsDBNull(1) ? null : rd.GetString(1);
                    data["UpdatedAtUtc"] = rd.IsDBNull(2) ? (object)null : rd.GetDateTime(2);
                    data["UpdatedBy"] = rd.IsDBNull(3) ? null : rd.GetString(3);
                    data["DataStatus"] = rd.IsDBNull(4) ? null : rd.GetString(4);
                }
            }

            return data;
        }
    }
}
