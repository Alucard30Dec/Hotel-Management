namespace Hotel_Management.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using HotelManagement.Models;
    using System.Collections.Generic;

    internal sealed class Configuration : DbMigrationsConfiguration<HotelManagement.Data.HotelDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(HotelManagement.Data.HotelDbContext context)
        {
            // --- CÁCH KHẮC PHỤC: DÙNG LOGIC INSERT THUẦN TÚY THAY VÌ AddOrUpdate ---
            // TiDB/MySQL đôi khi gặp vấn đề với lệnh MERGE của EF6. 
            // Ta sẽ kiểm tra tồn tại bằng .Any(), nếu chưa có thì .Add() -> SaveChanges()

            // 1. Seed Users
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                context.Users.Add(new User { Username = "admin", Password = "123", Role = "Admin" });
            }

            if (!context.Users.Any(u => u.Username == "letan"))
            {
                context.Users.Add(new User { Username = "letan", Password = "123", Role = "Letan" });
            }

            // Lưu User trước để đảm bảo không lỗi
            context.SaveChanges();

            // 2. Seed Rooms
            // Tạo danh sách phòng cần thêm
            var roomsToAdd = new List<Room>();

            // Tầng 1
            for (int i = 1; i <= 5; i++)
            {
                string ma = "10" + i;
                if (!context.Rooms.Any(r => r.MaPhong == ma))
                {
                    roomsToAdd.Add(new Room { MaPhong = ma, LoaiPhongID = 1, Tang = 1, TrangThai = 0, GhiChu = "" });
                }
            }

            // Tầng 2
            for (int i = 1; i <= 5; i++)
            {
                string ma = "20" + i;
                if (!context.Rooms.Any(r => r.MaPhong == ma))
                {
                    roomsToAdd.Add(new Room { MaPhong = ma, LoaiPhongID = 2, Tang = 2, TrangThai = 0, GhiChu = "" });
                }
            }

            // Tầng 3
            for (int i = 1; i <= 5; i++)
            {
                string ma = "30" + i;
                if (!context.Rooms.Any(r => r.MaPhong == ma))
                {
                    roomsToAdd.Add(new Room { MaPhong = ma, LoaiPhongID = 2, Tang = 3, TrangThai = 0, GhiChu = "" });
                }
            }

            // Thêm tất cả phòng mới vào context
            if (roomsToAdd.Count > 0)
            {
                context.Rooms.AddRange(roomsToAdd);
                context.SaveChanges();
            }
        }
    }
}