using System;
using System.ComponentModel.DataAnnotations; // Cần thêm dòng này
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManagement.Models
{
    public class Booking
    {
        [Key] // Đánh dấu thuộc tính này là khóa chính
        public int DatPhongID { get; set; }
        public int KhachHangID { get; set; }
        public int PhongID { get; set; }
        public DateTime NgayDen { get; set; }
        public DateTime NgayDiDuKien { get; set; }
        public DateTime? NgayDiThucTe { get; set; }
        public int TrangThai { get; set; }
        public int BookingType { get; set; }
        public decimal TienCoc { get; set; }
    }
}
