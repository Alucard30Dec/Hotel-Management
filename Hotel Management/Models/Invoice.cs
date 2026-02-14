using System;
using System.ComponentModel.DataAnnotations; // Cần thêm dòng này
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManagement.Models
{
    public class Invoice
    {
        [Key] // Đánh dấu thuộc tính này là khóa chính
        public int HoaDonID { get; set; }
        public int DatPhongID { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public bool DaThanhToan { get; set; }
    }
}