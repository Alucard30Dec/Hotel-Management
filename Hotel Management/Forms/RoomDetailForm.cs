using System;
using System.Drawing;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class RoomDetailForm : Form
    {
        private readonly Room _room;
        private readonly RoomDAL _roomDal = new RoomDAL();

        // Giá
        private const decimal GIA_DEM = 200000m;
        private const decimal GIA_NGAY = 250000m;
        private const decimal GIA_GIO_DAU = 70000m;
        private const decimal GIA_GIO_SAU = 20000m;

        private const decimal GIA_NUOC_NGOT = 20000m;
        private const decimal GIA_NUOC_SUOI = 10000m;

        private int _selectedStatus;
        private DateTime? _startTime;          // thời gian bắt đầu ở
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
            lblRoomCode.Text = _room.MaPhong;
            lblRoomType.Text = _room.LoaiPhongID == 1 ? "Phòng đơn" :
                               _room.LoaiPhongID == 2 ? "Phòng đôi" : "Khác";
            lblFloor.Text = "Tầng " + _room.Tang;

            txtGhiChu.Text = _room.GhiChu ?? "";

            _selectedStatus = _room.TrangThai;
            _startTime = _room.ThoiGianBatDau;

            // Nếu đang Có khách mà chưa có thời gian bắt đầu -> lấy hiện tại
            if (_selectedStatus == 1 && !_startTime.HasValue)
                _startTime = DateTime.Now;

            UpdateStatusButtons();

            // mặc định: thuê giờ, vì bạn muốn tính real-time theo giờ
            rdoGio.Checked = true;
            UpdateCustomerInputEnabled();

            dtpNgayDen.Value = DateTime.Now;
            dtpNgayDi.Value = DateTime.Now.AddDays(1);
            nudSoGio.Value = 1;

            nudNuocNgot.Value = 0;
            nudNuocSuoi.Value = 0;

            // Timer cập nhật theo giây
            _timer = new Timer();
            _timer.Interval = 1000; // 1s
            _timer.Tick += Timer_Tick;
            _timer.Start();

            TinhTien(); // init
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Chỉ cập nhật real-time khi phòng đang Có khách + mode Giờ
            if (_selectedStatus == 1 && _startTime.HasValue && rdoGio.Checked)
            {
                DateTime now = DateTime.Now;
                lblStartTime.Text = _startTime.Value.ToString("dd/MM/yyyy HH:mm:ss");
                lblEndTime.Text = now.ToString("dd/MM/yyyy HH:mm:ss");

                TimeSpan diff = now - _startTime.Value;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

                lblDuration.Text = string.Format("{0:00}:{1:00}:{2:00}",
                    (int)diff.TotalHours,
                    diff.Minutes,
                    diff.Seconds);

                // Tính tiền phòng theo giờ real-time
                int soGio = Math.Max(1, (int)Math.Ceiling(diff.TotalHours));
                decimal tienPhong = soGio == 1
                    ? GIA_GIO_DAU
                    : GIA_GIO_DAU + (soGio - 1) * GIA_GIO_SAU;

                decimal tienDichVu = TinhTienNuoc();
                UpdateTienHienThi(tienPhong, tienDichVu, tienPhong + tienDichVu);
            }
        }

        private void UpdateCustomerInputEnabled()
        {
            bool needCustomer = rdoDem.Checked || rdoNgay.Checked;
            txtTenKhach.Enabled = needCustomer;
            txtCCCD.Enabled = needCustomer;
            btnChonAnh.Enabled = needCustomer;

            if (!needCustomer)
            {
                txtTenKhach.Text = "";
                txtCCCD.Text = "";
                picCCCD.Image = null;
            }
        }

        #region Nút trạng thái

        private void UpdateStatusButtons()
        {
            // Màu chuẩn (đã chỉnh)
            Color cTrong = Color.FromArgb(76, 175, 80);   // xanh lá
            Color cCoKhach = Color.FromArgb(33, 150, 243);  // xanh dương
            Color cChuaDon = Color.FromArgb(255, 138, 128); // đỏ nhạt
            Color cDaDat = Color.FromArgb(255, 152, 0);   // cam

            ResetStatusButtonStyle(btnStatusTrong, cTrong);
            ResetStatusButtonStyle(btnStatusCoKhach, cCoKhach);
            ResetStatusButtonStyle(btnStatusChuaDon, cChuaDon);
            ResetStatusButtonStyle(btnStatusDaDat, cDaDat);

            // bôi đậm nút đang chọn
            Button selected = null;
            Color selectedColor = cTrong;

            switch (_selectedStatus)
            {
                case 0: selected = btnStatusTrong; selectedColor = cTrong; break;
                case 1: selected = btnStatusCoKhach; selectedColor = cCoKhach; break;
                case 2: selected = btnStatusChuaDon; selectedColor = cChuaDon; break;
                case 3: selected = btnStatusDaDat; selectedColor = cDaDat; break;
            }

            if (selected != null)
            {
                selected.BackColor = selectedColor;
                selected.ForeColor = Color.White;
                selected.FlatAppearance.BorderSize = 2;
                selected.FlatAppearance.BorderColor = Color.FromArgb(33, 33, 33);
            }

            // thông tin hiển thị trạng thái hiện tại
            lblCurrentStatus.Text = "Trạng thái phòng: " + GetStatusText(_selectedStatus);
        }

        private void ResetStatusButtonStyle(Button btn, Color baseColor)
        {
            btn.BackColor = baseColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(baseColor, 0.2f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(baseColor, 0.1f);
        }

        private string GetStatusText(int st)
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
            // Nếu trước đó là Trống => bắt đầu đếm thời gian từ bây giờ
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
            _startTime = null; // phòng không còn tính giờ
            UpdateStatusButtons();
        }

        private void btnStatusDaDat_Click(object sender, EventArgs e)
        {
            _selectedStatus = 3;
            _startTime = null;
            UpdateStatusButtons();
        }

        #endregion

        #region Nút chọn mode thuê

        private void rdoDem_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCustomerInputEnabled();
            TinhTien();
        }

        private void rdoNgay_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCustomerInputEnabled();
            TinhTien();
        }

        private void rdoGio_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCustomerInputEnabled();
            TinhTien();
        }

        #endregion

        #region Nước uống & tiền

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

            if (rdoDem.Checked)
            {
                int soDem = Math.Max(1, (int)Math.Ceiling((dtpNgayDi.Value - dtpNgayDen.Value).TotalDays));
                tienPhong = soDem * GIA_DEM;
            }
            else if (rdoNgay.Checked)
            {
                int soNgay = Math.Max(1, (int)Math.Ceiling((dtpNgayDi.Value - dtpNgayDen.Value).TotalDays));
                tienPhong = soNgay * GIA_NGAY;
            }
            else if (rdoGio.Checked)
            {
                // nếu đang Có khách và đã có startTime thì để Timer lo;
                if (!(_selectedStatus == 1 && _startTime.HasValue))
                {
                    int soGio = (int)nudSoGio.Value;
                    if (soGio <= 1) tienPhong = GIA_GIO_DAU;
                    else tienPhong = GIA_GIO_DAU + (soGio - 1) * GIA_GIO_SAU;
                }
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

        private void btnTinhTien_Click(object sender, EventArgs e)
        {
            TinhTien();
        }

        #endregion

        #region Lưu & Quay lại

        private void btnLuu_Click(object sender, EventArgs e)
        {
            // Bắt buộc tên khách khi thuê ngày/đêm
            if ((rdoDem.Checked || rdoNgay.Checked) &&
                string.IsNullOrWhiteSpace(txtTenKhach.Text))
            {
                MessageBox.Show("Vui lòng nhập tên khách khi thuê ngày/đêm.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? kieuThue = null;
            string tenKhachHienThi = null;

            if (rdoDem.Checked)
            {
                kieuThue = 1; // đêm
                tenKhachHienThi = txtTenKhach.Text.Trim();
            }
            else if (rdoNgay.Checked)
            {
                kieuThue = 2; // ngày
                tenKhachHienThi = txtTenKhach.Text.Trim();
            }
            else if (rdoGio.Checked)
            {
                kieuThue = 3; // giờ
                tenKhachHienThi = null;
            }

            // Cập nhật DB
            _roomDal.UpdateTrangThaiFull(
                _room.PhongID,
                _selectedStatus,
                txtGhiChu.Text,
                (_selectedStatus == 1 ? _startTime : (DateTime?)null),
                kieuThue,
                tenKhachHienThi);

            // Cập nhật object local
            _room.TrangThai = _selectedStatus;
            _room.GhiChu = txtGhiChu.Text;
            _room.ThoiGianBatDau = (_selectedStatus == 1 ? _startTime : null);
            _room.KieuThue = kieuThue;
            _room.TenKhachHienThi = tenKhachHienThi;

            Saved?.Invoke(this, EventArgs.Empty);

            MessageBox.Show("Đã lưu trạng thái phòng.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
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
    }
}
