namespace Hotel_Management.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DATPHONG",
                c => new
                    {
                        DatPhongID = c.Int(nullable: false, identity: true),
                        KhachHangID = c.Int(nullable: false),
                        PhongID = c.Int(nullable: false),
                        NgayDen = c.DateTime(nullable: false, precision: 0),
                        NgayDiDuKien = c.DateTime(nullable: false, precision: 0),
                        NgayDiThucTe = c.DateTime(precision: 0),
                        TrangThai = c.Int(nullable: false),
                        TienCoc = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.DatPhongID);
            
            CreateTable(
                "dbo.KHACHHANG",
                c => new
                    {
                        KhachHangID = c.Int(nullable: false, identity: true),
                        HoTen = c.String(unicode: false),
                        CCCD = c.String(unicode: false),
                        HinhCCCD = c.Binary(),
                        DienThoai = c.String(unicode: false),
                        DiaChi = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.KhachHangID);
            
            CreateTable(
                "dbo.HOADON",
                c => new
                    {
                        HoaDonID = c.Int(nullable: false, identity: true),
                        DatPhongID = c.Int(nullable: false),
                        NgayLap = c.DateTime(nullable: false, precision: 0),
                        TongTien = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DaThanhToan = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.HoaDonID);
            
            CreateTable(
                "dbo.PHONG",
                c => new
                    {
                        PhongID = c.Int(nullable: false, identity: true),
                        MaPhong = c.String(unicode: false),
                        LoaiPhongID = c.Int(nullable: false),
                        Tang = c.Int(nullable: false),
                        TrangThai = c.Int(nullable: false),
                        GhiChu = c.String(unicode: false),
                        ThoiGianBatDau = c.DateTime(precision: 0),
                        KieuThue = c.Int(),
                        TenKhachHienThi = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.PhongID);
            
            CreateTable(
                "dbo.USERS",
                c => new
                    {
                        UserID = c.Int(nullable: false, identity: true),
                        Username = c.String(unicode: false),
                        Password = c.String(unicode: false),
                        Role = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.UserID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.USERS");
            DropTable("dbo.PHONG");
            DropTable("dbo.HOADON");
            DropTable("dbo.KHACHHANG");
            DropTable("dbo.DATPHONG");
        }
    }
}
