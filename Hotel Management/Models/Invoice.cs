using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManagement.Models
{
    public class Invoice
    {
        public int HoaDonID { get; set; }
        public int DatPhongID { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public bool DaThanhToan { get; set; }
    }
}
