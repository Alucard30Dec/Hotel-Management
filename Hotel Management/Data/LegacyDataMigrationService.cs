using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace HotelManagement.Data
{
    public sealed class LegacyDataMigrationService
    {
        private const decimal DefaultSoftDrinkPrice = 20000m;
        private const decimal DefaultWaterPrice = 10000m;
        private const string MigrationActor = "legacy-migrate";

        private sealed class StayCandidate
        {
            public int DatPhongID { get; set; }
            public DateTime NgayDen { get; set; }
            public DateTime NgayDiDuKien { get; set; }
            public string LegacyNote { get; set; }
        }

        private sealed class ExtrasCandidate
        {
            public int DatPhongID { get; set; }
            public string LegacyRoomNote { get; set; }
        }

        public sealed class MigrationResult
        {
            public int BookingTypeBackfilled { get; set; }
            public int StayInfoInserted { get; set; }
            public int StayInfoSkippedNoLegacyData { get; set; }
            public int ExtrasInserted { get; set; }
            public int ExtrasSkippedNoLegacyData { get; set; }
            public List<int> StayInfoFailedBookingIds { get; } = new List<int>();
            public List<int> ExtrasFailedBookingIds { get; } = new List<int>();
            public string LogFilePath { get; set; }

            public string ToSummaryText()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Legacy migration finished.");
                sb.AppendLine("- BookingType backfilled: " + BookingTypeBackfilled.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- STAY_INFO inserted: " + StayInfoInserted.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- STAY_INFO skipped (no legacy tags): " + StayInfoSkippedNoLegacyData.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- BOOKING_EXTRAS inserted: " + ExtrasInserted.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- BOOKING_EXTRAS skipped (no legacy tags): " + ExtrasSkippedNoLegacyData.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- STAY_INFO errors: " + StayInfoFailedBookingIds.Count.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- EXTRAS errors: " + ExtrasFailedBookingIds.Count.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(LogFilePath))
                    sb.AppendLine("- Log file: " + LogFilePath);
                return sb.ToString().TrimEnd();
            }
        }

        public MigrationResult RunOnce()
        {
            var result = new MigrationResult();

            // Ensure latest schema before migration.
            SchemaMaintenanceService.EnsureStatisticsAndAuditSchema();

            using (var conn = DbHelper.GetConnection())
            {
                result.BookingTypeBackfilled = BackfillBookingType(conn);
                MigrateStayInfo(conn, result);
                MigrateExtras(conn, result);
            }

            result.LogFilePath = WriteLogFile(result);
            return result;
        }

        private static int BackfillBookingType(MySqlConnection conn)
        {
            string sql = @"UPDATE DATPHONG
                           SET BookingType = CASE
                                                WHEN DATE(COALESCE(NgayDiThucTe, NgayDiDuKien, NgayDen)) = DATE(NgayDen)
                                                     AND TIMESTAMPDIFF(HOUR, NgayDen, COALESCE(NgayDiThucTe, NgayDiDuKien, NgayDen)) < 10
                                                  THEN 1
                                                ELSE 2
                                             END
                           WHERE COALESCE(DataStatus, 'active') <> 'deleted'
                             AND (
                                    BookingType IS NULL
                                    OR BookingType NOT IN (1, 2)
                                    OR (
                                        BookingType = 2
                                        AND DATE(COALESCE(NgayDiThucTe, NgayDiDuKien, NgayDen)) = DATE(NgayDen)
                                        AND TIMESTAMPDIFF(HOUR, NgayDen, COALESCE(NgayDiThucTe, NgayDiDuKien, NgayDen)) < 10
                                    )
                                 )";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static void MigrateStayInfo(MySqlConnection conn, MigrationResult result)
        {
            var candidates = LoadStayCandidates(conn);
            foreach (var candidate in candidates)
            {
                try
                {
                    string note = candidate.LegacyNote ?? string.Empty;
                    if (!HasAnyStayTag(note))
                    {
                        result.StayInfoSkippedNoLegacyData++;
                        continue;
                    }

                    DateTime? ngaySinh = ParseDateTag(note, "NGSINH");
                    if (!ngaySinh.HasValue)
                        ngaySinh = ParseDateTag(note, "NS");

                    int soDem = ParsePositiveIntTag(note, "SL", false);
                    if (soDem <= 0)
                        soDem = ComputeNightCount(candidate.NgayDen, candidate.NgayDiDuKien);

                    decimal giaPhong = ParseMoneyTag(note, "GIA");
                    string loaiDiaBan = GetTag(note, "LOAIDB");
                    bool laDiaBanCu =
                        loaiDiaBan.IndexOf("cũ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        loaiDiaBan.IndexOf("cu", StringComparison.OrdinalIgnoreCase) >= 0;

                    string guestListJson = NormalizeGuestListTag(GetTag(note, "DSK"));

                    string sql = @"INSERT INTO STAY_INFO
                                   (DatPhongID, LyDoLuuTru, GioiTinh, NgaySinh, LoaiGiayTo, SoGiayTo, QuocTich, NoiCuTru,
                                    LaDiaBanCu, MaTinhMoi, MaXaMoi, MaTinhCu, MaHuyenCu, MaXaCu, DiaChiChiTiet,
                                    GiaPhong, SoDemLuuTru, GuestListJson, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                                   VALUES
                                   (@DatPhongID, @LyDoLuuTru, @GioiTinh, @NgaySinh, @LoaiGiayTo, @SoGiayTo, @QuocTich, @NoiCuTru,
                                    @LaDiaBanCu, @MaTinhMoi, @MaXaMoi, @MaTinhCu, @MaHuyenCu, @MaXaCu, @DiaChiChiTiet,
                                    @GiaPhong, @SoDemLuuTru, @GuestListJson, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)
                                   ON DUPLICATE KEY UPDATE DatPhongID = DatPhongID";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        DateTime nowUtc = DateTime.UtcNow;

                        cmd.Parameters.AddWithValue("@DatPhongID", candidate.DatPhongID);
                        cmd.Parameters.AddWithValue("@LyDoLuuTru", ToDbValue(GetTag(note, "LYDO")));
                        cmd.Parameters.AddWithValue("@GioiTinh", ToDbValue(GetTag(note, "GT")));
                        cmd.Parameters.AddWithValue("@NgaySinh", ngaySinh.HasValue ? (object)ngaySinh.Value.Date : DBNull.Value);
                        cmd.Parameters.AddWithValue("@LoaiGiayTo", ToDbValue(GetTag(note, "LGT")));
                        cmd.Parameters.AddWithValue("@SoGiayTo", ToDbValue(GetTag(note, "SGT")));
                        cmd.Parameters.AddWithValue("@QuocTich", ToDbValue(GetTag(note, "QT")));
                        cmd.Parameters.AddWithValue("@NoiCuTru", ToDbValue(GetTag(note, "NOICUTRU")));
                        cmd.Parameters.AddWithValue("@LaDiaBanCu", laDiaBanCu ? 1 : 0);
                        cmd.Parameters.AddWithValue("@MaTinhMoi", ToDbValue(FirstNotEmpty(GetTag(note, "DBMOIMATINH"), GetTag(note, "DBMATINH"))));
                        cmd.Parameters.AddWithValue("@MaXaMoi", ToDbValue(FirstNotEmpty(GetTag(note, "DBMOIMAXA"), GetTag(note, "DBMAXA"))));
                        cmd.Parameters.AddWithValue("@MaTinhCu", ToDbValue(FirstNotEmpty(GetTag(note, "DBCUMATINH"), GetTag(note, "DBMATINH"))));
                        cmd.Parameters.AddWithValue("@MaHuyenCu", ToDbValue(FirstNotEmpty(GetTag(note, "DBCUMAHUYEN"), GetTag(note, "DBMAHUYEN"))));
                        cmd.Parameters.AddWithValue("@MaXaCu", ToDbValue(FirstNotEmpty(GetTag(note, "DBCUMAXA"), GetTag(note, "DBMAXA"))));
                        cmd.Parameters.AddWithValue("@DiaChiChiTiet", ToDbValue(GetTag(note, "DBDCCT")));
                        cmd.Parameters.AddWithValue("@GiaPhong", giaPhong);
                        cmd.Parameters.AddWithValue("@SoDemLuuTru", soDem <= 0 ? 1 : soDem);
                        cmd.Parameters.AddWithValue("@GuestListJson", ToDbValue(guestListJson));
                        cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                        cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                        cmd.Parameters.AddWithValue("@CreatedBy", MigrationActor);
                        cmd.Parameters.AddWithValue("@UpdatedBy", MigrationActor);

                        cmd.ExecuteNonQuery();
                    }

                    result.StayInfoInserted++;
                }
                catch
                {
                    result.StayInfoFailedBookingIds.Add(candidate.DatPhongID);
                }
            }
        }

        private static void MigrateExtras(MySqlConnection conn, MigrationResult result)
        {
            var candidates = LoadExtrasCandidates(conn);
            foreach (var candidate in candidates)
            {
                try
                {
                    string note = candidate.LegacyRoomNote ?? string.Empty;
                    int softDrinkQty = ParsePositiveIntTag(note, "NN", false);
                    int waterQty = ParsePositiveIntTag(note, "NS", true);

                    if (softDrinkQty <= 0 && waterQty <= 0)
                    {
                        result.ExtrasSkippedNoLegacyData++;
                        continue;
                    }

                    if (InsertExtraIfMissing(conn, candidate.DatPhongID, "NN", "Nước ngọt", softDrinkQty, DefaultSoftDrinkPrice))
                        result.ExtrasInserted++;
                    if (InsertExtraIfMissing(conn, candidate.DatPhongID, "NS", "Nước suối", waterQty, DefaultWaterPrice))
                        result.ExtrasInserted++;
                }
                catch
                {
                    result.ExtrasFailedBookingIds.Add(candidate.DatPhongID);
                }
            }
        }

        private static bool InsertExtraIfMissing(MySqlConnection conn, int datPhongId, string itemCode, string itemName, int qty, decimal unitPrice)
        {
            if (qty <= 0) return false;

            string checkSql = "SELECT COUNT(1) FROM BOOKING_EXTRAS WHERE DatPhongID = @DatPhongID AND UPPER(ItemCode) = @ItemCode";
            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                checkCmd.Parameters.AddWithValue("@ItemCode", itemCode);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    return false;
            }

            decimal amount = Math.Max(0, qty) * Math.Max(0m, unitPrice);
            string insertSql = @"INSERT INTO BOOKING_EXTRAS
                                (DatPhongID, ItemCode, ItemName, Qty, UnitPrice, Amount, Note, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
                                VALUES
                                (@DatPhongID, @ItemCode, @ItemName, @Qty, @UnitPrice, @Amount, NULL, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy)";

            using (var insertCmd = new MySqlCommand(insertSql, conn))
            {
                DateTime nowUtc = DateTime.UtcNow;
                insertCmd.Parameters.AddWithValue("@DatPhongID", datPhongId);
                insertCmd.Parameters.AddWithValue("@ItemCode", itemCode);
                insertCmd.Parameters.AddWithValue("@ItemName", itemName);
                insertCmd.Parameters.AddWithValue("@Qty", qty);
                insertCmd.Parameters.AddWithValue("@UnitPrice", Math.Max(0m, unitPrice));
                insertCmd.Parameters.AddWithValue("@Amount", amount);
                insertCmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                insertCmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                insertCmd.Parameters.AddWithValue("@CreatedBy", MigrationActor);
                insertCmd.Parameters.AddWithValue("@UpdatedBy", MigrationActor);
                insertCmd.ExecuteNonQuery();
            }

            return true;
        }

        private static List<StayCandidate> LoadStayCandidates(MySqlConnection conn)
        {
            bool bookingHasNote = ColumnExists(conn, "DATPHONG", "GhiChu");
            string noteSql = bookingHasNote
                ? "COALESCE(NULLIF(b.GhiChu, ''), '')"
                : "''";

            string sql = @"SELECT b.DatPhongID, b.NgayDen, b.NgayDiDuKien, " + noteSql + @" AS LegacyNote
                           FROM DATPHONG b
                           LEFT JOIN STAY_INFO s ON s.DatPhongID = b.DatPhongID
                           WHERE COALESCE(b.DataStatus, 'active') <> 'deleted'
                             AND b.BookingType = 2
                             AND s.DatPhongID IS NULL
                           ORDER BY b.DatPhongID";

            var list = new List<StayCandidate>();
            using (var cmd = new MySqlCommand(sql, conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    list.Add(new StayCandidate
                    {
                        DatPhongID = rd.GetInt32(0),
                        NgayDen = rd.IsDBNull(1) ? DateTime.Now : rd.GetDateTime(1),
                        NgayDiDuKien = rd.IsDBNull(2) ? DateTime.Now : rd.GetDateTime(2),
                        LegacyNote = rd.IsDBNull(3) ? string.Empty : rd.GetString(3)
                    });
                }
            }

            return list;
        }

        private static List<ExtrasCandidate> LoadExtrasCandidates(MySqlConnection conn)
        {
            string sql = @"SELECT b.DatPhongID, '' AS LegacyRoomNote
                           FROM DATPHONG b
                           INNER JOIN (
                                SELECT PhongID, MAX(DatPhongID) AS LatestDatPhongID
                                FROM DATPHONG
                                WHERE COALESCE(DataStatus, 'active') <> 'deleted'
                                GROUP BY PhongID
                           ) lb ON lb.LatestDatPhongID = b.DatPhongID
                           WHERE COALESCE(b.DataStatus, 'active') <> 'deleted'
                           ORDER BY b.DatPhongID";

            var list = new List<ExtrasCandidate>();
            using (var cmd = new MySqlCommand(sql, conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    list.Add(new ExtrasCandidate
                    {
                        DatPhongID = rd.GetInt32(0),
                        LegacyRoomNote = rd.IsDBNull(1) ? string.Empty : rd.GetString(1)
                    });
                }
            }

            return list;
        }

        private static bool ColumnExists(MySqlConnection conn, string tableName, string columnName)
        {
            string sql = @"SELECT COUNT(1)
                           FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_SCHEMA = DATABASE()
                             AND TABLE_NAME = @TableName
                             AND COLUMN_NAME = @ColumnName";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static bool HasAnyStayTag(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return false;

            string[] keys =
            {
                "LYDO", "GT", "NGSINH", "LGT", "SGT", "QT", "NOICUTRU", "LOAIDB",
                "DBMATINH", "DBMAXA", "DBMOIMATINH", "DBMOIMAXA",
                "DBCUMATINH", "DBCUMAHUYEN", "DBCUMAXA", "DBDCCT", "GIA", "DSK"
            };

            return keys.Any(k => !string.IsNullOrWhiteSpace(GetTag(note, k)));
        }

        private static string GetTag(string text, string key)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key)) return string.Empty;
            Match m = Regex.Match(
                text,
                @"\b" + Regex.Escape(key) + @"\s*=\s*([^|]+)",
                RegexOptions.IgnoreCase);
            if (!m.Success) return string.Empty;
            return m.Groups[1].Value.Trim();
        }

        private static int ParsePositiveIntTag(string note, string key, bool treatDateLikeAsInvalid)
        {
            string raw = GetTag(note, key);
            if (string.IsNullOrWhiteSpace(raw)) return 0;

            string digits = Regex.Replace(raw, "[^0-9]", string.Empty);
            if (string.IsNullOrWhiteSpace(digits)) return 0;
            if (!int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return 0;

            if (treatDateLikeAsInvalid && digits.Length == 8 && value >= 19000101 && value <= 21001231)
                return 0;

            return Math.Max(0, value);
        }

        private static decimal ParseMoneyTag(string note, string key)
        {
            string raw = GetTag(note, key);
            if (string.IsNullOrWhiteSpace(raw)) return 0m;

            string digits = Regex.Replace(raw, "[^0-9]", string.Empty);
            if (string.IsNullOrWhiteSpace(digits)) return 0m;
            if (!decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) return 0m;
            return Math.Max(0m, value);
        }

        private static DateTime? ParseDateTag(string note, string key)
        {
            string raw = GetTag(note, key);
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string digits = Regex.Replace(raw, "[^0-9]", string.Empty);
            if (digits.Length != 8) return null;
            if (!DateTime.TryParseExact(digits, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
                return null;
            return value.Date;
        }

        private static int ComputeNightCount(DateTime checkIn, DateTime checkOutEstimate)
        {
            DateTime end = checkOutEstimate < checkIn ? checkIn : checkOutEstimate;
            int nights = (int)Math.Ceiling((end.Date - checkIn.Date).TotalDays);
            return nights <= 0 ? 1 : nights;
        }

        private static string NormalizeGuestListTag(string rawGuestList)
        {
            if (string.IsNullOrWhiteSpace(rawGuestList)) return string.Empty;

            var lines = rawGuestList
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x == null ? string.Empty : x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            if (lines.Count == 0) return string.Empty;
            return string.Join("\n", lines);
        }

        private static string FirstNotEmpty(string first, string second)
        {
            if (!string.IsNullOrWhiteSpace(first)) return first.Trim();
            return string.IsNullOrWhiteSpace(second) ? string.Empty : second.Trim();
        }

        private static object ToDbValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static string WriteLogFile(MigrationResult result)
        {
            try
            {
                string fileName = "legacy-migration-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".log";
                string logPath = Path.Combine(Path.GetTempPath(), fileName);

                var sb = new StringBuilder();
                sb.AppendLine(result.ToSummaryText());
                if (result.StayInfoFailedBookingIds.Count > 0)
                    sb.AppendLine("StayInfo failed booking IDs: " + string.Join(",", result.StayInfoFailedBookingIds));
                if (result.ExtrasFailedBookingIds.Count > 0)
                    sb.AppendLine("Extras failed booking IDs: " + string.Join(",", result.ExtrasFailedBookingIds));

                File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
                return logPath;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
