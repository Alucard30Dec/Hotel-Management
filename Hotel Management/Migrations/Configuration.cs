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
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(HotelManagement.Data.HotelDbContext context)
        {
            SeedUsers(context);
            SeedRooms(context);
            SeedSampleCustomers(context);
            SeedSampleBookingsAndInvoices(context);
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
            var roomsToAdd = new List<Room>();

            for (int i = 1; i <= 5; i++)
            {
                string ma = "10" + i;
                if (!context.Rooms.Any(r => r.MaPhong == ma))
                    roomsToAdd.Add(new Room { MaPhong = ma, LoaiPhongID = 1, Tang = 1, TrangThai = 0, GhiChu = "" });
            }

            for (int i = 1; i <= 5; i++)
            {
                string ma = "20" + i;
                if (!context.Rooms.Any(r => r.MaPhong == ma))
                    roomsToAdd.Add(new Room { MaPhong = ma, LoaiPhongID = 2, Tang = 2, TrangThai = 0, GhiChu = "" });
            }

            for (int i = 1; i <= 5; i++)
            {
                string ma = "30" + i;
                if (!context.Rooms.Any(r => r.MaPhong == ma))
                    roomsToAdd.Add(new Room { MaPhong = ma, LoaiPhongID = 2, Tang = 3, TrangThai = 0, GhiChu = "" });
            }

            if (roomsToAdd.Count == 0) return;
            context.Rooms.AddRange(roomsToAdd);
            context.SaveChanges();
        }

        private static void SeedSampleCustomers(HotelManagement.Data.HotelDbContext context)
        {
            var samples = new[]
            {
                new Customer { HoTen = "Nguyen Van An", CCCD = "990000000001", DienThoai = "0900000001", DiaChi = "Quan 1, TP.HCM" },
                new Customer { HoTen = "Tran Thi Bich", CCCD = "990000000002", DienThoai = "0900000002", DiaChi = "Quan 3, TP.HCM" },
                new Customer { HoTen = "Le Minh Chau", CCCD = "990000000003", DienThoai = "0900000003", DiaChi = "Hai Chau, Da Nang" },
                new Customer { HoTen = "Pham Quoc Dung", CCCD = "990000000004", DienThoai = "0900000004", DiaChi = "Ninh Kieu, Can Tho" },
                new Customer { HoTen = "Doan Thi Ha", CCCD = "990000000005", DienThoai = "0900000005", DiaChi = "Ba Dinh, Ha Noi" },
                new Customer { HoTen = "Vo Thanh Huy", CCCD = "990000000006", DienThoai = "0900000006", DiaChi = "Go Vap, TP.HCM" },
                new Customer { HoTen = "Bui Ngoc Kha", CCCD = "990000000007", DienThoai = "0900000007", DiaChi = "Bien Hoa, Dong Nai" },
                new Customer { HoTen = "Dang Gia Linh", CCCD = "990000000008", DienThoai = "0900000008", DiaChi = "Da Lat, Lam Dong" },
                new Customer { HoTen = "Hoang Anh Minh", CCCD = "990000000009", DienThoai = "0900000009", DiaChi = "Thu Duc, TP.HCM" },
                new Customer { HoTen = "Nguyen Thu Nhi", CCCD = "990000000010", DienThoai = "0900000010", DiaChi = "Quy Nhon, Binh Dinh" }
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

                    decimal firstHour = room.LoaiPhongID == 1 ? 70000m : 120000m;
                    decimal nextHour = room.LoaiPhongID == 1 ? 20000m : 30000m;
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
