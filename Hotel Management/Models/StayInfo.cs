using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class StayInfo
    {
        [Key]
        public int StayInfoID { get; set; }
        public int DatPhongID { get; set; }

        public string LyDoLuuTru { get; set; }
        public string GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string LoaiGiayTo { get; set; }
        public string SoGiayTo { get; set; }
        public string QuocTich { get; set; }
        public string NoiCuTru { get; set; }
        public bool LaDiaBanCu { get; set; }

        public string MaTinhMoi { get; set; }
        public string MaXaMoi { get; set; }
        public string MaTinhCu { get; set; }
        public string MaHuyenCu { get; set; }
        public string MaXaCu { get; set; }

        public string DiaChiChiTiet { get; set; }
        public decimal GiaPhong { get; set; }
        public int SoDemLuuTru { get; set; }
        public string GuestListJson { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
