using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class Room
    {
        [Key]
        public int PhongID { get; set; }
        public string MaPhong { get; set; }
        public int LoaiPhongID { get; set; }
        public int Tang { get; set; }
        public int TrangThai { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public int? KieuThue { get; set; }
        public string TenKhachHienThi { get; set; }
    }
}
