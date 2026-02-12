using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
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
        private readonly BookingDAL _bookingDal = new BookingDAL();

        public event EventHandler BackRequested;
        public event EventHandler Saved;

        private bool _layoutApplied = false;

        // Colors
        private readonly Color clrHeaderBg = Color.FromArgb(227, 242, 253); 
        private readonly Color clrHeaderText = Color.FromArgb(25, 118, 210); 
        private readonly Color clrPrimary = Color.FromArgb(33, 150, 243);    

        public RoomDetailForm(Room room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            _room = room;

            InitializeComponent();
            ApplyResponsiveLayout();
        }

        private void RoomDetailForm_Load(object sender, EventArgs e)
        {
            lblTitle.Text = "Nhận phòng nhanh";
            lblRoomText.Text = $"{_room.MaPhong} • {(_room.LoaiPhongID == 1 ? "Phòng Đơn" : _room.LoaiPhongID == 2 ? "Phòng Đôi" : "Phòng")}, Tầng {_room.Tang}";
            
            InitCombos();
            BindRoomInfo();

            dtpNhanPhong.Value = _room.ThoiGianBatDau ?? DateTime.Now;
            LoadStateFromGhiChu(_room.GhiChu);

            if (!string.IsNullOrWhiteSpace(_room.TenKhachHienThi) && string.IsNullOrWhiteSpace(txtHoTen.Text))
                txtHoTen.Text = _room.TenKhachHienThi;
        }

        private void ApplyResponsiveLayout()
        {
            if (_layoutApplied) return;
            _layoutApplied = true;

            this.TopLevel = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true; 
            this.BackColor = Color.White;

            this.Controls.Clear();

            // Root Table
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); 
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 

            // 1. Header
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };
            var headerTable = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1 };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var pnlTitle = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(33, 33, 33);
            lblTitle.AutoSize = true;

            lblRoomText.Font = new Font("Segoe UI", 10F);
            lblRoomText.ForeColor = Color.Gray;
            lblRoomText.AutoSize = true;

            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(lblRoomText);

            btnCloseTop.Text = "✕"; 
            btnCloseTop.Font = new Font("Arial", 14F, FontStyle.Regular);
            btnCloseTop.FlatStyle = FlatStyle.Flat;
            btnCloseTop.FlatAppearance.BorderSize = 0;
            btnCloseTop.ForeColor = Color.Gray;
            btnCloseTop.Size = new Size(40, 40);
            btnCloseTop.Cursor = Cursors.Hand;
            btnCloseTop.Click -= btnDong_Click;
            btnCloseTop.Click += btnDong_Click;

            headerTable.Controls.Add(pnlTitle, 0, 0);
            headerTable.Controls.Add(btnCloseTop, 1, 0);
            header.Controls.Add(headerTable);

            // 2. Content
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            var innerContent = new TableLayoutPanel 
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Padding = new Padding(0,0,15,0)
            };
            innerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Section 1: Luu Tru (Using IconHelper)
            var pnlLuuTruHeader = CreateSectionHeader("Thông tin lưu trú", IconHelper.CreateHomeIcon()); 
            var pnlLuuTruBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
            BuildLuuTruLayout(pnlLuuTruBody);

            // Section 2: Khach (Using IconHelper)
            var pnlKhachHeader = CreateSectionHeader("Thông tin khách", IconHelper.CreateUserIcon());
            
            var pnlKhachButtons = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5) };
            StyleButton(btnThemKhach, true); btnThemKhach.Width = 110;
            StyleButton(btnLamMoi, false); btnLamMoi.Width = 100;
            StyleButton(btnQuetMa, true); btnQuetMa.Width = 110;
            
            btnThemKhach.Dock = DockStyle.Left;
            var spacer = new Panel { Dock = DockStyle.Left, Width = 10 };
            btnLamMoi.Dock = DockStyle.Left;
            btnQuetMa.Dock = DockStyle.Right;

            pnlKhachButtons.Controls.Add(btnLamMoi);
            pnlKhachButtons.Controls.Add(spacer);
            pnlKhachButtons.Controls.Add(btnThemKhach);
            pnlKhachButtons.Controls.Add(btnQuetMa);

            var pnlKhachBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
            BuildKhachLayout(pnlKhachBody);

            // Section 3: List (Using IconHelper)
            var pnlListHeader = CreateSectionHeader("Danh sách khách", IconHelper.CreateListIcon());
            var pnlListBody = new Panel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(5) };
            lstKhach.Dock = DockStyle.Fill;
            lstKhach.BorderStyle = BorderStyle.FixedSingle;
            lstKhach.Font = new Font("Segoe UI", 10F);
            pnlListBody.Controls.Add(lstKhach);

            innerContent.Controls.Add(pnlLuuTruHeader, 0, 0);
            innerContent.Controls.Add(pnlLuuTruBody, 0, 1);
            innerContent.Controls.Add(pnlKhachHeader, 0, 2);
            innerContent.Controls.Add(pnlKhachButtons, 0, 3);
            innerContent.Controls.Add(pnlKhachBody, 0, 4);
            innerContent.Controls.Add(pnlListHeader, 0, 5);
            innerContent.Controls.Add(pnlListBody, 0, 6);
            contentPanel.Controls.Add(innerContent);

            // 3. Footer
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.WhiteSmoke
            };
            StyleButton(btnNhanPhong, true);
            btnNhanPhong.Width = 120;
            btnNhanPhong.Dock = DockStyle.Right;
            
            StyleButton(btnDong, false);
            btnDong.Width = 100;
            btnDong.Dock = DockStyle.Right;
            var footerSpacer = new Panel { Dock = DockStyle.Right, Width = 10 };

            footer.Controls.Add(btnNhanPhong);
            footer.Controls.Add(footerSpacer);
            footer.Controls.Add(btnDong);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(contentPanel, 0, 1);
            root.Controls.Add(footer, 0, 2);
            this.Controls.Add(root);
        }

        private Panel CreateSectionHeader(string title, Image icon)
        {
            var p = new Panel
            {
                Height = 36,
                BackColor = clrHeaderBg,
                Margin = new Padding(0, 10, 0, 0)
            };
            if (icon != null)
            {
                var pic = new PictureBox
                {
                    Image = icon,
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    Location = new Point(10, 6),
                    BackColor = Color.Transparent
                };
                p.Controls.Add(pic);
            }
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = clrHeaderText,
                AutoSize = true,
                Location = new Point(40, 8) 
            };
            p.Controls.Add(lbl);
            return p;
        }

        private void BuildLuuTruLayout(Panel container)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            for(int i=0; i<4; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            AddCell(tbl, lblNhanPhong, dtpNhanPhong, 0, 0);
            AddCell(tbl, lblTraPhong, dtpTraPhong, 1, 0);
            AddCell(tbl, lblLyDo, cboLyDoLuuTru, 2, 0, 2);

            AddCell(tbl, lblLoaiPhong, cboLoaiPhong, 0, 1);
            AddCell(tbl, lblPhong, cboPhong, 1, 1);
            AddCell(tbl, new Label { Text = "Loại giá :" }, cboLoaiGia, 2, 1);
            AddCell(tbl, lblGiaPhong, txtGiaPhong, 3, 1);

            AddCell(tbl, new Label { Text = "Ghi chú :" }, txtGhiChuLuuTru, 0, 2, 4);
            container.Controls.Add(tbl);
        }

        private void BuildKhachLayout(Panel container)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            for(int i=0; i<3; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            AddCell(tbl, lblHoTen, txtHoTen, 0, 0);
            AddCell(tbl, lblGioiTinh, cboGioiTinh, 1, 0);
            AddCell(tbl, lblNgaySinh, dtpNgaySinh, 2, 0);

            AddCell(tbl, lblDienThoai, txtDienThoai, 0, 1);
            AddCell(tbl, lblLoaiGiayTo, cboLoaiGiayTo, 1, 1);
            
            var pnlGiayTo = new Panel { Dock = DockStyle.Top, Height = 30, BorderStyle = BorderStyle.FixedSingle };
            var btnSearchGT = new Button 
            { 
                Dock = DockStyle.Right, 
                Width = 30, 
                Image = IconHelper.CreateSearchIcon(),
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.White, 
                Cursor = Cursors.Hand 
            };
            btnSearchGT.FlatAppearance.BorderSize = 0;
            txtSoGiayTo.BorderStyle = BorderStyle.None;
            txtSoGiayTo.Dock = DockStyle.Fill;
            var pnlTxtWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3,5,0,0), BackColor = Color.White };
            pnlTxtWrapper.Controls.Add(txtSoGiayTo);
            pnlGiayTo.Controls.Add(pnlTxtWrapper);
            pnlGiayTo.Controls.Add(btnSearchGT);

            var pCellGT = CreateCellPanel(lblSoGiayTo, pnlGiayTo);
            tbl.Controls.Add(pCellGT, 2, 1);

            AddCell(tbl, lblQuocTich, cboQuocTich, 0, 2);
            AddCell(tbl, lblGhiChuKhach, txtGhiChuKhach, 1, 2, 2);

            var pnlCuTru = CreateRadioPanel(rdoThuongTru, rdoTamTru, rdoNoiKhac);
            AddCell(tbl, lblNoiCuTru, pnlCuTru, 0, 3, 3);

            var pnlDiaBan = CreateRadioPanel(rdoDiaBanMoi, rdoDiaBanCu);
            AddCell(tbl, lblLoaiDiaBan, pnlDiaBan, 0, 4, 3);

            AddCell(tbl, lblTinhThanh, cboTinhThanh, 0, 5);
            AddCell(tbl, lblPhuongXa, cboPhuongXa, 1, 5);
            AddCell(tbl, lblDiaChiChiTiet, txtDiaChiChiTiet, 2, 5);

            AddCell(tbl, lblNgheNghiep, cboNgheNghiep, 0, 6);
            AddCell(tbl, lblNoiLamViec, txtNoiLamViec, 1, 6, 2);

            container.Controls.Add(tbl);
        }

        private Panel CreateCellPanel(Control label, Control input)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5, 5, 5, 10), AutoSize = true };
            label.Dock = DockStyle.Top;
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9F);
            label.ForeColor = Color.Black;
            label.Padding = new Padding(0, 0, 0, 4);
            if (label.Text.Contains("*")) label.ForeColor = Color.Black; 

            input.Dock = DockStyle.Top;
            if (!(input is Panel) && !(input is FlowLayoutPanel)) StyleControl(input);
            p.Controls.Add(input);
            p.Controls.Add(label);
            return p;
        }

        private void AddCell(TableLayoutPanel tbl, Control lbl, Control ctrl, int col, int row, int colSpan = 1)
        {
            var p = CreateCellPanel(lbl, ctrl);
            tbl.Controls.Add(p, col, row);
            if (colSpan > 1) tbl.SetColumnSpan(p, colSpan);
        }

        private Panel CreateRadioPanel(params RadioButton[] radios)
        {
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 5, 0, 0)
            };
            foreach (var r in radios)
            {
                r.AutoSize = true;
                r.Margin = new Padding(0, 0, 20, 0);
                r.Font = new Font("Segoe UI", 9.5f);
                p.Controls.Add(r);
            }
            return p;
        }

        private void StyleControl(Control c)
        {
            c.Font = new Font("Segoe UI", 10F);
            if (c is TextBox txt) 
            { 
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Height = 30;
                if (txt.Multiline) txt.Height = 60;
            }
            else if (c is ComboBox cb) { cb.FlatStyle = FlatStyle.System; cb.Height = 30; }
            else if (c is DateTimePicker dt) { dt.Height = 30; }
        }

        private void StyleButton(Button b, bool primary)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = clrPrimary;
            b.BackColor = primary ? clrPrimary : Color.White;
            b.ForeColor = primary ? Color.White : clrPrimary;
            b.Cursor = Cursors.Hand;
            b.Height = 36;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        }

        private void InitCombos()
        {
            cboLyDoLuuTru.Items.Clear();
            cboLyDoLuuTru.Items.AddRange(new object[] { "-- Chọn --", "Du lịch", "Công tác", "Thăm thân", "Khác" });
            cboLyDoLuuTru.SelectedIndex = 0;
            cboLoaiGia.Items.Clear();
            cboLoaiGia.Items.AddRange(new object[] { "Mặc định", "Theo giờ", "Theo ngày", "Khác" });
            cboLoaiGia.SelectedIndex = 0;
            cboGioiTinh.Items.Clear();
            cboGioiTinh.Items.AddRange(new object[] { "-- Chọn --", "Nam", "Nữ", "Khác" });
            cboGioiTinh.SelectedIndex = 0;
            cboLoaiGiayTo.Items.Clear();
            cboLoaiGiayTo.Items.AddRange(new object[] { "Thẻ CCCD", "CMND", "Hộ chiếu" });
            cboLoaiGiayTo.SelectedIndex = 0;
            cboQuocTich.Items.Clear();
            cboQuocTich.Items.AddRange(new object[] { "VNM - Việt Nam","USA - United States","KOR - Korea","JPN - Japan","CHN - China", "OTHER" });
            cboQuocTich.SelectedIndex = 0;
            cboTinhThanh.Items.Clear();
            cboTinhThanh.Items.AddRange(new object[] { "Chọn Tỉnh/Thành", "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Cần Thơ", "Khác" });
            cboTinhThanh.SelectedIndex = 0;
            cboPhuongXa.Items.Clear();
            cboPhuongXa.Items.AddRange(new object[] { "Chọn Phường/Xã", "Phường 1", "Phường 2", "Xã A", "Xã B" });
            cboPhuongXa.SelectedIndex = 0;
            cboNgheNghiep.Items.Clear();
            cboNgheNghiep.Items.AddRange(new object[] { "-- Chọn --", "Công nhân", "Nhân viên VP", "Tự do", "Học sinh/SV" });
            cboNgheNghiep.SelectedIndex = 0;
            rdoThuongTru.Checked = true;
            rdoDiaBanMoi.Checked = true;
            if (dtpNgaySinh.Value.Year < 1900) dtpNgaySinh.Value = new DateTime(1990, 1, 1);
        }

        private void BindRoomInfo()
        {
            cboLoaiPhong.Items.Clear();
            cboLoaiPhong.Items.Add(_room.LoaiPhongID == 1 ? "Phòng Đơn" : _room.LoaiPhongID == 2 ? "Phòng Đôi" : "Phòng");
            cboLoaiPhong.SelectedIndex = 0;
            cboLoaiPhong.Enabled = false;
            cboPhong.Items.Clear();
            cboPhong.Items.Add("Phòng " + _room.MaPhong);
            cboPhong.SelectedIndex = 0;
            cboPhong.Enabled = false;
            try
            {
                decimal gia = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
                if (gia <= 0) gia = (_room.LoaiPhongID == 1) ? 250000m : 350000m;
                txtGiaPhong.Text = gia.ToString("N0");
            }
            catch { txtGiaPhong.Text = "0"; }
        }

        private void btnDong_Click(object sender, EventArgs e) { BackRequested?.Invoke(this, EventArgs.Empty); }
        private void btnQuetMa_Click(object sender, EventArgs e) { MessageBox.Show("Chức năng quét mã sẽ được bổ sung sau.", "Thông báo"); }

        private void btnNhanPhong_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            string tenChinh = GetPrimaryGuestName();
            _room.TrangThai = 1; 
            _room.KieuThue = (_room.KieuThue.HasValue && _room.KieuThue.Value > 0) ? _room.KieuThue : 1;
            _room.ThoiGianBatDau = dtpNhanPhong.Value;
            _room.TenKhachHienThi = tenChinh;
            string ghiChu = BuildGhiChuForSave();
            _roomDal.UpdateTrangThaiFull(_room.PhongID, _room.TrangThai, ghiChu, _room.ThoiGianBatDau, _room.KieuThue, _room.TenKhachHienThi);
            _room.GhiChu = ghiChu;
            Saved?.Invoke(this, EventArgs.Empty);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateForm()
        {
            if (cboLyDoLuuTru.SelectedIndex <= 0) { MessageBox.Show("Vui lòng chọn Lý do lưu trú.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); cboLyDoLuuTru.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(GetPrimaryGuestName())) { MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtHoTen.Focus(); return false; }
            return true;
        }

        private string GetPrimaryGuestName()
        {
            if (lstKhach.Items.Count > 0)
            {
                string item = lstKhach.Items[0]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(item))
                {
                    int idx = item.IndexOf(" - ", StringComparison.Ordinal);
                    return idx > 0 ? item.Substring(0, idx).Trim() : item.Trim();
                }
            }
            return (txtHoTen.Text ?? "").Trim();
        }

        private void btnThemKhach_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text)) return;
            string display = $"{txtHoTen.Text.Trim()} - {txtSoGiayTo.Text.Trim()}";
            lstKhach.Items.Add(display);
            txtHoTen.SelectAll();
            txtHoTen.Focus();
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            txtHoTen.Text = ""; cboGioiTinh.SelectedIndex = 0; dtpNgaySinh.Value = new DateTime(1990, 1, 1);
            txtDienThoai.Text = ""; txtSoGiayTo.Text = ""; txtGhiChuKhach.Text = "";
            cboNgheNghiep.SelectedIndex = 0; txtNoiLamViec.Text = "";
            cboTinhThanh.SelectedIndex = 0; cboPhuongXa.SelectedIndex = 0; txtDiaChiChiTiet.Text = "";
        }

        private string BuildGhiChuForSave()
        {
            var tags = new List<string>();
            tags.Add("LYDO=" + SafeTagValue(cboLyDoLuuTru.SelectedItem?.ToString()));
            tags.Add("GT=" + SafeTagValue(cboGioiTinh.SelectedItem?.ToString()));
            tags.Add("NS=" + dtpNgaySinh.Value.ToString("yyyyMMdd"));
            tags.Add("LGT=" + SafeTagValue(cboLoaiGiayTo.SelectedItem?.ToString()));
            tags.Add("SGT=" + SafeTagValue(txtSoGiayTo.Text.Trim()));
            tags.Add("QT=" + SafeTagValue(cboQuocTich.SelectedItem?.ToString()));
            tags.Add("SDT=" + SafeTagValue(txtDienThoai.Text));
            tags.Add("JOB=" + SafeTagValue(cboNgheNghiep.SelectedItem?.ToString()));
            tags.Add("WORK=" + SafeTagValue(txtNoiLamViec.Text));
            if (!string.IsNullOrWhiteSpace(txtGhiChuLuuTru.Text)) tags.Add("NOTE_S=" + SafeTagValue(txtGhiChuLuuTru.Text));
            if (!string.IsNullOrWhiteSpace(txtGhiChuKhach.Text)) tags.Add("NOTE_G=" + SafeTagValue(txtGhiChuKhach.Text));
            if (dtpTraPhong.Checked) tags.Add("TRAP=" + dtpTraPhong.Value.ToString("yyyyMMdd"));
            if (lstKhach.Items.Count > 0)
            {
                var guests = new List<string>();
                foreach (var it in lstKhach.Items) {
                    if (it == null) continue;
                    string s = it.ToString().Trim();
                    if (s.Length > 0) guests.Add(SafeTagValue(s));
                }
                if (guests.Count > 0) tags.Add("DSK=" + string.Join(";", guests));
            }
            return string.Join(" | ", tags);
        }

        private static string SafeTagValue(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "";
            s = s.Replace("|", "/");
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static string GetStringTag(string text, string key, string defaultVal)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            Match m = Regex.Match(text, @"\b" + Regex.Escape(key) + @"\s*=\s*([^|]+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();
            return defaultVal;
        }

        private void LoadStateFromGhiChu(string ghiChu)
        {
            if (string.IsNullOrWhiteSpace(ghiChu)) return;
            SelectComboByText(cboLyDoLuuTru, GetStringTag(ghiChu, "LYDO", ""));
            SelectComboByText(cboGioiTinh, GetStringTag(ghiChu, "GT", ""));
            string ns = GetStringTag(ghiChu, "NS", "");
            if (ns.Length == 8 && DateTime.TryParseExact(ns, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob)) dtpNgaySinh.Value = dob;
            txtSoGiayTo.Text = GetStringTag(ghiChu, "SGT", "");
            txtDienThoai.Text = GetStringTag(ghiChu, "SDT", "");
            txtNoiLamViec.Text = GetStringTag(ghiChu, "WORK", "");
            SelectComboByText(cboNgheNghiep, GetStringTag(ghiChu, "JOB", ""));
            txtGhiChuLuuTru.Text = GetStringTag(ghiChu, "NOTE_S", "");
            txtGhiChuKhach.Text = GetStringTag(ghiChu, "NOTE_G", "");
            string dsk = GetStringTag(ghiChu, "DSK", "");
            if (!string.IsNullOrWhiteSpace(dsk)) {
                var parts = dsk.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) lstKhach.Items.Add(p.Trim());
            }
        }

        private static void SelectComboByText(ComboBox cbo, string text)
        {
            if (cbo == null || cbo.Items.Count == 0 || string.IsNullOrWhiteSpace(text)) return;
            for (int i = 0; i < cbo.Items.Count; i++) {
                string it = cbo.Items[i]?.ToString() ?? "";
                if (string.Equals(it, text, StringComparison.OrdinalIgnoreCase)) { cbo.SelectedIndex = i; return; }
            }
        }
    }

    // IconHelper class definition
    public static class IconHelper
    {
        private static readonly Color PrimaryColor = Color.FromArgb(25, 118, 210);

        public static Bitmap CreateHomeIcon(int size = 24)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    Point[] roof = { new Point(size / 2, 2), new Point(2, size / 2 + 2), new Point(size - 2, size / 2 + 2) };
                    g.FillPolygon(brush, roof);
                    g.FillRectangle(brush, 5, size / 2 + 2, size - 10, size / 2 - 2);
                }
            }
            return bmp;
        }

        public static Bitmap CreateUserIcon(int size = 24)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    int headSize = size / 2 - 2;
                    g.FillEllipse(brush, (size - headSize) / 2, 2, headSize, headSize);
                    int bodyH = size / 2;
                    g.FillPie(brush, 2, size - bodyH - 2, size - 4, bodyH * 2, 180, 180);
                }
            }
            return bmp;
        }

        public static Bitmap CreateListIcon(int size = 24)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    int h = 4; int gap = 3; int y = 4;
                    g.FillRectangle(brush, 4, y, size - 8, h);
                    g.FillRectangle(brush, 4, y + h + gap, size - 8, h);
                    g.FillRectangle(brush, 4, y + (h + gap) * 2, size - 12, h);
                }
            }
            return bmp;
        }

        public static Bitmap CreateSearchIcon(int size = 16)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.Gray, 2))
                {
                    g.DrawEllipse(pen, 2, 2, 8, 8);
                    g.DrawLine(pen, 9, 9, 13, 13);
                }
            }
            return bmp;
        }
    }
}
