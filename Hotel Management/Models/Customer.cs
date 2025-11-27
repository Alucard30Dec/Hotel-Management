using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManagement.Models
{
    public class Customer
    {
        public int KhachHangID { get; set; }
        public string HoTen { get; set; }
        public string CCCD { get; set; }
        public byte[] HinhCCCD { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
    }
}
