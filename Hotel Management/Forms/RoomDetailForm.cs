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
        private DateTime? _startTime;
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

            _selectedStatus = _room.TrangThai;
            _startTime = _room.ThoiGianBatDau;

            if (_selectedStatus == 1 && !_startTime.HasValue)
                _startTime = DateTime.Now;

            // Thiết lập kiểu thuê theo dữ liệu, mặc định giờ nếu null
            if (_room.KieuThue == 1)
                rdoDem.Checked = true;
            else
                rdoGio.Checked = true;

            ApplyHireModeUI();
            LoadStateFromGhiChu();

            // Nếu phòng đêm đã lưu tên khách thì hiển thị lại
            if (!string.IsNullOrWhiteSpace(_room.TenKhachHienThi))
                txtTenKhach.Text = _room.TenKhachHienThi;

            UpdateStatusButtons();

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            _timer.Start();

            TinhTien();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Chỉ đếm giờ thuê GIỜ khi có khách
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

        #region Trạng thái

        private void UpdateStatusButtons()
        {
            Color cTrong = Color.FromArgb(76, 175, 80);
            Color cCoKhach = Color.FromArgb(33, 150, 243);
            Color cChuaDon = Color.FromArgb(255, 138, 128);
            Color cDaDat = Color.FromArgb(255, 152, 0);

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
            TinhTien();
        }

        private void btnStatusCoKhach_Click(object sender, EventArgs e)
        {
            if (_room.TrangThai == 0 && _selectedStatus != 1)
                _startTime = DateTime.Now;
            else if (!_startTime.HasValue)
                _startTime = DateTime.Now;

            _selectedStatus = 1;
            UpdateStatusButtons();
            TinhTien();
        }

        private void btnStatusChuaDon_Click(object sender, EventArgs e)
        {
            _selectedStatus = 2;
            _startTime = null;
            UpdateStatusButtons();
            TinhTien();
        }

        private void btnStatusDaDat_Click(object sender, EventArgs e)
        {
            _selectedStatus = 3;
            _startTime = null;
            UpdateStatusButtons();
            TinhTien();
        }

        #endregion

        #region Kiểu thuê

        private void ApplyHireModeUI()
        {
            bool needCustomer = rdoDem.Checked;

            txtTenKhach.Enabled = needCustomer;
            txtCCCD.Enabled = needCustomer;
            btnChonAnh.Enabled = needCustomer;

            if (rdoGio.Checked)
            {
                lblSoLuong.Visible = false;
                nudSoLuong.Visible = false;
            }
            else
            {
                lblSoLuong.Visible = true;
                nudSoLuong.Visible = true;
                lblSoLuong.Text = "Số đêm";
            }

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
            // radio này đã ẩn + disable, nhưng giữ handler cho an toàn
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

            DateTime now = DateTime.Now;
            DateTime ngayTraChuan = start.Date.AddDays(soDem).AddHours(12);
            if (now > ngayTraChuan)
            {
                phuThu = PHU_THU_TRA_TRE;
                tong += phuThu;
            }

            return tong;
        }

        private decimal GetTienDaThuRaw()
        {
            string raw = txtTienDaThu.Text ?? "";
            raw = raw.Replace(".", "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(raw)) return 0m;

            decimal v;
            if (decimal.TryParse(raw, out v)) return v;
            return 0m;
        }

        // giá trị thực tế (đồng) = raw * 1000 (100 => 100.000)
        private decimal GetTienDaThu()
        {
            decimal raw = GetTienDaThuRaw();
            return raw * 1000m;
        }

        private static decimal RoundUp(decimal value, decimal step)
        {
            if (step <= 0) return value;
            return Math.Ceiling(value / step) * step;
        }

        private void SetSuggestionButton(Button btn, decimal amount)
        {
            if (btn == null) return;

            if (amount <= 0)
            {
                btn.Visible = false;
                btn.Tag = null;
                btn.Text = "";
            }
            else
            {
                btn.Visible = true;
                btn.Tag = amount;
                btn.Text = amount.ToString("N0") + " đ";
            }
        }

        private void UpdateTienHienThi(decimal tienPhong, decimal tienDichVu)
        {
            decimal daThu = GetTienDaThu();
            decimal tongTruocTru = tienPhong + tienDichVu;          // tổng tiền phòng + nước
            decimal conLai = Math.Max(0, tongTruocTru - daThu);     // tổng tiền CẦN THANH TOÁN (còn lại)

            lblTienPhong.Text = tienPhong.ToString("N0") + " đ";
            lblTienDichVu.Text = tienDichVu.ToString("N0") + " đ";
            lblTienDaThu.Text = daThu.ToString("N0") + " đ";
            lblTongTien.Text = conLai.ToString("N0") + " đ";

            // === GỢI Ý 3 KHOẢNG TIỀN ===
            // 1) Tổng tiền cần thanh toán (còn lại)          -> gợi ý 1
            // 2) Tổng tiền phòng chưa tính nước (tienPhong)  -> gợi ý 2
            // 3) Một số tiền nhỏ hơn nữa                     -> gợi ý 3 (khoảng 50% số phù hợp, đã đảm bảo nhỏ hơn)

            decimal sugTotalPay = conLai;      // tổng cần thanh toán
            decimal sugRoomOnly = tienPhong;   // chỉ tiền phòng (không nước, không trừ đã thu)
            decimal sugSmaller = 0m;

            // chọn base nhỏ hơn để làm "một số tiền nhỏ hơn nữa"
            decimal baseSmall = 0m;
            if (sugTotalPay > 0 && sugRoomOnly > 0)
                baseSmall = Math.Min(sugTotalPay, sugRoomOnly);
            else if (sugTotalPay > 0)
                baseSmall = sugTotalPay;
            else if (sugRoomOnly > 0)
                baseSmall = sugRoomOnly;

            if (baseSmall > 0)
            {
                // lấy 50% rồi làm tròn lên 10.000
                sugSmaller = RoundUp(baseSmall * 0.5m, 10000m);

                // đảm bảo nhỏ hơn base
                if (sugSmaller >= baseSmall)
                    sugSmaller = baseSmall - 10000m;

                if (sugSmaller < 0)
                    sugSmaller = 0;
            }

            // tránh trùng nhau
            if (sugRoomOnly == sugTotalPay) sugRoomOnly = 0;
            if (sugSmaller == sugTotalPay || sugSmaller == sugRoomOnly) sugSmaller = 0;

            SetSuggestionButton(btnGoiY1, sugTotalPay);
            SetSuggestionButton(btnGoiY2, sugRoomOnly);
            SetSuggestionButton(btnGoiY3, sugSmaller);
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

        private void txtTienDaThu_TextChanged(object sender, EventArgs e)
        {
            TinhTien();
        }

        private void btnGoiY_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            decimal amount = (decimal)btn.Tag; // số tiền thật (đồng)
            decimal raw = amount / 1000m;      // đưa về đơn vị x1000 để gõ vào textbox

            txtTienDaThu.Text = raw.ToString("0");
        }

        private void btnTinhTien_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn tính tiền cho phòng này?\nSau khi tính tiền phòng sẽ chuyển sang trạng thái 'Chưa dọn'.",
                "Xác nhận tính tiền",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            TinhTien();

            _selectedStatus = 2; // Chưa dọn
            _startTime = null;
            UpdateStatusButtons();

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

        #endregion

        #region Lưu / quay lại

        private string BuildGhiChuForSave()
        {
            string noteUser = txtGhiChu.Text.Trim();

            int soDem = rdoDem.Checked ? (int)nudSoLuong.Value : 0;
            int nn = (int)nudNuocNgot.Value;
            int ns = (int)nudNuocSuoi.Value;
            decimal daThu = GetTienDaThu();
            string cccd = txtCCCD.Text.Trim();

            System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>();

            if (rdoDem.Checked && soDem > 0)
                tags.Add("SL=" + soDem);
            if (nn > 0)
                tags.Add("NN=" + nn);
            if (ns > 0)
                tags.Add("NS=" + ns);
            if (daThu > 0)
                tags.Add("DT=" + ((long)daThu));
            if (!string.IsNullOrEmpty(cccd))
                tags.Add("CCCD=" + cccd);

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
                kieuThue = 1;
                tenKhach = txtTenKhach.Text.Trim();
            }
            else if (rdoGio.Checked)
            {
                kieuThue = 3;
                tenKhach = null;

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

            _room.TrangThai = _selectedStatus;
            _room.GhiChu = ghiChu;
            _room.ThoiGianBatDau = (_selectedStatus == 1 ? _startTime : null);
            _room.KieuThue = kieuThue;
            _room.TenKhachHienThi = tenKhach;

            if (Saved != null) Saved(this, EventArgs.Empty);
            if (BackRequested != null) BackRequested(this, EventArgs.Empty);
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
            using (OpenFileDialog ofd = new OpenFileDialog())
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

        #region Tag ghi chú (SL/NN/NS/DT/CCCD)

        private static int GetIntTag(string text, string key, int defaultVal)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            Match m = Regex.Match(text, @"\b" + key + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                int v;
                if (int.TryParse(m.Groups[1].Value, out v)) return v;
            }
            return defaultVal;
        }

        private static decimal GetDecimalTag(string text, string key, decimal defaultVal)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            Match m = Regex.Match(text, @"\b" + key + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                decimal v;
                if (decimal.TryParse(m.Groups[1].Value, out v)) return v;
            }
            return defaultVal;
        }

        private static string GetStringTag(string text, string key, string defaultVal)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            Match m = Regex.Match(text, @"\b" + key + @"\s*=\s*([^|]+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();
            return defaultVal;
        }

        private static string RemoveSystemTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string pattern = @"(\s*\|\s*)?(SL|NN|NS|DT|CCCD)\s*=\s*[^|]*";
            string result = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase).Trim();
            if (result.EndsWith("|")) result = result.TrimEnd('|').Trim();
            return result;
        }

        private void LoadStateFromGhiChu()
        {
            string ghiChu = _room.GhiChu ?? "";

            int soLuong = GetIntTag(ghiChu, "SL", 1);
            if (soLuong < (int)nudSoLuong.Minimum) soLuong = (int)nudSoLuong.Minimum;
            if (soLuong > (int)nudSoLuong.Maximum) soLuong = (int)nudSoLuong.Maximum;
            nudSoLuong.Value = soLuong;

            int nn = GetIntTag(ghiChu, "NN", 0);
            if (nn < (int)nudNuocNgot.Minimum) nn = (int)nudNuocNgot.Minimum;
            if (nn > (int)nudNuocNgot.Maximum) nn = (int)nudNuocNgot.Maximum;
            nudNuocNgot.Value = nn;

            int ns = GetIntTag(ghiChu, "NS", 0);
            if (ns < (int)nudNuocSuoi.Minimum) ns = (int)nudNuocSuoi.Minimum;
            if (ns > (int)nudNuocSuoi.Maximum) ns = (int)nudNuocSuoi.Maximum;
            nudNuocSuoi.Value = ns;

            decimal daThu = GetDecimalTag(ghiChu, "DT", 0m);
            if (daThu <= 0)
            {
                // Không có dữ liệu đã thu -> để trống
                txtTienDaThu.Text = "";
            }
            else
            {
                // Có dữ liệu -> hiển thị dạng x1000 (vd 100000 -> 100)
                txtTienDaThu.Text = (daThu / 1000m).ToString("0");
            }


            string cccd = GetStringTag(ghiChu, "CCCD", "");
            txtCCCD.Text = cccd;

            txtGhiChu.Text = RemoveSystemTags(ghiChu);
        }

        #endregion
    }
}
