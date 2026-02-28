using System.Data.Entity;
using System.Data.Entity.Infrastructure; // DbConfigurationType
using HotelManagement.Models;

namespace HotelManagement.Data
{
    [DbConfigurationType(typeof(TiDbEfConfiguration))]
    public class HotelDbContext : DbContext
    {
        public HotelDbContext() : base("name=HotelDb") { }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>().ToTable("PHONG");
            modelBuilder.Entity<Customer>().ToTable("KHACHHANG");
            modelBuilder.Entity<Booking>().ToTable("DATPHONG");
            modelBuilder.Entity<Booking>().Ignore(x => x.BookingType);
            modelBuilder.Entity<Invoice>().ToTable("HOADON");
            modelBuilder.Entity<User>().ToTable("USERS");

            base.OnModelCreating(modelBuilder);
        }
    }
}
