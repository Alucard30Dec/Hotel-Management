using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HotelManagement.Models
{
    [DataContract]
    public class Huyen
    {
        [DataMember]
        public string MaHuyen { get; set; }

        [DataMember]
        public string TenHuyen { get; set; }

        [DataMember]
        public bool IsActive { get; set; } = true;

        [DataMember]
        public string GhiChu { get; set; }

        [DataMember]
        public List<Xa> Xas { get; set; } = new List<Xa>();
    }
}
