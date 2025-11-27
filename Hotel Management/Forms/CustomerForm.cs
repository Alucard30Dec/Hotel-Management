using System;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class CustomerForm : Form
    {
        private CustomerDAL customerDal = new CustomerDAL();

        public CustomerForm()
        {
            InitializeComponent();
        }

        private void CustomerForm_Load(object sender, EventArgs e)
        {
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            dgvCustomers.DataSource = customerDal.GetAll();
        }

        private void dgvCustomers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvCustomers.CurrentRow != null)
            {
                var c = dgvCustomers.CurrentRow.DataBoundItem as Customer;
                if (c != null)
                {
                    txtKhachHangID.Text = c.KhachHangID.ToString();
                    txtHoTen.Text = c.HoTen;
                    txtCCCD.Text = c.CCCD;
                    txtDienThoai.Text = c.DienThoai;
                    txtDiaChi.Text = c.DiaChi;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtHoTen.Text.Trim()) || string.IsNullOrEmpty(txtCCCD.Text.Trim()))
            {
                MessageBox.Show("Vui lòng nhập họ tên và CCCD");
                return;
            }

            Customer c = new Customer
            {
                HoTen = txtHoTen.Text.Trim(),
                CCCD = txtCCCD.Text.Trim(),
                DienThoai = txtDienThoai.Text.Trim(),
                DiaChi = txtDiaChi.Text.Trim()
            };
            customerDal.Insert(c);
            LoadCustomers();
            MessageBox.Show("Thêm khách hàng thành công");
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtKhachHangID.Text))
            {
                MessageBox.Show("Vui lòng chọn khách hàng");
                return;
            }

            Customer c = new Customer
            {
                KhachHangID = int.Parse(txtKhachHangID.Text),
                HoTen = txtHoTen.Text.Trim(),
                CCCD = txtCCCD.Text.Trim(),
                DienThoai = txtDienThoai.Text.Trim(),
                DiaChi = txtDiaChi.Text.Trim()
            };
            customerDal.Update(c);
            LoadCustomers();
            MessageBox.Show("Cập nhật khách hàng thành công");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtKhachHangID.Text))
            {
                MessageBox.Show("Vui lòng chọn khách hàng");
                return;
            }

            int id = int.Parse(txtKhachHangID.Text);
            if (MessageBox.Show("Bạn có chắc muốn xoá khách hàng này?", "Xác nhận",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                customerDal.Delete(id);
                LoadCustomers();
                MessageBox.Show("Xoá khách hàng thành công");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadCustomers();
        }
    }
}
