using System.Runtime.Serialization;

namespace HotelManagement.Models
{
    [DataContract]
    public class Xa
    {
        [DataMember]
        public string MaXa { get; set; }

        [DataMember]
        public string TenXa { get; set; }

        [DataMember]
        public bool IsActive { get; set; } = true;

        [DataMember]
        public string GhiChu { get; set; }
    }
}
