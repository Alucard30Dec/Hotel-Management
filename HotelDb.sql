/* =====================================================
   0. ĐẢM BẢO DATABASE TỒN TẠI & KHÔNG SINGLE_USER
   ===================================================== */
IF DB_ID('HotelDb') IS NULL
BEGIN
    CREATE DATABASE HotelDb;
END
GO

IF DB_ID('HotelDb') IS NOT NULL
BEGIN
    ALTER DATABASE HotelDb 
        SET MULTI_USER WITH ROLLBACK IMMEDIATE;
END
GO

/* =====================================================
   1. SỬ DỤNG DATABASE HotelDb
   ===================================================== */
USE HotelDb;
GO

/* =====================================================
   2. XÓA CÁC BẢNG CŨ (NẾU TỒN TẠI)
      - Xóa bảng con trước, bảng cha sau
   ===================================================== */
IF OBJECT_ID('dbo.CHITIETDICHVU', 'U') IS NOT NULL DROP TABLE dbo.CHITIETDICHVU;
IF OBJECT_ID('dbo.HOADON',        'U') IS NOT NULL DROP TABLE dbo.HOADON;
IF OBJECT_ID('dbo.DATPHONG',      'U') IS NOT NULL DROP TABLE dbo.DATPHONG;
IF OBJECT_ID('dbo.KHACHHANG',     'U') IS NOT NULL DROP TABLE dbo.KHACHHANG;
IF OBJECT_ID('dbo.PHONG',         'U') IS NOT NULL DROP TABLE dbo.PHONG;
IF OBJECT_ID('dbo.DICHVU',        'U') IS NOT NULL DROP TABLE dbo.DICHVU;
IF OBJECT_ID('dbo.LOAIPHONG',     'U') IS NOT NULL DROP TABLE dbo.LOAIPHONG;
IF OBJECT_ID('dbo.USERS',         'U') IS NOT NULL DROP TABLE dbo.USERS;
GO

/* =====================================================
   3. TẠO LẠI CÁC BẢNG
   ===================================================== */

-- BẢNG LOAIPHONG
CREATE TABLE LOAIPHONG (
    LoaiPhongID INT IDENTITY(1,1) PRIMARY KEY,
    TenLoai      NVARCHAR(50)  NOT NULL,
    DonGiaNgay   DECIMAL(18,0) NOT NULL,  -- Giá base theo ngày/đêm (250k / 350k)
    DonGiaGio    DECIMAL(18,0) NOT NULL,  -- Giá giờ đầu (70k / 120k)
    GhiChu       NVARCHAR(200) NULL
);
GO

-- BẢNG PHONG
CREATE TABLE PHONG (
    PhongID         INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong         NVARCHAR(20) NOT NULL UNIQUE,
    LoaiPhongID     INT NOT NULL,
    Tang            INT NOT NULL,
    TrangThai       INT NOT NULL DEFAULT 0,      -- 0=Trống,1=Có khách,2=Chưa dọn,3=Đã có khách đặt
    GhiChu          NVARCHAR(200) NULL,
    ThoiGianBatDau  DATETIME NULL,              -- dùng cho tính giờ / đêm

    -- CỘT PHỤ ĐỂ KHỚP VỚI CODE C#
    -- 1 = Đêm, 3 = Giờ, (2 = Ngày - hiện không dùng), NULL = chưa xác định
    KieuThue        INT NULL,
    TenKhachHienThi NVARCHAR(100) NULL,

    CONSTRAINT FK_PHONG_LOAIPHONG 
        FOREIGN KEY (LoaiPhongID) REFERENCES LOAIPHONG(LoaiPhongID)
);
GO

-- BẢNG KHACHHANG (để dành nếu sau dùng đặt phòng/hoá đơn)
CREATE TABLE KHACHHANG (
    KhachHangID INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    CCCD        NVARCHAR(20)  NOT NULL,
    HinhCCCD    VARBINARY(MAX) NULL,
    DienThoai   NVARCHAR(20)  NULL,
    DiaChi      NVARCHAR(200) NULL
);
GO

-- BẢNG DATPHONG (booking / hoá đơn, code hiện tại ít dùng nhưng để sẵn)
CREATE TABLE DATPHONG (
    DatPhongID     INT IDENTITY(1,1) PRIMARY KEY,
    KhachHangID    INT NOT NULL,
    PhongID        INT NOT NULL,
    NgayDen        DATETIME NOT NULL,
    NgayDiDuKien   DATETIME NOT NULL,
    NgayDiThucTe   DATETIME NULL,
    TrangThai      INT NOT NULL DEFAULT 0,  -- 0=Đặt trước,1=Đang ở,2=Đã trả
    TienCoc        DECIMAL(18,0) NOT NULL DEFAULT 0,
    CONSTRAINT FK_DATPHONG_KHACHHANG 
        FOREIGN KEY (KhachHangID) REFERENCES KHACHHANG(KhachHangID),
    CONSTRAINT FK_DATPHONG_PHONG 
        FOREIGN KEY (PhongID) REFERENCES PHONG(PhongID)
);
GO

