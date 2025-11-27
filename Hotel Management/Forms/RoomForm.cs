using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class RoomForm : Form
    {
        private RoomDAL roomDal = new RoomDAL();

        public RoomForm()
        {
            InitializeComponent();
        }

        private void RoomForm_Load(object sender, EventArgs e)
        {
            LoadLoaiPhongToCombo();
            LoadTrangThaiToCombo();
            LoadRoomsToGrid();
        }

        private void LoadLoaiPhongToCombo()
        {
            cboLoaiPhong.Items.Clear();
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                string query = "SELECT LoaiPhongID, TenLoai FROM LOAIPHONG";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cboLoaiPhong.Items.Add(new ComboBoxItem
                    {
                        Value = reader.GetInt32(0),
                        Text = reader.GetString(1)
                    });
                }
            }
            if (cboLoaiPhong.Items.Count > 0)
                cboLoaiPhong.SelectedIndex = 0;
        }

        private void LoadTrangThaiToCombo()
        {
            cboTrangThai.Items.Clear();
            // 0 = Trống
            cboTrangThai.Items.Add(new ComboBoxItem { Value = 0, Text = "Trống" });
            // 1 = Có khách
            cboTrangThai.Items.Add(new ComboBoxItem { Value = 1, Text = "Có khách" });
            // 2 = Chưa dọn
            cboTrangThai.Items.Add(new ComboBoxItem { Value = 2, Text = "Chưa dọn" });
            // 3 = Đã có khách đặt
            cboTrangThai.Items.Add(new ComboBoxItem { Value = 3, Text = "Đã có khách đặt" });

            cboTrangThai.SelectedIndex = 0;
        }


        private void LoadRoomsToGrid()
        {
            var list = roomDal.GetAll();
            dgvRooms.DataSource = list;
        }

        private void dgvRooms_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvRooms.CurrentRow != null)
            {
                var room = dgvRooms.CurrentRow.DataBoundItem as Room;
                if (room != null)
                {
                    txtPhongID.Text = room.PhongID.ToString();
                    txtMaPhong.Text = room.MaPhong;
                    txtGhiChu.Text = room.GhiChu;

                    for (int i = 0; i < cboLoaiPhong.Items.Count; i++)
                    {
                        var item = (ComboBoxItem)cboLoaiPhong.Items[i];
                        if ((int)item.Value == room.LoaiPhongID)
                        {
                            cboLoaiPhong.SelectedIndex = i;
                            break;
                        }
                    }

                    for (int i = 0; i < cboTrangThai.Items.Count; i++)
                    {
                        var item = (ComboBoxItem)cboTrangThai.Items[i];
                        if ((int)item.Value == room.TrangThai)
                        {
                            cboTrangThai.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtMaPhong.Text.Trim()))
            {
                MessageBox.Show("Vui lòng nhập mã phòng");
                return;
            }

            var loaiItem = (ComboBoxItem)cboLoaiPhong.SelectedItem;
            var trangThaiItem = (ComboBoxItem)cboTrangThai.SelectedItem;

            Room room = new Room
            {
                MaPhong = txtMaPhong.Text.Trim(),
                LoaiPhongID = (int)loaiItem.Value,
                TrangThai = (int)trangThaiItem.Value,
                GhiChu = txtGhiChu.Text.Trim()
            };

            roomDal.Insert(room);
            LoadRoomsToGrid();
            MessageBox.Show("Thêm phòng thành công");
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPhongID.Text))
            {
                MessageBox.Show("Vui lòng chọn phòng cần sửa");
                return;
            }

            var loaiItem = (ComboBoxItem)cboLoaiPhong.SelectedItem;
            var trangThaiItem = (ComboBoxItem)cboTrangThai.SelectedItem;

            Room room = new Room
            {
                PhongID = int.Parse(txtPhongID.Text),
                MaPhong = txtMaPhong.Text.Trim(),
                LoaiPhongID = (int)loaiItem.Value,
                TrangThai = (int)trangThaiItem.Value,
                GhiChu = txtGhiChu.Text.Trim()
            };

            roomDal.Update(room);
            LoadRoomsToGrid();
            MessageBox.Show("Cập nhật phòng thành công");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPhongID.Text))
            {
                MessageBox.Show("Vui lòng chọn phòng cần xoá");
                return;
            }

            int id = int.Parse(txtPhongID.Text);
            if (MessageBox.Show("Bạn có chắc muốn xoá phòng này?", "Xác nhận",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                roomDal.Delete(id);
                LoadRoomsToGrid();
                MessageBox.Show("Xoá phòng thành công");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadRoomsToGrid();
        }
    }
}
