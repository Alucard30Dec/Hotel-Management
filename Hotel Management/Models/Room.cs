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

        // NEW: kiểu thuê & tên khách hiển thị
        // 1 = Đêm, 2 = Ngày, 3 = Giờ, null = chưa xác định
        public int? KieuThue { get; set; }
        public string TenKhachHienThi { get; set; }
    }
}
