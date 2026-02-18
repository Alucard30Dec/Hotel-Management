using System;

namespace HotelManagement.Services
{
    public sealed class CccdInfo
    {
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string DocumentNumber { get; set; }
        public string Nationality { get; set; }
        public string AddressRaw { get; set; }
        public string Province { get; set; }
        public string Ward { get; set; }
        public string AddressDetail { get; set; }
        public string RawQr { get; set; }
        public string RawOcrText { get; set; }
    }
}
