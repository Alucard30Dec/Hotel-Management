using System;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class InvoiceForm : Form
    {
        private BookingDAL bookingDal = new BookingDAL();
        private InvoiceDAL invoiceDal = new InvoiceDAL();

        public InvoiceForm()
        {
            InitializeComponent();
        }

        private void btnTinhTien_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtDatPhongID.Text, out int datPhongId))
            {
                MessageBox.Show("Vui lòng nhập mã phiếu đặt phòng hợp lệ");
                return;
            }

            Booking b = bookingDal.GetById(datPhongId);
            if (b == null)
            {
                MessageBox.Show("Không tìm thấy phiếu đặt phòng");
                return;
            }

            DateTime ngayDiThucTe = b.NgayDiThucTe ?? DateTime.Now;
            TimeSpan diff = ngayDiThucTe - b.NgayDen;
            int soNgay = (int)Math.Ceiling(diff.TotalDays);
            if (soNgay <= 0) soNgay = 1;

            decimal donGiaNgay = bookingDal.GetDonGiaNgayByPhong(b.PhongID);
            decimal tienPhong = soNgay * donGiaNgay;
            decimal tongTien = tienPhong - b.TienCoc; // đơn giản: trừ tiền cọc

            lblInfo.Text = $"Ngày đến: {b.NgayDen:dd/MM/yyyy HH:mm} - " +
                           $"Ngày đi: {ngayDiThucTe:dd/MM/yyyy HH:mm}\n" +
                           $"Số ngày tính tiền: {soNgay}\n" +
                           $"Đơn giá / ngày: {donGiaNgay:N0} đ\n" +
                           $"Tiền phòng: {tienPhong:N0} đ\n" +
                           $"Tiền cọc: {b.TienCoc:N0} đ";

            txtTongTien.Text = tongTien.ToString();
        }

        private void btnLuuHoaDon_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtDatPhongID.Text, out int datPhongId))
            {
                MessageBox.Show("Mã phiếu không hợp lệ");
                return;
            }
            if (!decimal.TryParse(txtTongTien.Text, out decimal tongTien))
            {
                MessageBox.Show("Tổng tiền không hợp lệ");
                return;
            }

            Invoice inv = new Invoice
            {
                DatPhongID = datPhongId,
                NgayLap = DateTime.Now,
                TongTien = tongTien,
                DaThanhToan = true
            };
            invoiceDal.CreateInvoice(inv);
            MessageBox.Show("Lưu hoá đơn thành công");
        }
    }
}
