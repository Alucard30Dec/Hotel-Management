namespace HotelManagement.Models
{
    public enum BookingStatus
    {
        DatTruoc = 0,
        DangO = 1,
        DaTra = 2,
        DaHuy = 3,
        NoShow = 4
    }

    public enum PaymentStatus
    {
        ChuaThanhToan = 0,
        DaThanhToan = 1,
        ThanhToanMotPhan = 2,
        DaHoanTien = 3
    }

    public enum BookingType
    {
        Hourly = 1,
        Overnight = 2
    }

    public enum RoomStatus
    {
        Trong = 0,
        CoKhach = 1,
        ChuaDon = 2
    }

    public enum RentalType
    {
        Overnight = 1,
        Hourly = 3
    }
}
