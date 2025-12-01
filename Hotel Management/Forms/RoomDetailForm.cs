using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class RoomDetailForm : Form
    {
        private readonly Room _room;
        private readonly RoomDAL _roomDal = new RoomDAL();

        // Giá mặc định
        private const decimal GIA_DEM = 200000m;
        private const decimal GIA_NGAY = 250000m;
        private const decimal GIA_GIO_DAU = 70000m;
        private const decimal GIA_GIO_SAU = 20000m;

        private const decimal GIA_NUOC_NGOT = 20000m;
        private const decimal GIA_NUOC_SUOI = 10000m;

        private int _selectedStatus;
        private DateTime? _startTime;   // thời gian bắt đầu ở
        private Timer _timer;

        public event EventHandler BackRequested;
        public event EventHandler Saved;

        public RoomDetailForm(Room room)
        {
            _room = room;
            InitializeComponent();
        }

        private void RoomDetailForm_Load(object sender, EventArgs e)
        {
            // Header
            lblRoomCode.Text = _room.MaPhong;
            lblRoomType.Text = _room.LoaiPhongID == 1 ? "Phòng đơn" :
                               _room.LoaiPhongID == 2 ? "Phòng đôi" : "Khác";
            lblFloor.Text = "Tầng " + _room.Tang;

            txtGhiChu.Text = _room.GhiChu ?? string.Empty;

            // Trạng thái & thời gian bắt đầu
            _selectedStatus = _room.TrangThai;
            _startTime = _room.ThoiGianBatDau;

            if (_selectedStatus == 1 && !_startTime.HasValue)
                _startTime = DateTime.Now;

            // Mặc định kiểu thuê: Giờ (để thấy realtime rõ nhất)
            rdoGio.Checked = true;
            ApplyHireModeUI();
            LoadSoLuongFromNote(); // nếu trước đó đã lưu SL vào ghi chú

            UpdateStatusButtons();

            // Timer realtime
            _timer = new Timer();
            _timer.Interval = 1000; // 1s
            _timer.Tick += Timer_Tick;
            _timer.Start();

            TinhTien(); // init
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Chỉ cập nhật realtime khi phòng đang "Có khách" và kiểu thuê giờ
            if (_selectedStatus == 1 && _startTime.HasValue && rdoGio.Checked)
            {
                DateTime now = DateTime.Now;
                lblStartTime.Text = _startTime.Value.ToString("dd/MM/yyyy HH:mm:ss");
                lblEndTime.Text = now.ToString("dd/MM/yyyy HH:mm:ss");

                TimeSpan diff = now - _startTime.Value;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

                lblDuration.Text = string.Format("{0:00}:{1:00}:{2:00}",
                    (int)diff.TotalHours, diff.Minutes, diff.Seconds);

                int soGio = Math.Max(1, (int)Math.Ceiling(diff.TotalHours));
                decimal tienPhong = (soGio <= 1)
                    ? GIA_GIO_DAU
                    : GIA_GIO_DAU + (soGio - 1) * GIA_GIO_SAU;

                decimal tienDichVu = TinhTienNuoc();
                UpdateTienHienThi(tienPhong, tienDichVu, tienPhong + tienDichVu);
            }
        }

        #region Trạng thái (4 nút lớn)

        private void UpdateStatusButtons()
        {
            // màu theo yêu cầu
            Color cTrong = Color.FromArgb(76, 175, 80);      // xanh lá
            Color cCoKhach = Color.FromArgb(33, 150, 243);   // xanh dương
            Color cChuaDon = Color.FromArgb(255, 138, 128);  // đỏ nhạt
            Color cDaDat = Color.FromArgb(255, 152, 0);      // cam

            ResetStatusButtonStyle(btnStatusTrong, cTrong);
            ResetStatusButtonStyle(btnStatusCoKhach, cCoKhach);
            ResetStatusButtonStyle(btnStatusChuaDon, cChuaDon);
            ResetStatusButtonStyle(btnStatusDaDat, cDaDat);

            Button selected = btnStatusTrong;
            switch (_selectedStatus)
            {
                case 0:
                    selected = btnStatusTrong;
                    break;
                case 1:
                    selected = btnStatusCoKhach;
                    break;
                case 2:
                    selected = btnStatusChuaDon;
                    break;
                case 3:
                    selected = btnStatusDaDat;
                    break;
            }

            selected.FlatAppearance.BorderSize = 3;
            selected.FlatAppearance.BorderColor = Color.FromArgb(33, 33, 33);

            lblCurrentStatus.Text = "Trạng thái phòng";
            lblCurrentStatusDesc.Text = GetStatusText(_selectedStatus);
        }

        private static void ResetStatusButtonStyle(Button btn, Color baseColor)
        {
            btn.BackColor = baseColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        private static string GetStatusText(int st)
        {
            switch (st)
            {
                case 0: return "Trống";
                case 1: return "Có khách";
                case 2: return "Chưa dọn";
                case 3: return "Đã có khách đặt";
                default: return "Không rõ";
            }
        }

        private void btnStatusTrong_Click(object sender, EventArgs e)
        {
            _selectedStatus = 0;
            _startTime = null;
            UpdateStatusButtons();
        }

        private void btnStatusCoKhach_Click(object sender, EventArgs e)
        {
            // Nếu trước đó là Trống => bắt đầu đếm từ bây giờ
            if (_room.TrangThai == 0 && _selectedStatus != 1)
                _startTime = DateTime.Now;
            else if (!_startTime.HasValue)
                _startTime = DateTime.Now;

            _selectedStatus = 1;
            UpdateStatusButtons();
        }

        private void btnStatusChuaDon_Click(object sender, EventArgs e)
        {
            _selectedStatus = 2;
            _startTime = null;
            UpdateStatusButtons();
        }

        private void btnStatusDaDat_Click(object sender, EventArgs e)
        {
            _selectedStatus = 3;
            _startTime = null;
            UpdateStatusButtons();
        }

        #endregion

        #region Thuê: Đêm / Ngày / Giờ + Số lượng

        private void ApplyHireModeUI()
        {
            bool needCustomer = rdoDem.Checked || rdoNgay.Checked;
            txtTenKhach.Enabled = needCustomer;
            txtCCCD.Enabled = needCustomer;
            btnChonAnh.Enabled = needCustomer;

            if (rdoDem.Checked)
                lblSoLuong.Text = "Số đêm";
            else if (rdoNgay.Checked)
                lblSoLuong.Text = "Số ngày";
            else
                lblSoLuong.Text = "Số giờ";
        }

        private void rdoDem_CheckedChanged(object sender, EventArgs e)
        {
            ApplyHireModeUI();
            TinhTien();
        }

        private void rdoNgay_CheckedChanged(object sender, EventArgs e)
        {
            ApplyHireModeUI();
            TinhTien();
        }

        private void rdoGio_CheckedChanged(object sender, EventArgs e)
        {
            ApplyHireModeUI();
            TinhTien();
        }

        #endregion

        #region Tính tiền

        private decimal TinhTienNuoc()
        {
            int slNuocNgot = (int)nudNuocNgot.Value;
            int slNuocSuoi = (int)nudNuocSuoi.Value;
            return slNuocNgot * GIA_NUOC_NGOT + slNuocSuoi * GIA_NUOC_SUOI;
        }

        private void UpdateTienHienThi(decimal tienPhong, decimal tienDichVu, decimal tong)
        {
            lblTienPhong.Text = tienPhong.ToString("N0") + " đ";
            lblTienDichVu.Text = tienDichVu.ToString("N0") + " đ";
            lblTongTien.Text = tong.ToString("N0") + " đ";
        }

        private void TinhTien()
        {
            decimal tienPhong = 0;
            int sl = (int)nudSoLuong.Value;

            if (rdoDem.Checked)
            {
                tienPhong = sl * GIA_DEM;
            }
            else if (rdoNgay.Checked)
            {
                tienPhong = sl * GIA_NGAY;
            }
            else if (rdoGio.Checked)
            {
                if (sl <= 1)
                    tienPhong = GIA_GIO_DAU;
                else
                    tienPhong = GIA_GIO_DAU + (sl - 1) * GIA_GIO_SAU;
            }

            decimal tienDichVu = TinhTienNuoc();
            UpdateTienHienThi(tienPhong, tienDichVu, tienPhong + tienDichVu);
        }

        private void nudNuocNgot_ValueChanged(object sender, EventArgs e)
        {
            TinhTien();
        }

        private void nudNuocSuoi_ValueChanged(object sender, EventArgs e)
        {
            TinhTien();
        }

        private void nudSoLuong_ValueChanged(object sender, EventArgs e)
        {
            TinhTien();
        }

        private void btnTinhTien_Click(object sender, EventArgs e)
        {
            TinhTien();
        }

        #endregion

        #region Lưu / Quay lại

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if ((rdoDem.Checked || rdoNgay.Checked) &&
                string.IsNullOrWhiteSpace(txtTenKhach.Text))
            {
                MessageBox.Show("Vui lòng nhập tên khách khi thuê ngày/đêm.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? kieuThue = null;
            string tenKhach = null;

            if (rdoDem.Checked)
            {
                kieuThue = 1; // đêm
                tenKhach = txtTenKhach.Text.Trim();
            }
            else if (rdoNgay.Checked)
            {
                kieuThue = 2; // ngày
                tenKhach = txtTenKhach.Text.Trim();
            }
            else if (rdoGio.Checked)
            {
                kieuThue = 3; // giờ
                tenKhach = null;
            }

            string ghiChu = UpsertSoLuongToNote(txtGhiChu.Text, (int)nudSoLuong.Value);

            _roomDal.UpdateTrangThaiFull(
                _room.PhongID,
                _selectedStatus,
                ghiChu,
                (_selectedStatus == 1 ? _startTime : (DateTime?)null),
                kieuThue,
                tenKhach
            );

            // cập nhật object local
            _room.TrangThai = _selectedStatus;
            _room.GhiChu = ghiChu;
            _room.ThoiGianBatDau = (_selectedStatus == 1 ? _startTime : null);
            _room.KieuThue = kieuThue;
            _room.TenKhachHienThi = tenKhach;

            if (Saved != null)
                Saved(this, EventArgs.Empty);

            MessageBox.Show("Đã lưu trạng thái phòng.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string UpsertSoLuongToNote(string note, int soLuong)
        {
            if (note == null)
                note = string.Empty;

            if (Regex.IsMatch(note, @"SL=\d+", RegexOptions.IgnoreCase))
            {
                return Regex.Replace(note, @"SL=\d+", "SL=" + soLuong,
                    RegexOptions.IgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(note))
                return "SL=" + soLuong;

            return note.Trim() + " | SL=" + soLuong;
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            if (BackRequested != null)
                BackRequested(this, EventArgs.Empty);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (BackRequested != null)
                BackRequested(this, EventArgs.Empty);
        }

        #endregion

        private void btnChonAnh_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        picCCCD.Image = Image.FromFile(ofd.FileName);
                        picCCCD.Tag = ofd.FileName;
                    }
                    catch
                    {
                        MessageBox.Show("Không thể mở hình ảnh.", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadSoLuongFromNote()
        {
            if (string.IsNullOrWhiteSpace(_room.GhiChu))
                return;

            Match m = Regex.Match(_room.GhiChu, @"SL=(\d+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                int value;
                if (int.TryParse(m.Groups[1].Value, out value))
                {
                    if (value < (int)nudSoLuong.Minimum) value = (int)nudSoLuong.Minimum;
                    if (value > (int)nudSoLuong.Maximum) value = (int)nudSoLuong.Maximum;
                    nudSoLuong.Value = value;
                }
            }
        }
    }
}
