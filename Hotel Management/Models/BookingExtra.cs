using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class BookingExtra
    {
        [Key]
        public int BookingExtraID { get; set; }
        public int DatPhongID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
