-- Reset all booking-related data and room runtime status.
-- Scope:
--   - Delete all bookings, invoices, stay info, extras
--   - Reset all rooms to empty state
--   - Reset auto increment counters
--
-- Run on MySQL/TiDB using the same HotelDb connection.

SET FOREIGN_KEY_CHECKS = 0;

START TRANSACTION;

DELETE FROM HOADON;
DELETE FROM BOOKING_EXTRAS;
DELETE FROM STAY_INFO;
DELETE FROM DATPHONG;

UPDATE PHONG
SET TrangThai = 0,
    KieuThue = NULL,
    ThoiGianBatDau = NULL,
    TenKhachHienThi = NULL,
    UpdatedAtUtc = UTC_TIMESTAMP(),
    UpdatedBy = 'manual-reset-booking',
    DataStatus = COALESCE(NULLIF(DataStatus, ''), 'active');

COMMIT;

ALTER TABLE HOADON AUTO_INCREMENT = 1;
ALTER TABLE BOOKING_EXTRAS AUTO_INCREMENT = 1;
ALTER TABLE STAY_INFO AUTO_INCREMENT = 1;
ALTER TABLE DATPHONG AUTO_INCREMENT = 1;

SET FOREIGN_KEY_CHECKS = 1;

-- Verify result
SELECT
    (SELECT COUNT(1) FROM DATPHONG) AS BookingCount,
    (SELECT COUNT(1) FROM HOADON) AS InvoiceCount,
    (SELECT COUNT(1) FROM STAY_INFO) AS StayInfoCount,
    (SELECT COUNT(1) FROM BOOKING_EXTRAS) AS ExtrasCount,
    (SELECT COUNT(1)
     FROM PHONG
     WHERE TrangThai <> 0
        OR KieuThue IS NOT NULL
        OR ThoiGianBatDau IS NOT NULL
        OR COALESCE(TenKhachHienThi, '') <> '') AS DirtyRoomCount;
