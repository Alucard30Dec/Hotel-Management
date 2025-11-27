using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using HotelManagement;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class BookingForm : Form
    {
        private BookingDAL bookingDal = new BookingDAL();

        public BookingForm()
        {
            InitializeComponent();
        }

        private void BookingForm_Load(object sender, EventArgs e)
        {
            LoadCustomers();
            LoadRooms();
            LoadBookings();
        }

        private void LoadCustomers()
        {
            cboKhachHang.Items.Clear();
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = "SELECT KhachHangID, HoTen FROM KHACHHANG";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    cboKhachHang.Items.Add(new ComboBoxItem
                    {
                        Value = rd.GetInt32(0),
                        Text = rd.GetString(1)
                    });
                }
            }
            if (cboKhachHang.Items.Count > 0)
                cboKhachHang.SelectedIndex = 0;
        }

        private void LoadRooms()
        {
            cboPhong.Items.Clear();
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                // Lấy phòng đang trống
                string query = "SELECT PhongID, MaPhong FROM PHONG WHERE TrangThai = 0";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    cboPhong.Items.Add(new ComboBoxItem
                    {
                        Value = rd.GetInt32(0),
                        Text = rd.GetString(1)
                    });
                }
            }
            if (cboPhong.Items.Count > 0)
                cboPhong.SelectedIndex = 0;
        }

        private void LoadBookings()
        {
            dgvBookings.DataSource = bookingDal.GetAll();
        }

        private void dgvBookings_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvBookings.CurrentRow != null)
            {
                var b = dgvBookings.CurrentRow.DataBoundItem as Booking;
                if (b != null)
                {
                    txtDatPhongID.Text = b.DatPhongID.ToString();
                    dtpNgayDen.Value = b.NgayDen;
                    dtpNgayDiDK.Value = b.NgayDiDuKien;
                    txtTienCoc.Text = b.TienCoc.ToString();

                    // chọn lại khách & phòng
                    for (int i = 0; i < cboKhachHang.Items.Count; i++)
                    {
                        var item = (ComboBoxItem)cboKhachHang.Items[i];
                        if ((int)item.Value == b.KhachHangID)
                        {
                            cboKhachHang.SelectedIndex = i;
                            break;
                        }
                    }
                    for (int i = 0; i < cboPhong.Items.Count; i++)
                    {
                        var item = (ComboBoxItem)cboPhong.Items[i];
                        if ((int)item.Value == b.PhongID)
                        {
                            cboPhong.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (cboKhachHang.SelectedItem == null || cboPhong.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn khách hàng và phòng");
                return;
            }

            var khItem = (ComboBoxItem)cboKhachHang.SelectedItem;
            var phongItem = (ComboBoxItem)cboPhong.SelectedItem;

            Booking b = new Booking
            {
                KhachHangID = (int)khItem.Value,
                PhongID = (int)phongItem.Value,
                NgayDen = dtpNgayDen.Value,
                NgayDiDuKien = dtpNgayDiDK.Value,
                TrangThai = 0, // Đặt trước
                TienCoc = decimal.TryParse(txtTienCoc.Text, out var coc) ? coc : 0
            };

            int newId = bookingDal.CreateBooking(b);
            LoadBookings();
            MessageBox.Show("Tạo phiếu đặt phòng thành công. Mã: " + newId);
        }

        private void btnCheckIn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDatPhongID.Text))
            {
                MessageBox.Show("Vui lòng chọn phiếu đặt phòng");
                return;
            }

            int id = int.Parse(txtDatPhongID.Text);
            bookingDal.UpdateStatus(id, 1, null); // đang ở
            LoadBookings();
            MessageBox.Show("Check-in thành công");
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDatPhongID.Text))
            {
                MessageBox.Show("Vui lòng chọn phiếu đặt phòng");
                return;
            }

            int id = int.Parse(txtDatPhongID.Text);
            bookingDal.UpdateStatus(id, 2, DateTime.Now); // đã trả
            LoadBookings();
            MessageBox.Show("Check-out thành công. Vui lòng sang màn hình hoá đơn để thanh toán.");
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadBookings();
            LoadRooms(); // cập nhật phòng trống
        }
    }
}
