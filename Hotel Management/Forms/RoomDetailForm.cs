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

        // Giá theo yêu cầu
        // Phòng đơn: LoaiPhongID = 1
        // Phòng đôi : LoaiPhongID = 2
        private const decimal GIA_DEM_DON_SAU_18 = 200000m;
        private const decimal GIA_DEM_DON_TRUOC_18 = 250000m;
        private const decimal GIA_DEM_DON_NHIEU = 250000m;

        private const decimal GIA_DEM_DOI_SAU_18 = 300000m;
        private const decimal GIA_DEM_DOI_TRUOC_18 = 350000m;
        private const decimal GIA_DEM_DOI_NHIEU = 350000m;

        private const decimal GIA_GIO_DON_DAU = 70000m;
        private const decimal GIA_GIO_DON_SAU = 20000m;

        private const decimal GIA_GIO_DOI_DAU = 120000m;
        private const decimal GIA_GIO_DOI_SAU = 30000m;

        private const decimal GIA_NUOC_NGOT = 20000m;
        private const decimal GIA_NUOC_SUOI = 10000m;

        private const decimal PHU_THU_TRA_TRE = 40000m;

        private int _selectedStatus;
        private DateTime? _startTime;   // thời gian bắt đầu
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
            lblRoomType.Text = _room.LoaiPhongID == 1 ? "Phòng đơn"
                              : _room.LoaiPhongID == 2 ? "Phòng đôi"
                              : "Khác";
            lblFloor.Text = "Tầng " + _room.Tang;

            // Trạng thái/giờ bắt đầu
            _selectedStatus = _room.TrangThai;
            _startTime = _room.ThoiGianBatDau;

            if (_selectedStatus == 1 && !_startTime.HasValue)
                _startTime = DateTime.Now;

            // Thiết lập kiểu thuê theo dữ liệu, mặc định là GIỜ
            if (_room.KieuThue == 1)
                rdoDem.Checked = true;
            else
                rdoGio.Checked = true;

            ApplyHireModeUI();
            LoadStateFromGhiChu(); // nạp SL, số chai, tiền đã thu + ghi chú hiển thị

            UpdateStatusButtons();

            // Timer realtime
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            _timer.Start();

            TinhTien(); // init
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // CHỈ realtime khi: Có khách + Thuê giờ + đã có thời điểm bắt đầu
            if (_selectedStatus == 1 && _startTime.HasValue && rdoGio.Checked)
            {
                DateTime now = DateTime.Now;
                lblStartTime.Text = _startTime.Value.ToString("dd/MM/yyyy HH:mm:ss");
                lblEndTime.Text = now.ToString("dd/MM/yyyy HH:mm:ss");

                TimeSpan diff = now - _startTime.Value;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

                lblDuration.Text = string.Format("{0:00}:{1:00}:{2:00}",
                    (int)diff.TotalHours, diff.Minutes, diff.Seconds);

                int soGio;
                decimal tienPhong;
                TinhTienPhongGio(out soGio, out tienPhong);

                decimal tienDichVu = TinhTienNuoc();
                UpdateTienHienThi(tienPhong, tienDichVu);
            }
        }

        #region Trạng thái (nút)

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
                case 0: selected = btnStatusTrong; break;
                case 1: selected = btnStatusCoKhach; break;
                case 2: selected = btnStatusChuaDon; break;
                case 3: selected = btnStatusDaDat; break;
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
            // Nếu từ Trống chuyển sang Có khách => giờ bắt đầu = hiện tại
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

        #region Kiểu thuê (Đêm / Giờ)

        private void ApplyHireModeUI()
        {
            // Chỉ thuê ĐÊM mới bắt buộc thông tin khách
            bool needCustomer = rdoDem.Checked;

            txtTenKhach.Enabled = needCustomer;
            txtCCCD.Enabled = needCustomer;
            btnChonAnh.Enabled = needCustomer;

            // Khi thuê GIỜ: ẩn control số lượng
            if (rdoGio.Checked)
            {
                lblSoLuong.Visible = false;
                nudSoLuong.Visible = false;
            }
            else
            {
                lblSoLuong.Visible = true;
                nudSoLuong.Visible = true;

                if (rdoDem.Checked) lblSoLuong.Text = "Số đêm";
            }

            // Nếu đang thuê giờ & có khách mà chưa có thời điểm bắt đầu => set = Now
            if (rdoGio.Checked && _selectedStatus == 1 && !_startTime.HasValue)
            {
                _startTime = DateTime.Now;
            }
        }

        private void rdoDem_CheckedChanged(object sender, EventArgs e)
        {
            ApplyHireModeUI();
            TinhTien();
        }

        private void rdoNgay_CheckedChanged(object sender, EventArgs e)
        {
            // Không sử dụng thuê ngày nữa
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

        private bool IsPhongDon()
        {
            return _room.LoaiPhongID == 1;
        }

        private decimal TinhTienNuoc()
        {
            int slNuocNgot = (int)nudNuocNgot.Value;
            int slNuocSuoi = (int)nudNuocSuoi.Value;
            return slNuocNgot * GIA_NUOC_NGOT + slNuocSuoi * GIA_NUOC_SUOI;
        }

        private void TinhTienPhongGio(out int soGio, out decimal tienPhong)
        {
            soGio = 0;
            tienPhong = 0m;

            if (!_startTime.HasValue) return;

            DateTime start = _startTime.Value;
            DateTime now = DateTime.Now;
            if (now < start) now = start;

            TimeSpan diff = now - start;
            soGio = Math.Max(1, (int)Math.Ceiling(diff.TotalHours));

            bool laPhongDon = IsPhongDon();
            decimal giaGioDau = laPhongDon ? GIA_GIO_DON_DAU : GIA_GIO_DOI_DAU;
            decimal giaGioSau = laPhongDon ? GIA_GIO_DON_SAU : GIA_GIO_DOI_SAU;

            if (soGio <= 1)
                tienPhong = giaGioDau;
            else
                tienPhong = giaGioDau + (soGio - 1) * giaGioSau;
        }

        private decimal TinhTienPhongDem(int soDem, out decimal phuThu)
        {
            phuThu = 0m;
            if (soDem <= 0) return 0m;

            DateTime start = _startTime ?? DateTime.Now;
            bool laPhongDon = IsPhongDon();
            decimal tong = 0m;

            if (soDem == 1)
            {
                bool sau18h = start.TimeOfDay >= new TimeSpan(18, 0, 0);
                if (laPhongDon)
                    tong = sau18h ? GIA_DEM_DON_SAU_18 : GIA_DEM_DON_TRUOC_18;
                else
                    tong = sau18h ? GIA_DEM_DOI_SAU_18 : GIA_DEM_DOI_TRUOC_18;
            }
            else
            {
                decimal giaMoiDem = laPhongDon ? GIA_DEM_DON_NHIEU : GIA_DEM_DOI_NHIEU;
                tong = giaMoiDem * soDem;
            }

            // Phụ thu trả trễ (sau 12h trưa ngày trả chuẩn)
            DateTime now = DateTime.Now;
            DateTime ngayTraChuan = start.Date.AddDays(soDem).AddHours(12);
            if (now > ngayTraChuan)
            {
                phuThu = PHU_THU_TRA_TRE;
                tong += phuThu;
            }

            return tong;
        }

        private decimal GetTienDaThu()
        {
            string raw = txtTienDaThu.Text ?? "";
            raw = raw.Replace(".", "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(raw)) return 0m;
            if (decimal.TryParse(raw, out decimal v)) return v;
            return 0m;
        }

        private void UpdateTienHienThi(decimal tienPhong, decimal tienDichVu)
        {
            decimal daThu = GetTienDaThu();
            decimal tongTruocTru = tienPhong + tienDichVu;
            decimal conLai = Math.Max(0, tongTruocTru - daThu);

            lblTienPhong.Text = tienPhong.ToString("N0") + " đ";
            lblTienDichVu.Text = tienDichVu.ToString("N0") + " đ";
            lblTienDaThu.Text = daThu.ToString("N0") + " đ";
            lblTongTien.Text = conLai.ToString("N0") + " đ";
        }

        private void TinhTien()
        {
            decimal tienPhong = 0m;
            decimal phuThu;
            decimal tienDichVu = TinhTienNuoc();

            if (rdoDem.Checked)
            {
                int sl = (int)nudSoLuong.Value;
                tienPhong = TinhTienPhongDem(sl, out phuThu);
            }
            else if (rdoGio.Checked)
            {
                if (_selectedStatus == 1 && _startTime.HasValue)
                {
                    int soGio;
                    TinhTienPhongGio(out soGio, out tienPhong);
                }
                else
                {
                    tienPhong = 0m;
                }
            }

            UpdateTienHienThi(tienPhong, tienDichVu);
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
            // Tính lại cho chắc chắn
            TinhTien();

            // Sau khi tính tiền thì set trạng thái phòng = Chưa dọn
            _selectedStatus = 2; // Chưa dọn
            _startTime = null;
            UpdateStatusButtons();

            // Sau khi thanh toán xong: không còn kiểu thuê, không còn tên khách hiển thị
            _room.TrangThai = _selectedStatus;
            _room.ThoiGianBatDau = null;
            _room.KieuThue = null;
            _room.TenKhachHienThi = null;

            string ghiChu = BuildGhiChuForSave();

            _roomDal.UpdateTrangThaiFull(
                _room.PhongID,
                _selectedStatus,
                ghiChu,
                null,
                null,
                null);

            _room.GhiChu = ghiChu;

            if (Saved != null) Saved(this, EventArgs.Empty);
            if (BackRequested != null) BackRequested(this, EventArgs.Empty);
        }

        private void txtTienDaThu_TextChanged(object sender, EventArgs e)
        {
            TinhTien();
        }

        #endregion

        #region Lưu / quay lại

        private string BuildGhiChuForSave()
        {
            string noteUser = txtGhiChu.Text.Trim();

            int soDem = rdoDem.Checked ? (int)nudSoLuong.Value : 0;
            int nn = (int)nudNuocNgot.Value;
            int ns = (int)nudNuocSuoi.Value;
            decimal daThu = GetTienDaThu();

            System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>();

            if (rdoDem.Checked && soDem > 0)
                tags.Add("SL=" + soDem);
            if (nn > 0)
                tags.Add("NN=" + nn);
            if (ns > 0)
                tags.Add("NS=" + ns);
            if (daThu > 0)
                tags.Add("DT=" + ((long)daThu));

            string result = noteUser;
            if (tags.Count > 0)
            {
                if (!string.IsNullOrEmpty(result))
                    result += " | ";
                result += string.Join(" | ", tags);
            }

            return result;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            // Thuê đêm bắt buộc nhập tên khách
            if (rdoDem.Checked &&
                string.IsNullOrWhiteSpace(txtTenKhach.Text))
            {
                MessageBox.Show("Vui lòng nhập tên khách khi thuê theo đêm.",
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
            else if (rdoGio.Checked)
            {
                kieuThue = 3; // giờ
                tenKhach = null;

                // Khi lưu mà đang thuê giờ + Có khách nhưng chưa set startTime => set = Now
                if (_selectedStatus == 1 && !_startTime.HasValue)
                    _startTime = DateTime.Now;
            }

            string ghiChu = BuildGhiChuForSave();

            _roomDal.UpdateTrangThaiFull(
                _room.PhongID,
                _selectedStatus,
                ghiChu,
                (_selectedStatus == 1 ? _startTime : (DateTime?)null),
                kieuThue,
                tenKhach);

            // cập nhật object local
            _room.TrangThai = _selectedStatus;
            _room.GhiChu = ghiChu;
            _room.ThoiGianBatDau = (_selectedStatus == 1 ? _startTime : null);
            _room.KieuThue = kieuThue;
            _room.TenKhachHienThi = tenKhach;

            if (Saved != null) Saved(this, EventArgs.Empty);
            if (BackRequested != null) BackRequested(this, EventArgs.Empty);
            // KHÔNG hiển thị MessageBox theo yêu cầu
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            if (BackRequested != null) BackRequested(this, EventArgs.Empty);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (BackRequested != null) BackRequested(this, EventArgs.Empty);
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

        // ====== Ghi chú / tag hệ thống (SL/NN/NS/DT) ======
        private static int GetIntTag(string text, string key, int defaultVal = 0)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            var m = Regex.Match(text, @"\b" + key + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int v))
                return v;
            return defaultVal;
        }

        private static decimal GetDecimalTag(string text, string key, decimal defaultVal = 0m)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            var m = Regex.Match(text, @"\b" + key + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out decimal v))
                return v;
            return defaultVal;
        }

        private static string RemoveSystemTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string pattern = @"(\s*\|\s*)?(SL|NN|NS|DT)\s*=\s*[^|]*";
            string result = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase).Trim();
            if (result.EndsWith("|")) result = result.TrimEnd('|').Trim();
            return result;
        }

        private void LoadStateFromGhiChu()
        {
            string ghiChu = _room.GhiChu ?? "";

            // SL (chỉ dùng cho thuê đêm)
            int soLuong = GetIntTag(ghiChu, "SL", 1);
            if (soLuong < (int)nudSoLuong.Minimum) soLuong = (int)nudSoLuong.Minimum;
            if (soLuong > (int)nudSoLuong.Maximum) soLuong = (int)nudSoLuong.Maximum;
            nudSoLuong.Value = soLuong;

            // số chai nước
            int nn = GetIntTag(ghiChu, "NN", 0);
            if (nn < (int)nudNuocNgot.Minimum) nn = (int)nudNuocNgot.Minimum;
            if (nn > (int)nudNuocNgot.Maximum) nn = (int)nudNuocNgot.Maximum;
            nudNuocNgot.Value = nn;

            int ns = GetIntTag(ghiChu, "NS", 0);
            if (ns < (int)nudNuocSuoi.Minimum) ns = (int)nudNuocSuoi.Minimum;
            if (ns > (int)nudNuocSuoi.Maximum) ns = (int)nudNuocSuoi.Maximum;
            nudNuocSuoi.Value = ns;

            decimal daThu = GetDecimalTag(ghiChu, "DT", 0m);
            if (daThu < 0) daThu = 0;
            txtTienDaThu.Text = daThu.ToString("0");

            // Ghi chú hiển thị bỏ phần tag hệ thống
            txtGhiChu.Text = RemoveSystemTags(ghiChu);
        }
    }
}
