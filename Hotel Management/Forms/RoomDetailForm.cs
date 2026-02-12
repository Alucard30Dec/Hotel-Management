using System;
using System.Collections.Generic;
using System.Drawing;
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
            // Format title text
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

            // ===== ROOT LAYOUT =====
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Content (Scrollable)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Footer

            // ===== 1. HEADER =====
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 10),
                Margin = new Padding(0)
            };
            var headerFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Dock = DockStyle.Left
            };
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 4);
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            
            lblRoomText.AutoSize = true;
            lblRoomText.ForeColor = Color.Gray;
            
            headerFlow.Controls.Add(lblTitle);
            headerFlow.Controls.Add(lblRoomText);

            btnCloseTop.Text = "✕"; 
            btnCloseTop.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            btnCloseTop.FlatStyle = FlatStyle.Flat;
            btnCloseTop.FlatAppearance.BorderSize = 0;
            btnCloseTop.ForeColor = Color.Gray;
            btnCloseTop.Size = new Size(40, 40);
            btnCloseTop.Dock = DockStyle.Right;
            btnCloseTop.Cursor = Cursors.Hand;
            btnCloseTop.Click -= btnDong_Click;
            btnCloseTop.Click += btnDong_Click;

            header.Controls.Add(headerFlow);
            header.Controls.Add(btnCloseTop);

            // ===== 2. CONTENT =====
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 5, 0, 5)
            };
            
            var innerContent = new TableLayoutPanel 
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                AutoSizeMode = AutoSizeMode.GrowAndShrink 
            };
            innerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Section 1: Thông tin lưu trú
            var pnlLuuTruHeader = CreateSectionHeader("Thông tin lưu trú", "header_home.png"); // Placeholder icon name
            pnlLuuTruHeader.Dock = DockStyle.Top;
            
            var pnlLuuTruBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            BuildLuuTruLayout(pnlLuuTruBody);

            // Section 2: Thông tin khách
            var pnlKhachHeader = CreateSectionHeader("Thông tin khách", "header_user.png");
            pnlKhachHeader.Dock = DockStyle.Top;
            
            var pnlKhachButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 5, 10, 5)
            };
            StyleButton(btnQuetMa, true);
            StyleButton(btnLamMoi, false);
            StyleButton(btnThemKhach, true);
            pnlKhachButtons.Controls.Add(btnQuetMa);
            pnlKhachButtons.Controls.Add(btnLamMoi);
            pnlKhachButtons.Controls.Add(btnThemKhach);

            var pnlKhachBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            BuildKhachLayout(pnlKhachBody);

            // Section 3: Danh sách khách
            var pnlListHeader = CreateSectionHeader("Danh sách khách", "header_list.png");
            pnlListHeader.Dock = DockStyle.Top;
            
            var pnlListBody = new Panel { Dock = DockStyle.Top, Height = 150, Padding = new Padding(10) };
            lstKhach.Dock = DockStyle.Fill;
            lstKhach.BorderStyle = BorderStyle.FixedSingle;
            pnlListBody.Controls.Add(lstKhach);


            innerContent.Controls.Add(pnlLuuTruHeader, 0, 0);
            innerContent.Controls.Add(pnlLuuTruBody, 0, 1);
            innerContent.Controls.Add(pnlKhachHeader, 0, 2);
            innerContent.Controls.Add(pnlKhachButtons, 0, 3);
            innerContent.Controls.Add(pnlKhachBody, 0, 4);
            innerContent.Controls.Add(pnlListHeader, 0, 5);
            innerContent.Controls.Add(pnlListBody, 0, 6);
            
            contentPanel.Controls.Add(innerContent);

            // ===== 3. FOOTER =====
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0)
            };
            StyleButton(btnNhanPhong, true);
            btnNhanPhong.AutoSize = true;
            btnNhanPhong.Margin = new Padding(10, 0, 0, 0);
            
            StyleButton(btnDong, false);
            btnDong.AutoSize = true;

            footer.Controls.Add(btnNhanPhong);
            footer.Controls.Add(btnDong);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(contentPanel, 0, 1);
            root.Controls.Add(footer, 0, 2);

            this.Controls.Add(root);
        }

        private Panel CreateSectionHeader(string title, string icon)
        {
            var p = new Panel
            {
                Height = 35,
                BackColor = Color.FromArgb(232, 241, 255), // Light blue background
            };
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210),
                AutoSize = true,
                Location = new Point(10, 8)
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
                ColumnCount = 4, // 4 columns for dense layout
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            // Columns percentages: 15% - 35% - 15% - 35%
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            // Row 1: NhanPhong | TraPhong (with Checkbox) | LyDo (Span 2 cols if needed, or split)
            // Let's do 3 groups on one line visually in the image, but table layout is grid.
            // Image: [NhanPhong] [TraPhong] [LyDo]
            // We can use nested flow/tables or just a 6-column grid. Let's try 6 columns.
            tbl.ColumnCount = 6;
            tbl.ColumnStyles.Clear();
            for(int i=0; i<6; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6f));

            // Helpers to add control with label
            AddControlGroup(tbl, lblNhanPhong, dtpNhanPhong, 0, 0, 2);
            AddControlGroup(tbl, lblTraPhong, dtpTraPhong, 2, 0, 2);
            AddControlGroup(tbl, lblLyDo, cboLyDoLuuTru, 4, 0, 2);

            AddControlGroup(tbl, lblLoaiPhong, cboLoaiPhong, 0, 1, 2);
            AddControlGroup(tbl, lblPhong, cboPhong, 2, 1, 2);
            
            // Gia Phong Group (Price Type + Value)
            var pnlGia = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount=2, Margin=new Padding(0) };
            pnlGia.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            pnlGia.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            StyleControl(cboLoaiGia);
            StyleControl(txtGiaPhong);
            pnlGia.Controls.Add(cboLoaiGia, 0, 0);
            pnlGia.Controls.Add(txtGiaPhong, 1, 0);

            // Add Label for Price
            var lblGia = new Label { Text="Loại giá / Giá phòng:", AutoSize=true, Font=new Font("Segoe UI",9F) };
            // Actually image shows "Loai gia" and "Gia phong" as separate inputs on same row or separate.
            // Image: [LoaiPhong] [Phong] [LoaiGia] [GiaPhong]
            // Let's split row 2 into 4 items.
            // To fit 4 items in 6 columns is hard. Let's make it 4 columns grid for the whole table?
            // Image Row 1: Time In (Large), Time Out (Large), Reason (Small) -> Not equal.
            // Let's stick to standard flow within cells or just use specific Row/Col spans.
            
            // Re-doing Table for better match
            tbl.Controls.Clear();
            tbl.RowStyles.Clear();
            tbl.ColumnStyles.Clear();
            tbl.ColumnCount = 3;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));

            // Row 0
            AddCell(tbl, lblNhanPhong, dtpNhanPhong, 0, 0);
            AddCell(tbl, lblTraPhong, dtpTraPhong, 1, 0);
            AddCell(tbl, lblLyDo, cboLyDoLuuTru, 2, 0);

            // Row 1
            // Need 4 items in 3 columns? [LoaiPhong][Phong] [LoaiGia][GiaPhong]
            // Let's put LoaiGia and GiaPhong in column 3? Or make a 4 column row.
            // Simplified: Row 1 has 3 items. Row 2 has 3 items (LoaiPhong, Phong, PriceGroup)
            AddCell(tbl, lblLoaiPhong, cboLoaiPhong, 0, 1);
            AddCell(tbl, lblPhong, cboPhong, 1, 1);
            
            // Price Group combined
            var pnlPrice = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents=false, AutoSize=true, Margin=new Padding(0) };
            var p1 = new Panel { Width = 100, Height = 50 }; 
            var l1 = new Label { Text="Loại giá:", AutoSize=true };
            cboLoaiGia.Width = 95;
            p1.Controls.Add(cboLoaiGia); cboLoaiGia.Top = 20; p1.Controls.Add(l1);
            
            var p2 = new Panel { Width = 120, Height = 50 };
            var l2 = new Label { Text="Giá phòng:", AutoSize=true };
            txtGiaPhong.Width = 115;
            p2.Controls.Add(txtGiaPhong); txtGiaPhong.Top = 20; p2.Controls.Add(l2);

            pnlPrice.Controls.Add(p1);
            pnlPrice.Controls.Add(p2);
            // tbl.Controls.Add(pnlPrice, 2, 1); // This is messy.

            // Clean approach: Just labels and inputs
            // Let's use 4 columns for the whole form, Row 1 spans cols.
            tbl.ColumnCount = 4;
            tbl.ColumnStyles.Clear();
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            
            // Row 0: NhanPhong (span 1), TraPhong (span 1), LyDo (span 2)
            AddCell(tbl, lblNhanPhong, dtpNhanPhong, 0, 0);
            AddCell(tbl, lblTraPhong, dtpTraPhong, 1, 0);
            AddCell(tbl, lblLyDo, cboLyDoLuuTru, 2, 0, 2);

            // Row 1: LoaiPhong, Phong, LoaiGia, GiaPhong
            AddCell(tbl, lblLoaiPhong, cboLoaiPhong, 0, 1);
            AddCell(tbl, lblPhong, cboPhong, 1, 1);
            
            var lblLoaiGia = new Label { Text = "Loại giá:", AutoSize = true };
            AddCell(tbl, lblLoaiGia, cboLoaiGia, 2, 1);
            AddCell(tbl, lblGiaPhong, txtGiaPhong, 3, 1);

            // Row 2: Ghi Chu (Span all)
            var lblGC = new Label { Text = "Ghi chú:", AutoSize = true };
            AddCell(tbl, lblGC, txtGhiChuLuuTru, 0, 2, 4);

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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));

            // Row 0: HoTen, GioiTinh, NgaySinh
            AddCell(tbl, lblHoTen, txtHoTen, 0, 0);
            AddCell(tbl, lblGioiTinh, cboGioiTinh, 1, 0);
            AddCell(tbl, lblNgaySinh, dtpNgaySinh, 2, 0);

            // Row 1: DienThoai, LoaiGiayTo, SoGiayTo
            AddCell(tbl, lblDienThoai, txtDienThoai, 0, 1);
            AddCell(tbl, lblLoaiGiayTo, cboLoaiGiayTo, 1, 1);
            AddCell(tbl, lblSoGiayTo, txtSoGiayTo, 2, 1); // Should have icon but keep simple

            // Row 2: QuocTich, GhiChu (Span 2)
            AddCell(tbl, lblQuocTich, cboQuocTich, 0, 2);
            AddCell(tbl, lblGhiChuKhach, txtGhiChuKhach, 1, 2, 2);

            // Row 3: NoiCuTru (Span 3)
            var pnlNoiCuTru = CreateRadioPanel(rdoThuongTru, rdoTamTru, rdoNoiKhac);
            AddCell(tbl, lblNoiCuTru, pnlNoiCuTru, 0, 3, 3);

            // Row 4: LoaiDiaBan (Span 3)
            var pnlDiaBan = CreateRadioPanel(rdoDiaBanMoi, rdoDiaBanCu);
            AddCell(tbl, lblLoaiDiaBan, pnlDiaBan, 0, 4, 3);

            // Row 5: Dia Ban Moi Panel (City, District, Address)
            // Visually visually separated in image.
            var pnlDiaBanGroup = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = Color.FromArgb(245,245,245), Padding=new Padding(5) };
            // We can just add rows to table but give them background? Table doesn't support row bg easily.
            // Let's just continue in table.
            
            AddCell(tbl, lblTinhThanh, cboTinhThanh, 0, 5);
            AddCell(tbl, lblPhuongXa, cboPhuongXa, 1, 5);
            AddCell(tbl, lblDiaChiChiTiet, txtDiaChiChiTiet, 2, 5);

            // Row 6: Nghe Nghiep, Noi Lam Viec
            AddCell(tbl, lblNgheNghiep, cboNgheNghiep, 0, 6);
            AddCell(tbl, lblNoiLamViec, txtNoiLamViec, 1, 6, 2);

            container.Controls.Add(tbl);
        }

        private void AddCell(TableLayoutPanel tbl, Control lbl, Control ctrl, int col, int row, int colSpan = 1)
        {
            // Panel to hold Label top and Control bottom
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5), Height = 55 }; // Fixed height for uniformity
            
            lbl.Dock = DockStyle.Top;
            lbl.Height = 18;
            lbl.Font = new Font("Segoe UI", 9F);
            
            // Add Red asterisk if text contains *
            if (lbl.Text.Contains("*"))
            {
                // Simple color change not possible in partial text for standard label
                // Just keep it simple
                lbl.ForeColor = Color.Black; 
            }

            ctrl.Dock = DockStyle.Top;
            ctrl.Height = 28;
            StyleControl(ctrl);

            p.Controls.Add(ctrl);
            p.Controls.Add(lbl);

            tbl.Controls.Add(p, col, row);
            if (colSpan > 1) tbl.SetColumnSpan(p, colSpan);
        }
        
        private void AddControlGroup(TableLayoutPanel tbl, Control label, Control input, int col, int row, int span=1)
        {
            AddCell(tbl, label, input, col, row, span);
        }

        private void StyleControl(Control c)
        {
            if (c is TextBox txt) 
            { 
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Height = 30;
                // If textarea
                if (txt.Multiline) txt.Height = 60;
            }
            else if (c is ComboBox cb) 
            { 
                cb.FlatStyle = FlatStyle.System;
                cb.Height = 30; 
            }
            else if (c is DateTimePicker dt) 
            { 
                dt.Height = 30;
            }
        }

        private void StyleButton(Button b, bool primary)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(33, 150, 243);
            b.BackColor = primary ? Color.FromArgb(33, 150, 243) : Color.White;
            b.ForeColor = primary ? Color.White : Color.FromArgb(33, 150, 243);
            b.Cursor = Cursors.Hand;
            b.Height = 32;
            b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        private Panel CreateRadioPanel(params RadioButton[] radios)
        {
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, // Changed from Fill
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0)
            };
            foreach (var r in radios)
            {
                r.AutoSize = true;
                r.Margin = new Padding(0, 5, 15, 5);
                p.Controls.Add(r);
            }
            return p;
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
            cboQuocTich.Items.AddRange(new object[]
            {
                "VNM - Việt Nam","USA - United States","KOR - Korea","JPN - Japan","CHN - China",
                "OTHER"
            });
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

        private void btnDong_Click(object sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnNhanPhong_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            string tenChinh = GetPrimaryGuestName();

            _room.TrangThai = 1; 
            _room.KieuThue = (_room.KieuThue.HasValue && _room.KieuThue.Value > 0) ? _room.KieuThue : 1;
            _room.ThoiGianBatDau = dtpNhanPhong.Value;
            _room.TenKhachHienThi = tenChinh;

            string ghiChu = BuildGhiChuForSave();
            
            _roomDal.UpdateTrangThaiFull(
                _room.PhongID,
                _room.TrangThai,
                ghiChu,
                _room.ThoiGianBatDau,
                _room.KieuThue,
                _room.TenKhachHienThi
            );

            _room.GhiChu = ghiChu;

            Saved?.Invoke(this, EventArgs.Empty);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateForm()
        {
            if (cboLyDoLuuTru.SelectedIndex <= 0)
            {
                MessageBox.Show("Vui lòng chọn Lý do lưu trú.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboLyDoLuuTru.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(GetPrimaryGuestName()))
            {
                MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHoTen.Focus();
                return false;
            }
            // Basic validation
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
            txtHoTen.Text = "";
            cboGioiTinh.SelectedIndex = 0;
            dtpNgaySinh.Value = new DateTime(1990, 1, 1);
            txtDienThoai.Text = "";
            txtSoGiayTo.Text = "";
            txtGhiChuKhach.Text = "";
            cboNgheNghiep.SelectedIndex = 0;
            txtNoiLamViec.Text = "";
            // Reset address
            cboTinhThanh.SelectedIndex = 0;
            txtDiaChiChiTiet.Text = "";
        }

        private void btnQuetMa_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Chức năng quét mã sẽ được bổ sung sau.", "Thông báo");
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
            
            // Notes
            if (!string.IsNullOrWhiteSpace(txtGhiChuLuuTru.Text)) tags.Add("NOTE_S=" + SafeTagValue(txtGhiChuLuuTru.Text));
            if (!string.IsNullOrWhiteSpace(txtGhiChuKhach.Text)) tags.Add("NOTE_G=" + SafeTagValue(txtGhiChuKhach.Text));

            if (dtpTraPhong.Checked)
                tags.Add("TRAP=" + dtpTraPhong.Value.ToString("yyyyMMdd"));

            if (lstKhach.Items.Count > 0)
            {
                var guests = new List<string>();
                foreach (var it in lstKhach.Items)
                {
                    if (it == null) continue;
                    string s = it.ToString().Trim();
                    if (s.Length > 0) guests.Add(SafeTagValue(s));
                }
                if (guests.Count > 0)
                    tags.Add("DSK=" + string.Join(";", guests));
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
            if (ns.Length == 8 && DateTime.TryParseExact(ns, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                dtpNgaySinh.Value = dob;

            txtSoGiayTo.Text = GetStringTag(ghiChu, "SGT", "");
            txtDienThoai.Text = GetStringTag(ghiChu, "SDT", "");
            txtNoiLamViec.Text = GetStringTag(ghiChu, "WORK", "");
            SelectComboByText(cboNgheNghiep, GetStringTag(ghiChu, "JOB", ""));
            
            txtGhiChuLuuTru.Text = GetStringTag(ghiChu, "NOTE_S", "");
            txtGhiChuKhach.Text = GetStringTag(ghiChu, "NOTE_G", "");

            string dsk = GetStringTag(ghiChu, "DSK", "");
            if (!string.IsNullOrWhiteSpace(dsk))
            {
                var parts = dsk.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) lstKhach.Items.Add(p.Trim());
            }
        }

        private static void SelectComboByText(ComboBox cbo, string text)
        {
            if (cbo == null || cbo.Items.Count == 0 || string.IsNullOrWhiteSpace(text)) return;
            for (int i = 0; i < cbo.Items.Count; i++)
            {
                string it = cbo.Items[i]?.ToString() ?? "";
                if (string.Equals(it, text, StringComparison.OrdinalIgnoreCase)) { cbo.SelectedIndex = i; return; }
            }
        }
    }
}
