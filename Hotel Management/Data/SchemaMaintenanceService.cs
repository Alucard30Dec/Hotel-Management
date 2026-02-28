using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace HotelManagement.Data
{
    public static class SchemaMaintenanceService
    {
        private sealed class ColumnSpec
        {
            public string TableName { get; set; }
            public string ColumnName { get; set; }
            public string DefinitionSql { get; set; }
        }

        public static void EnsureStatisticsAndAuditSchema()
        {
            using (var conn = DbHelper.GetConnection())
            {
                EnsureCoreAuditColumns(conn);
                EnsureAuditLogTable(conn);
                EnsureBookingCoreSchema(conn);
                BackfillCoreAuditColumns(conn);
                EnsureStatisticsIndexes(conn);
            }
        }

        private static void EnsureCoreAuditColumns(MySqlConnection conn)
        {
            var specs = new List<ColumnSpec>
            {
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "CreatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "UpdatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "DeletedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "CreatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "UpdatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "DeletedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "DataStatus", DefinitionSql = "VARCHAR(32) NULL" },
                new ColumnSpec { TableName = "DATPHONG", ColumnName = "KenhDat", DefinitionSql = "VARCHAR(64) NULL" },

                new ColumnSpec { TableName = "HOADON", ColumnName = "CreatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "UpdatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "DeletedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "CreatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "UpdatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "DeletedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "DataStatus", DefinitionSql = "VARCHAR(32) NULL" },
                new ColumnSpec { TableName = "HOADON", ColumnName = "PaymentStatus", DefinitionSql = "VARCHAR(32) NULL" },

                new ColumnSpec { TableName = "PHONG", ColumnName = "CreatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "PHONG", ColumnName = "UpdatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "PHONG", ColumnName = "DeletedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "PHONG", ColumnName = "CreatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "PHONG", ColumnName = "UpdatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "PHONG", ColumnName = "DeletedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "PHONG", ColumnName = "DataStatus", DefinitionSql = "VARCHAR(32) NULL" },

                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "CreatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "UpdatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "DeletedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "CreatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "UpdatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "DeletedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "KHACHHANG", ColumnName = "DataStatus", DefinitionSql = "VARCHAR(32) NULL" },

                new ColumnSpec { TableName = "USERS", ColumnName = "CreatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "USERS", ColumnName = "UpdatedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "USERS", ColumnName = "DeletedAtUtc", DefinitionSql = "DATETIME NULL" },
                new ColumnSpec { TableName = "USERS", ColumnName = "CreatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "USERS", ColumnName = "UpdatedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "USERS", ColumnName = "DeletedBy", DefinitionSql = "VARCHAR(80) NULL" },
                new ColumnSpec { TableName = "USERS", ColumnName = "DataStatus", DefinitionSql = "VARCHAR(32) NULL" }
            };

            foreach (var spec in specs)
            {
                if (ColumnExists(conn, spec.TableName, spec.ColumnName)) continue;

                string sql = "ALTER TABLE `" + spec.TableName + "` ADD COLUMN `" + spec.ColumnName + "` " + spec.DefinitionSql;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureAuditLogTable(MySqlConnection conn)
        {
            string createSql = @"CREATE TABLE IF NOT EXISTS `AUDIT_LOG` (
                                    `AuditLogID` INT NOT NULL AUTO_INCREMENT,
                                    `EntityName` VARCHAR(50) NOT NULL,
                                    `EntityId` INT NULL,
                                    `RelatedBookingId` INT NULL,
                                    `RelatedRoomId` INT NULL,
                                    `RelatedInvoiceId` INT NULL,
                                    `ActionType` VARCHAR(30) NOT NULL,
                                    `Actor` VARCHAR(80) NULL,
                                    `Source` VARCHAR(120) NULL,
                                    `CorrelationId` VARCHAR(100) NULL,
                                    `BeforeData` LONGTEXT NULL,
                                    `AfterData` LONGTEXT NULL,
                                    `OccurredAtUtc` DATETIME NOT NULL,
                                    PRIMARY KEY (`AuditLogID`),
                                    INDEX `IDX_AUDITLOG_OCCURRED` (`OccurredAtUtc`),
                                    INDEX `IDX_AUDITLOG_ENTITY` (`EntityName`, `EntityId`),
                                    INDEX `IDX_AUDITLOG_BOOKING` (`RelatedBookingId`),
                                    INDEX `IDX_AUDITLOG_ROOM` (`RelatedRoomId`)
                                )";

            using (var cmd = new MySqlCommand(createSql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureBookingCoreSchema(MySqlConnection conn)
        {
            EnsureRoomTypePricingSchema(conn);
            EnsureHotelSettingsSchema(conn);
            SeedHotelSettingsDefaults(conn);

            if (!ColumnExists(conn, "DATPHONG", "BookingType"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE DATPHONG ADD COLUMN BookingType TINYINT NOT NULL DEFAULT 2");
            }
            else
            {
                ExecuteNonQuery(conn, "ALTER TABLE DATPHONG MODIFY COLUMN BookingType TINYINT NOT NULL DEFAULT 2");
            }

            string stayInfoSql = @"CREATE TABLE IF NOT EXISTS `STAY_INFO` (
                                    `StayInfoID` INT NOT NULL AUTO_INCREMENT,
                                    `DatPhongID` INT NOT NULL,
                                    `LyDoLuuTru` VARCHAR(120) NULL,
                                    `GioiTinh` VARCHAR(40) NULL,
                                    `NgaySinh` DATE NULL,
                                    `LoaiGiayTo` VARCHAR(80) NULL,
                                    `SoGiayTo` VARCHAR(60) NULL,
                                    `QuocTich` VARCHAR(80) NULL,
                                    `NoiCuTru` VARCHAR(40) NULL,
                                    `LaDiaBanCu` TINYINT(1) NOT NULL DEFAULT 0,
                                    `MaTinhMoi` VARCHAR(16) NULL,
                                    `MaXaMoi` VARCHAR(16) NULL,
                                    `MaTinhCu` VARCHAR(16) NULL,
                                    `MaHuyenCu` VARCHAR(16) NULL,
                                    `MaXaCu` VARCHAR(16) NULL,
                                    `DiaChiChiTiet` VARCHAR(255) NULL,
                                    `GiaPhong` DECIMAL(18,2) NOT NULL DEFAULT 0,
                                    `SoDemLuuTru` INT NOT NULL DEFAULT 1,
                                    `GuestListJson` LONGTEXT NULL,
                                    `CreatedAtUtc` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    `UpdatedAtUtc` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                    `CreatedBy` VARCHAR(80) NULL,
                                    `UpdatedBy` VARCHAR(80) NULL,
                                    PRIMARY KEY (`StayInfoID`),
                                    UNIQUE KEY `UX_STAY_INFO_BOOKING` (`DatPhongID`)
                                )";
            ExecuteNonQuery(conn, stayInfoSql);
            EnsureStayInfoSchemaDetails(conn);

            string extrasSql = @"CREATE TABLE IF NOT EXISTS `BOOKING_EXTRAS` (
                                    `BookingExtraID` INT NOT NULL AUTO_INCREMENT,
                                    `DatPhongID` INT NOT NULL,
                                    `ItemCode` VARCHAR(32) NOT NULL,
                                    `ItemName` VARCHAR(120) NOT NULL,
                                    `Qty` INT NOT NULL,
                                    `UnitPrice` DECIMAL(18,2) NOT NULL,
                                    `Amount` DECIMAL(18,2) NOT NULL,
                                    `Note` VARCHAR(255) NULL,
                                    `CreatedAtUtc` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    `UpdatedAtUtc` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                    `CreatedBy` VARCHAR(80) NULL,
                                    `UpdatedBy` VARCHAR(80) NULL,
                                    PRIMARY KEY (`BookingExtraID`),
                                    UNIQUE KEY `UX_BOOKING_EXTRAS_CODE` (`DatPhongID`, `ItemCode`)
                                )";
            ExecuteNonQuery(conn, extrasSql);
            EnsureBookingExtrasSchemaDetails(conn);
            EnsureBookingExtrasUniqueKey(conn);
        }

        private static void EnsureHotelSettingsSchema(MySqlConnection conn)
        {
            string settingsSql = @"CREATE TABLE IF NOT EXISTS `HOTEL_SETTINGS` (
                                    `Key` VARCHAR(120) NOT NULL,
                                    `Value` VARCHAR(255) NULL,
                                    `UpdatedAtUtc` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                    `UpdatedBy` VARCHAR(80) NULL,
                                    `DataStatus` VARCHAR(32) NULL,
                                    PRIMARY KEY (`Key`)
                                  )";
            ExecuteNonQuery(conn, settingsSql);
        }

        private static void SeedHotelSettingsDefaults(MySqlConnection conn)
        {
            UpsertSettingIfMissing(conn, "Default.Single.NightlyRate", "200000");
            UpsertSettingIfMissing(conn, "Default.Double.NightlyRate", "300000");
            UpsertSettingIfMissing(conn, "Default.Single.DailyRate", "250000");
            UpsertSettingIfMissing(conn, "Default.Double.DailyRate", "350000");

            UpsertSettingIfMissing(conn, "Hourly.Single.Hour1", "60000");
            UpsertSettingIfMissing(conn, "Hourly.Single.NextHour", "20000");
            UpsertSettingIfMissing(conn, "Hourly.Single.ThresholdMinutes", "1");

            UpsertSettingIfMissing(conn, "Hourly.Double.Hour1", "60000");
            UpsertSettingIfMissing(conn, "Hourly.Double.NextHour", "20000");
            UpsertSettingIfMissing(conn, "Hourly.Double.ThresholdMinutes", "1");

            UpsertSettingIfMissing(conn, "Overnight.CheckoutHour", "12");
            UpsertSettingIfMissing(conn, "Overnight.NightStartHour", "20");
            UpsertSettingIfMissing(conn, "Overnight.Single.GraceHours", "0");
            UpsertSettingIfMissing(conn, "Overnight.Double.GraceHours", "0");
            UpsertSettingIfMissing(conn, "Overnight.Single.LateFee", "20000");
            UpsertSettingIfMissing(conn, "Overnight.Double.LateFee", "20000");

            UpsertSettingIfMissing(conn, "Drink.Soft.UnitPrice", "20000");
            UpsertSettingIfMissing(conn, "Drink.Water.UnitPrice", "10000");

            ExecuteNonQuery(conn, @"UPDATE HOTEL_SETTINGS
                                    SET DataStatus = COALESCE(NULLIF(DataStatus, ''), 'active'),
                                        UpdatedAtUtc = COALESCE(UpdatedAtUtc, UTC_TIMESTAMP())");
        }

        private static void UpsertSettingIfMissing(MySqlConnection conn, string key, string value)
        {
            string sql = @"INSERT INTO HOTEL_SETTINGS (`Key`, `Value`, UpdatedAtUtc, UpdatedBy, DataStatus)
                           VALUES (@Key, @Value, UTC_TIMESTAMP(), 'system:seed', 'active')
                           ON DUPLICATE KEY UPDATE
                               `Value` = IF(COALESCE(DataStatus, 'active') = 'deleted', VALUES(`Value`), `Value`),
                               UpdatedAtUtc = IF(COALESCE(DataStatus, 'active') = 'deleted', UTC_TIMESTAMP(), UpdatedAtUtc),
                               UpdatedBy = IF(COALESCE(DataStatus, 'active') = 'deleted', 'system:seed', UpdatedBy),
                               DataStatus = IF(COALESCE(DataStatus, 'active') = 'deleted', 'active', DataStatus)";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.Parameters.AddWithValue("@Value", value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureRoomTypePricingSchema(MySqlConnection conn)
        {
            string loaiPhongSql = @"CREATE TABLE IF NOT EXISTS `LOAIPHONG` (
                                        `LoaiPhongID` INT NOT NULL,
                                        `TenLoaiPhong` VARCHAR(120) NULL,
                                        `DonGiaNgay` DECIMAL(18,2) NOT NULL DEFAULT 0,
                                        PRIMARY KEY (`LoaiPhongID`)
                                    )";
            ExecuteNonQuery(conn, loaiPhongSql);

            ExecuteNonQuery(conn, @"INSERT INTO LOAIPHONG (LoaiPhongID, TenLoaiPhong, DonGiaNgay)
                                    VALUES (1, 'Phòng đơn', 200000)
                                    ON DUPLICATE KEY UPDATE
                                        TenLoaiPhong = VALUES(TenLoaiPhong),
                                        DonGiaNgay = VALUES(DonGiaNgay)");

            ExecuteNonQuery(conn, @"INSERT INTO LOAIPHONG (LoaiPhongID, TenLoaiPhong, DonGiaNgay)
                                    VALUES (2, 'Phòng đôi', 300000)
                                    ON DUPLICATE KEY UPDATE
                                        TenLoaiPhong = VALUES(TenLoaiPhong),
                                        DonGiaNgay = VALUES(DonGiaNgay)");
        }

        private static void BackfillCoreAuditColumns(MySqlConnection conn)
        {
            ExecuteNonQuery(conn, @"UPDATE DATPHONG
                                    SET CreatedAtUtc = COALESCE(CreatedAtUtc, UTC_TIMESTAMP()),
                                        UpdatedAtUtc = COALESCE(UpdatedAtUtc, CreatedAtUtc, UTC_TIMESTAMP()),
                                        DataStatus = COALESCE(NULLIF(DataStatus, ''), IF(DeletedAtUtc IS NULL, 'active', 'deleted')),
                                        KenhDat = COALESCE(NULLIF(KenhDat, ''), 'TrucTiep')");

            ExecuteNonQuery(conn, @"UPDATE HOADON
                                    SET CreatedAtUtc = COALESCE(CreatedAtUtc, UTC_TIMESTAMP()),
                                        UpdatedAtUtc = COALESCE(UpdatedAtUtc, CreatedAtUtc, UTC_TIMESTAMP()),
                                        DataStatus = COALESCE(NULLIF(DataStatus, ''), IF(DeletedAtUtc IS NULL, 'active', 'deleted')),
                                        PaymentStatus = COALESCE(NULLIF(PaymentStatus, ''), IF(DaThanhToan = 1, 'paid', 'unpaid'))");

            ExecuteNonQuery(conn, @"UPDATE PHONG
                                    SET CreatedAtUtc = COALESCE(CreatedAtUtc, UTC_TIMESTAMP()),
                                        UpdatedAtUtc = COALESCE(UpdatedAtUtc, CreatedAtUtc, UTC_TIMESTAMP()),
                                        DataStatus = COALESCE(NULLIF(DataStatus, ''), IF(DeletedAtUtc IS NULL, 'active', 'deleted'))");
            ExecuteNonQuery(conn, @"UPDATE PHONG
                                    SET TrangThai = 0
                                    WHERE TrangThai IS NULL OR TrangThai NOT IN (0, 1, 2)");

            ExecuteNonQuery(conn, @"UPDATE KHACHHANG
                                    SET CreatedAtUtc = COALESCE(CreatedAtUtc, UTC_TIMESTAMP()),
                                        UpdatedAtUtc = COALESCE(UpdatedAtUtc, CreatedAtUtc, UTC_TIMESTAMP()),
                                        DataStatus = COALESCE(NULLIF(DataStatus, ''), IF(DeletedAtUtc IS NULL, 'active', 'deleted'))");

            ExecuteNonQuery(conn, @"UPDATE USERS
                                    SET CreatedAtUtc = COALESCE(CreatedAtUtc, UTC_TIMESTAMP()),
                                        UpdatedAtUtc = COALESCE(UpdatedAtUtc, CreatedAtUtc, UTC_TIMESTAMP()),
                                        DataStatus = COALESCE(NULLIF(DataStatus, ''), IF(DeletedAtUtc IS NULL, 'active', 'deleted'))");
        }

        private static void EnsureStatisticsIndexes(MySqlConnection conn)
        {
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_DATASTATUS", "CREATE INDEX IDX_DATPHONG_DATASTATUS ON DATPHONG (DataStatus)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_KENHDAT", "CREATE INDEX IDX_DATPHONG_KENHDAT ON DATPHONG (KenhDat)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_DATERANGE_TYPE_STATUS", "CREATE INDEX IDX_DATPHONG_DATERANGE_TYPE_STATUS ON DATPHONG (NgayDen, BookingType, TrangThai)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_TYPE_DATE", "CREATE INDEX IDX_DATPHONG_TYPE_DATE ON DATPHONG (BookingType, NgayDen)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_PHONG_STATUS", "CREATE INDEX IDX_DATPHONG_PHONG_STATUS ON DATPHONG (PhongID, TrangThai)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_DATERANGE_TYPE_STATUS_ID", "CREATE INDEX IDX_DATPHONG_DATERANGE_TYPE_STATUS_ID ON DATPHONG (NgayDen, BookingType, TrangThai, DatPhongID)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_TYPE_DATE_ID", "CREATE INDEX IDX_DATPHONG_TYPE_DATE_ID ON DATPHONG (BookingType, NgayDen, DatPhongID)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_PHONG_STATUS_ID", "CREATE INDEX IDX_DATPHONG_PHONG_STATUS_ID ON DATPHONG (PhongID, TrangThai, DatPhongID)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_RANGE_DATA_TYPE_STATUS_ID", "CREATE INDEX IDX_DATPHONG_RANGE_DATA_TYPE_STATUS_ID ON DATPHONG (NgayDen, DataStatus, BookingType, TrangThai, DatPhongID)");
            EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_ROOM_STATUS_DATA_ID", "CREATE INDEX IDX_DATPHONG_ROOM_STATUS_DATA_ID ON DATPHONG (PhongID, TrangThai, DataStatus, DatPhongID)");

            EnsureIndex(conn, "HOADON", "IDX_HOADON_DATASTATUS", "CREATE INDEX IDX_HOADON_DATASTATUS ON HOADON (DataStatus)");
            EnsureIndex(conn, "HOADON", "IDX_HOADON_PAYMENTSTATUS", "CREATE INDEX IDX_HOADON_PAYMENTSTATUS ON HOADON (PaymentStatus)");
            EnsureIndex(conn, "HOADON", "IDX_HOADON_BOOKING_DATE", "CREATE INDEX IDX_HOADON_BOOKING_DATE ON HOADON (DatPhongID, NgayLap)");
            EnsureIndex(conn, "HOADON", "IDX_HOADON_BOOKING_STATUS_PAY_DATE", "CREATE INDEX IDX_HOADON_BOOKING_STATUS_PAY_DATE ON HOADON (DatPhongID, DataStatus, DaThanhToan, NgayLap)");
            EnsureIndex(conn, "HOADON", "IDX_HOADON_DATE_STATUS_BOOKING", "CREATE INDEX IDX_HOADON_DATE_STATUS_BOOKING ON HOADON (NgayLap, DataStatus, DatPhongID)");

            EnsureIndex(conn, "BOOKING_EXTRAS", "IDX_BOOKING_EXTRAS_BOOKING", "CREATE INDEX IDX_BOOKING_EXTRAS_BOOKING ON BOOKING_EXTRAS (DatPhongID)");
            EnsureIndex(conn, "BOOKING_EXTRAS", "IDX_BOOKING_EXTRAS_BOOKING_AMOUNT", "CREATE INDEX IDX_BOOKING_EXTRAS_BOOKING_AMOUNT ON BOOKING_EXTRAS (DatPhongID, Amount)");

            EnsureIndex(conn, "HOTEL_SETTINGS", "IDX_HOTEL_SETTINGS_DATASTATUS", "CREATE INDEX IDX_HOTEL_SETTINGS_DATASTATUS ON HOTEL_SETTINGS (DataStatus)");

            EnsureIndex(conn, "PHONG", "IDX_PHONG_DATASTATUS", "CREATE INDEX IDX_PHONG_DATASTATUS ON PHONG (DataStatus)");
            EnsureIndex(conn, "KHACHHANG", "IDX_KHACHHANG_DATASTATUS", "CREATE INDEX IDX_KHACHHANG_DATASTATUS ON KHACHHANG (DataStatus)");
        }

        private static void EnsureBookingExtrasUniqueKey(MySqlConnection conn)
        {
            ExecuteNonQuery(conn, @"UPDATE BOOKING_EXTRAS
                                    SET ItemCode = UPPER(TRIM(ItemCode))
                                    WHERE COALESCE(ItemCode, '') <> ''");

            ExecuteNonQuery(conn, @"DELETE eOld
                                    FROM BOOKING_EXTRAS eOld
                                    JOIN BOOKING_EXTRAS eNew
                                      ON eOld.DatPhongID = eNew.DatPhongID
                                     AND UPPER(TRIM(eOld.ItemCode)) = UPPER(TRIM(eNew.ItemCode))
                                     AND eOld.BookingExtraID < eNew.BookingExtraID");

            if (IndexExists(conn, "BOOKING_EXTRAS", "UX_BOOKING_EXTRAS_CODE")
                && !IsUniqueIndex(conn, "BOOKING_EXTRAS", "UX_BOOKING_EXTRAS_CODE"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE BOOKING_EXTRAS DROP INDEX UX_BOOKING_EXTRAS_CODE");
            }

            if (!IndexExists(conn, "BOOKING_EXTRAS", "UX_BOOKING_EXTRAS_CODE"))
            {
                ExecuteNonQuery(conn, @"ALTER TABLE BOOKING_EXTRAS
                                        ADD UNIQUE KEY UX_BOOKING_EXTRAS_CODE (DatPhongID, ItemCode)");
            }
        }

        private static void EnsureStayInfoSchemaDetails(MySqlConnection conn)
        {
            EnsureColumn(conn, "STAY_INFO", "LyDoLuuTru", "VARCHAR(120) NULL");
            EnsureColumn(conn, "STAY_INFO", "GioiTinh", "VARCHAR(40) NULL");
            EnsureColumn(conn, "STAY_INFO", "NgaySinh", "DATE NULL");
            EnsureColumn(conn, "STAY_INFO", "LoaiGiayTo", "VARCHAR(80) NULL");
            EnsureColumn(conn, "STAY_INFO", "SoGiayTo", "VARCHAR(60) NULL");
            EnsureColumn(conn, "STAY_INFO", "QuocTich", "VARCHAR(80) NULL");
            EnsureColumn(conn, "STAY_INFO", "NoiCuTru", "VARCHAR(40) NULL");
            EnsureColumn(conn, "STAY_INFO", "LaDiaBanCu", "TINYINT(1) NOT NULL DEFAULT 0");
            EnsureColumn(conn, "STAY_INFO", "MaTinhMoi", "VARCHAR(16) NULL");
            EnsureColumn(conn, "STAY_INFO", "MaXaMoi", "VARCHAR(16) NULL");
            EnsureColumn(conn, "STAY_INFO", "MaTinhCu", "VARCHAR(16) NULL");
            EnsureColumn(conn, "STAY_INFO", "MaHuyenCu", "VARCHAR(16) NULL");
            EnsureColumn(conn, "STAY_INFO", "MaXaCu", "VARCHAR(16) NULL");
            EnsureColumn(conn, "STAY_INFO", "DiaChiChiTiet", "VARCHAR(255) NULL");
            EnsureColumn(conn, "STAY_INFO", "GiaPhong", "DECIMAL(18,2) NOT NULL DEFAULT 0");
            EnsureColumn(conn, "STAY_INFO", "SoDemLuuTru", "INT NOT NULL DEFAULT 1");
            EnsureColumn(conn, "STAY_INFO", "GuestListJson", "LONGTEXT NULL");
            EnsureColumn(conn, "STAY_INFO", "CreatedAtUtc", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
            EnsureColumn(conn, "STAY_INFO", "UpdatedAtUtc", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            EnsureColumn(conn, "STAY_INFO", "CreatedBy", "VARCHAR(80) NULL");
            EnsureColumn(conn, "STAY_INFO", "UpdatedBy", "VARCHAR(80) NULL");

            ExecuteNonQuery(conn, @"ALTER TABLE STAY_INFO MODIFY COLUMN GiaPhong DECIMAL(18,2) NOT NULL DEFAULT 0");
            ExecuteNonQuery(conn, @"ALTER TABLE STAY_INFO MODIFY COLUMN SoDemLuuTru INT NOT NULL DEFAULT 1");
            ExecuteNonQuery(conn, @"ALTER TABLE STAY_INFO MODIFY COLUMN LaDiaBanCu TINYINT(1) NOT NULL DEFAULT 0");

            ExecuteNonQuery(conn, @"DELETE sOld
                                    FROM STAY_INFO sOld
                                    JOIN STAY_INFO sNew
                                      ON sOld.DatPhongID = sNew.DatPhongID
                                     AND sOld.StayInfoID < sNew.StayInfoID");

            if (IndexExists(conn, "STAY_INFO", "UX_STAY_INFO_BOOKING")
                && !IsUniqueIndex(conn, "STAY_INFO", "UX_STAY_INFO_BOOKING"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE STAY_INFO DROP INDEX UX_STAY_INFO_BOOKING");
            }

            if (!IndexExists(conn, "STAY_INFO", "UX_STAY_INFO_BOOKING"))
            {
                ExecuteNonQuery(conn, @"ALTER TABLE STAY_INFO
                                        ADD UNIQUE KEY UX_STAY_INFO_BOOKING (DatPhongID)");
            }
        }

        private static void EnsureBookingExtrasSchemaDetails(MySqlConnection conn)
        {
            EnsureColumn(conn, "BOOKING_EXTRAS", "ItemCode", "VARCHAR(32) NOT NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "ItemName", "VARCHAR(120) NOT NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "Qty", "INT NOT NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "UnitPrice", "DECIMAL(18,2) NOT NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "Amount", "DECIMAL(18,2) NOT NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "Note", "VARCHAR(255) NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "CreatedAtUtc", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
            EnsureColumn(conn, "BOOKING_EXTRAS", "UpdatedAtUtc", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            EnsureColumn(conn, "BOOKING_EXTRAS", "CreatedBy", "VARCHAR(80) NULL");
            EnsureColumn(conn, "BOOKING_EXTRAS", "UpdatedBy", "VARCHAR(80) NULL");

            ExecuteNonQuery(conn, @"ALTER TABLE BOOKING_EXTRAS MODIFY COLUMN ItemCode VARCHAR(32) NOT NULL");
            ExecuteNonQuery(conn, @"ALTER TABLE BOOKING_EXTRAS MODIFY COLUMN ItemName VARCHAR(120) NOT NULL");
            ExecuteNonQuery(conn, @"ALTER TABLE BOOKING_EXTRAS MODIFY COLUMN Qty INT NOT NULL");
            ExecuteNonQuery(conn, @"ALTER TABLE BOOKING_EXTRAS MODIFY COLUMN UnitPrice DECIMAL(18,2) NOT NULL");
            ExecuteNonQuery(conn, @"ALTER TABLE BOOKING_EXTRAS MODIFY COLUMN Amount DECIMAL(18,2) NOT NULL");
        }

        private static void EnsureColumn(MySqlConnection conn, string tableName, string columnName, string definitionSql)
        {
            if (ColumnExists(conn, tableName, columnName)) return;
            ExecuteNonQuery(conn, "ALTER TABLE `" + tableName + "` ADD COLUMN `" + columnName + "` " + definitionSql);
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

        private static void EnsureIndex(MySqlConnection conn, string tableName, string indexName, string createSql)
        {
            if (IndexExists(conn, tableName, indexName))
                return;

            using (var createCmd = new MySqlCommand(createSql, conn))
            {
                createCmd.ExecuteNonQuery();
            }
        }

        private static bool IndexExists(MySqlConnection conn, string tableName, string indexName)
        {
            string checkSql = @"SELECT COUNT(1)
                                FROM INFORMATION_SCHEMA.STATISTICS
                                WHERE TABLE_SCHEMA = DATABASE()
                                  AND TABLE_NAME = @TableName
                                  AND INDEX_NAME = @IndexName";

            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@TableName", tableName);
                checkCmd.Parameters.AddWithValue("@IndexName", indexName);
                return Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            }
        }

        private static bool IsUniqueIndex(MySqlConnection conn, string tableName, string indexName)
        {
            string sql = @"SELECT MIN(NON_UNIQUE)
                           FROM INFORMATION_SCHEMA.STATISTICS
                           WHERE TABLE_SCHEMA = DATABASE()
                             AND TABLE_NAME = @TableName
                             AND INDEX_NAME = @IndexName";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@IndexName", indexName);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value) return false;
                return Convert.ToInt32(value) == 0;
            }
        }

        private static void ExecuteNonQuery(MySqlConnection conn, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
