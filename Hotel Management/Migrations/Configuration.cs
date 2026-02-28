namespace Hotel_Management.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using HotelManagement.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<HotelManagement.Data.HotelDbContext>
    {
        public Configuration()
        {
            // Allow EF6 to apply additive schema changes at startup
            // (new tables/columns) when code model moves ahead of explicit migrations.
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(HotelManagement.Data.HotelDbContext context)
        {
            SeedUsers(context);
            SeedRooms(context);
            SeedSampleCustomers(context);
            // Booking sample data should be created manually from UI seed tools when needed.
            // Keep default Code First startup clean.
        }

        private static void SeedUsers(HotelManagement.Data.HotelDbContext context)
        {
            if (!context.Users.Any(u => u.Username == "admin"))
                context.Users.Add(new User { Username = "admin", Password = "123", Role = "Admin" });

            if (!context.Users.Any(u => u.Username == "letan"))
                context.Users.Add(new User { Username = "letan", Password = "123", Role = "Letan" });

            context.SaveChanges();
        }

        private static void SeedRooms(HotelManagement.Data.HotelDbContext context)
        {
            var targetRooms = new[]
            {
                new Room { MaPhong = "01",  LoaiPhongID = 1, Tang = 0 },
                new Room { MaPhong = "02",  LoaiPhongID = 1, Tang = 0 },
                new Room { MaPhong = "03",  LoaiPhongID = 1, Tang = 0 },
                new Room { MaPhong = "04",  LoaiPhongID = 2, Tang = 0 },
                new Room { MaPhong = "101", LoaiPhongID = 1, Tang = 1 },
                new Room { MaPhong = "102", LoaiPhongID = 1, Tang = 1 },
                new Room { MaPhong = "103", LoaiPhongID = 1, Tang = 1 },
                new Room { MaPhong = "104", LoaiPhongID = 1, Tang = 1 },
                new Room { MaPhong = "105", LoaiPhongID = 1, Tang = 1 },
                new Room { MaPhong = "201", LoaiPhongID = 2, Tang = 2 },
                new Room { MaPhong = "202", LoaiPhongID = 1, Tang = 2 }
            };

            var existing = context.Rooms.ToList();

            foreach (var target in targetRooms)
            {
                var room = existing.FirstOrDefault(r => string.Equals(r.MaPhong, target.MaPhong, StringComparison.OrdinalIgnoreCase));
                if (room == null)
                {
                    context.Rooms.Add(new Room
                    {
                        MaPhong = target.MaPhong,
                        LoaiPhongID = target.LoaiPhongID,
                        Tang = target.Tang,
                        TrangThai = 0
                    });
                    continue;
                }

                room.LoaiPhongID = target.LoaiPhongID;
                room.Tang = target.Tang;
            }

            context.SaveChanges();
        }

        private static void SeedSampleCustomers(HotelManagement.Data.HotelDbContext context)
        {
            var samples = new[]
            {
                new Customer { HoTen = "Khách mẫu 01", CCCD = "990000000001", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 02", CCCD = "990000000002", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 03", CCCD = "990000000003", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 04", CCCD = "990000000004", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 05", CCCD = "990000000005", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 06", CCCD = "990000000006", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 07", CCCD = "990000000007", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 08", CCCD = "990000000008", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 09", CCCD = "990000000009", DienThoai = "", DiaChi = "" },
                new Customer { HoTen = "Khách mẫu 10", CCCD = "990000000010", DienThoai = "", DiaChi = "" }
            };

            foreach (var sample in samples)
            {
                if (context.Customers.Any(c => c.CCCD == sample.CCCD)) continue;
                context.Customers.Add(sample);
            }

            context.SaveChanges();
        }

        private static void SeedSampleBookingsAndInvoices(HotelManagement.Data.HotelDbContext context)
        {
            var sampleCustomerIds = context.Customers
                .Where(c => c.CCCD != null && c.CCCD.StartsWith("9900000000"))
                .Select(c => c.KhachHangID)
                .ToList();

            if (!sampleCustomerIds.Any()) return;

            bool hasSampleBookings = context.Bookings.Any(b => sampleCustomerIds.Contains(b.KhachHangID));
            if (hasSampleBookings) return;

            var rooms = context.Rooms.OrderBy(r => r.PhongID).ToList();
            if (!rooms.Any()) return;

            var customers = context.Customers
                .Where(c => sampleCustomerIds.Contains(c.KhachHangID))
                .OrderBy(c => c.KhachHangID)
                .ToList();
            if (!customers.Any()) return;

            var random = new Random(20260218);
            var today = DateTime.Today;
            var startDate = today.AddDays(-20);
            var endDate = today.AddDays(2);

            int roomIndex = 0;
            int customerIndex = 0;

            for (DateTime day = startDate; day <= endDate; day = day.AddDays(1))
            {
                int bookingsPerDay = day.Date == today ? 3 : 2;
                for (int i = 0; i < bookingsPerDay; i++)
                {
                    var room = rooms[roomIndex % rooms.Count];
                    var customer = customers[customerIndex % customers.Count];
                    roomIndex++;
                    customerIndex++;

                    DateTime checkin = day.AddHours(11 + (i % 7));
                    DateTime checkoutPlan = checkin.AddDays(1);

                    int status;
                    DateTime? checkoutReal = null;
                    if (day.Date > today)
                    {
                        status = 0;
                    }
                    else if (day.Date >= today.AddDays(-1))
                    {
                        status = 1;
                    }
                    else
                    {
                        status = 2;
                        checkoutReal = checkoutPlan.AddHours(random.Next(0, 4));
                    }

                    decimal deposit = room.LoaiPhongID == 1 ? 200000m : 300000m;

                    var booking = new Booking
                    {
                        KhachHangID = customer.KhachHangID,
                        PhongID = room.PhongID,
                        NgayDen = checkin,
                        NgayDiDuKien = checkoutPlan,
                        NgayDiThucTe = checkoutReal,
                        TrangThai = status,
                        TienCoc = deposit
                    };
                    context.Bookings.Add(booking);
                    context.SaveChanges();

                    bool shouldCreateInvoice = status == 2 || (status == 1 && (i % 2 == 0));
                    if (!shouldCreateInvoice) continue;

                    int stayedHours = status == 2
                        ? Math.Max(1, (int)Math.Ceiling((checkoutReal.Value - checkin).TotalHours))
                        : Math.Max(1, (int)Math.Ceiling((DateTime.Now - checkin).TotalHours));

                    decimal firstHour = room.LoaiPhongID == 1 ? 60000m : 60000m;
                    decimal nextHour = room.LoaiPhongID == 1 ? 20000m : 20000m;
                    decimal roomCharge = stayedHours <= 1 ? firstHour : firstHour + (stayedHours - 1) * nextHour;
                    decimal drinkCharge = random.Next(0, 4) * 10000m;
                    decimal total = roomCharge + drinkCharge;
                    bool paid = status == 2;

                    DateTime invoiceDate = status == 2
                        ? checkoutReal.Value
                        : DateTime.Now.AddMinutes(-(i * 11 + roomIndex));
                    if (invoiceDate.Date < startDate) invoiceDate = startDate.AddHours(14);

                    context.Invoices.Add(new Invoice
                    {
                        DatPhongID = booking.DatPhongID,
                        NgayLap = invoiceDate,
                        TongTien = total,
                        DaThanhToan = paid
                    });
                }
            }

            context.SaveChanges();
        }
    }
}
