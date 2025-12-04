using System;

namespace HotelManagement.Models
{
    public class Room
    {
        public int PhongID { get; set; }
        public string MaPhong { get; set; }
        public int LoaiPhongID { get; set; }
        public int Tang { get; set; }
        public int TrangThai { get; set; }
        public string GhiChu { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }

        // Kiểu thuê & tên khách hiển thị
        // 1 = Đêm, 3 = Giờ, (2 để dành/không dùng), null = chưa xác định
        public int? KieuThue { get; set; }
        public string TenKhachHienThi { get; set; }
    }
}