-- BẢNG DICHVU
CREATE TABLE DICHVU (
    DichVuID INT IDENTITY(1,1) PRIMARY KEY,
    TenDichVu NVARCHAR(100) NOT NULL,
    DonGia    DECIMAL(18,0) NOT NULL
);
GO

-- BẢNG CHITIETDICHVU
CREATE TABLE CHITIETDICHVU (
    ChiTietDichVuID INT IDENTITY(1,1) PRIMARY KEY,
    DatPhongID      INT NOT NULL,
    DichVuID        INT NOT NULL,
    SoLuong         INT NOT NULL,
    CONSTRAINT FK_CTDV_DATPHONG 
        FOREIGN KEY (DatPhongID) REFERENCES DATPHONG(DatPhongID),
    CONSTRAINT FK_CTDV_DICHVU 
        FOREIGN KEY (DichVuID) REFERENCES DICHVU(DichVuID)
);
GO

-- BẢNG HOADON
CREATE TABLE HOADON (
    HoaDonID     INT IDENTITY(1,1) PRIMARY KEY,
    DatPhongID   INT NOT NULL,
    NgayLap      DATETIME NOT NULL DEFAULT GETDATE(),
    TongTien     DECIMAL(18,0) NOT NULL,
    DaThanhToan  BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_HOADON_DATPHONG 
        FOREIGN KEY (DatPhongID) REFERENCES DATPHONG(DatPhongID)
);
GO

-- BẢNG USERS
CREATE TABLE USERS (
    UserID   INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50)  NOT NULL UNIQUE,
    [Password] NVARCHAR(200) NOT NULL,
    [Role]   NVARCHAR(20)  NOT NULL         -- Admin / Letan
);
GO

/* =====================================================
   4. DỮ LIỆU MẪU
   ===================================================== */

-- Loại phòng: ÁP ĐÚNG QUY TẮC GIÁ BẠN YÊU CẦU
-- Phòng đơn:
--   1 đêm sau 18h: 200.000
--   1 đêm trước 18h: 250.000
--   Nhiều đêm: 250.000/đêm
--   Giờ đầu: 70.000, giờ sau: 20.000
-- Phòng đôi:
--   1 đêm sau 18h: 300.000
--   1 đêm trước 18h: 350.000
--   Nhiều đêm: 350.000/đêm
--   Giờ đầu: 120.000, giờ sau: 30.000
INSERT INTO LOAIPHONG (TenLoai, DonGiaNgay, DonGiaGio, GhiChu)
VALUES (N'Phòng đơn', 250000,  70000,  N'1 đêm sau 18h: 200k; 1 đêm trước 18h: 250k; nhiều đêm: 250k; giờ đầu: 70k; giờ sau: 20k'),
       (N'Phòng đôi', 350000, 120000, N'1 đêm sau 18h: 300k; 1 đêm trước 18h: 350k; nhiều đêm: 350k; giờ đầu: 120k; giờ sau: 30k');
GO

-- Phòng các tầng (KieuThue, TenKhachHienThi để NULL – chỉ set khi có khách)
INSERT INTO PHONG (MaPhong, LoaiPhongID, Tang, TrangThai, GhiChu, ThoiGianBatDau, KieuThue, TenKhachHienThi)
VALUES (N'101', 1, 1, 0, NULL, NULL, NULL, NULL),
       (N'102', 1, 1, 0, NULL, NULL, NULL, NULL),
       (N'103', 2, 1, 0, NULL, NULL, NULL, NULL),

       (N'201', 1, 2, 0, NULL, NULL, NULL, NULL),
       (N'202', 1, 2, 0, NULL, NULL, NULL, NULL),
       (N'203', 2, 2, 0, NULL, NULL, NULL, NULL),

       (N'301', 1, 3, 0, NULL, NULL, NULL, NULL),
       (N'302', 1, 3, 0, NULL, NULL, NULL, NULL),
       (N'303', 2, 3, 0, NULL, NULL, NULL, NULL);
GO

-- Users demo
INSERT INTO USERS (Username, [Password], [Role])
VALUES ('admin', '123456', 'Admin'),
       ('letan',  '123456', 'Letan');
GO

-- Dịch vụ (khớp với form chi tiết phòng: nước ngọt / nước suối)
INSERT INTO DICHVU (TenDichVu, DonGia)
VALUES (N'Nước ngọt', 20000),
       (N'Nước suối', 10000),
       (N'Giặt là',    30000);
GO
