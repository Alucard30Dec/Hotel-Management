using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class HotelSetting
    {
        [Key]
        [StringLength(120)]
        public string Key { get; set; }

        [StringLength(255)]
        public string Value { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        [StringLength(80)]
        public string UpdatedBy { get; set; }

        [StringLength(32)]
        public string DataStatus { get; set; }
    }
}
