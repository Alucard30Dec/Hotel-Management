using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManagement.Models
{
    // Lớp đại diện cho phòng (bản ghi bảng PHONG)
    public class Room
    {
        public int PhongID { get; set; }
        public string MaPhong { get; set; }
        public int LoaiPhongID { get; set; }
        public int TrangThai { get; set; }
        public string GhiChu { get; set; }
    }
}
