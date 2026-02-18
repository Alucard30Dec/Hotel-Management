using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HotelManagement.Models
{
    [DataContract]
    public class Tinh
    {
        [DataMember]
        public string MaTinh { get; set; }

        [DataMember]
        public string TenTinh { get; set; }

        [DataMember]
        public bool IsActive { get; set; } = true;

        [DataMember]
        public string GhiChu { get; set; }

        [DataMember]
        public List<Huyen> Huyens { get; set; } = new List<Huyen>();
    }
}
