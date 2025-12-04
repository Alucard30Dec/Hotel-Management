using System.Windows.Forms;
using System.Drawing;

namespace HotelManagement.Forms
{
    partial class RoomDetailForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblTitle;
        private Label lblRoomCode;
        private Label lblRoomType;
        private Label lblFloor;
        private Button btnBack;

        private Label lblCurrentStatus;
        private Label lblCurrentStatusDesc;
        private Button btnStatusTrong;
        private Button btnStatusCoKhach;
        private Button btnStatusChuaDon;
        private Button btnStatusDaDat;

        private Label lblGhiChu;
        private TextBox txtGhiChu;

        private GroupBox grpThue;
        private RadioButton rdoDem;
        private RadioButton rdoNgay;
        private RadioButton rdoGio;
        private Label lblSoLuong;
        private NumericUpDown nudSoLuong;

        private GroupBox grpKhach;
        private Label lblTenKhach;
        private TextBox txtTenKhach;
        private Label lblCCCD;
        private TextBox txtCCCD;
        private Button btnChonAnh;
        private PictureBox picCCCD;

        private GroupBox grpDichVu;
        private Label lblNuocNgot;
        private Label lblNuocSuoi;
        private NumericUpDown nudNuocNgot;
        private NumericUpDown nudNuocSuoi;
        private Label lblNuocNgotGia;
        private Label lblNuocSuoiGia;

        private GroupBox grpThanhTien;
        private Label labelTienPhong;
        private Label labelTienDichVu;
        private Label labelDaThu;
        private Label labelTongTien;
        private Label labelGoiYThu;
        private Label lblTienPhong;
        private Label lblTienDichVu;
        private Label lblTienDaThu;
        private Label lblTongTien;
        private TextBox txtTienDaThu;
        private Button btnGoiY1;
        private Button btnGoiY2;
        private Button btnGoiY3;

        private Label lblStartCaption;
        private Label lblEndCaption;
        private Label lblDurationCaption;
        private Label lblStartTime;
        private Label lblEndTime;
        private Label lblDuration;

        private Button btnTinhTien;
        private Button btnLuu;
        private Button btnHuy;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.lblTitle = new Label();
            this.lblRoomCode = new Label();
            this.lblRoomType = new Label();
            this.lblFloor = new Label();
            this.btnBack = new Button();

            this.lblCurrentStatus = new Label();
            this.lblCurrentStatusDesc = new Label();
            this.btnStatusTrong = new Button();
            this.btnStatusCoKhach = new Button();
            this.btnStatusChuaDon = new Button();
            this.btnStatusDaDat = new Button();

            this.lblGhiChu = new Label();
            this.txtGhiChu = new TextBox();

            this.grpThue = new GroupBox();
            this.rdoDem = new RadioButton();
            this.rdoNgay = new RadioButton();
            this.rdoGio = new RadioButton();
            this.lblSoLuong = new Label();
            this.nudSoLuong = new NumericUpDown();

            this.grpKhach = new GroupBox();
            this.lblTenKhach = new Label();
            this.txtTenKhach = new TextBox();
            this.lblCCCD = new Label();
            this.txtCCCD = new TextBox();
            this.btnChonAnh = new Button();
            this.picCCCD = new PictureBox();

            this.grpDichVu = new GroupBox();
            this.lblNuocNgot = new Label();
            this.lblNuocSuoi = new Label();
            this.nudNuocNgot = new NumericUpDown();
            this.nudNuocSuoi = new NumericUpDown();
            this.lblNuocNgotGia = new Label();
            this.lblNuocSuoiGia = new Label();

            this.grpThanhTien = new GroupBox();
            this.labelTienPhong = new Label();
            this.labelTienDichVu = new Label();
            this.labelDaThu = new Label();
            this.labelTongTien = new Label();
            this.labelGoiYThu = new Label();
            this.lblTienPhong = new Label();
            this.lblTienDichVu = new Label();
            this.lblTienDaThu = new Label();
            this.lblTongTien = new Label();
            this.txtTienDaThu = new TextBox();
            this.btnGoiY1 = new Button();
            this.btnGoiY2 = new Button();
            this.btnGoiY3 = new Button();

            this.lblStartCaption = new Label();
            this.lblEndCaption = new Label();
            this.lblDurationCaption = new Label();
            this.lblStartTime = new Label();
            this.lblEndTime = new Label();
            this.lblDuration = new Label();

            this.btnTinhTien = new Button();
            this.btnLuu = new Button();
            this.btnHuy = new Button();

            ((System.ComponentModel.ISupportInitialize)(this.nudSoLuong)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocNgot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocSuoi)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCCCD)).BeginInit();

            this.SuspendLayout();

            // Form
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 580);
            this.BackColor = Color.White;
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Load += new System.EventHandler(this.RoomDetailForm_Load);

            // Back
            this.btnBack.Text = "◀ Quay lại";
            this.btnBack.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnBack.BackColor = Color.Transparent;
            this.btnBack.FlatStyle = FlatStyle.Flat;
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.ForeColor = Color.FromArgb(63, 81, 181);
            this.btnBack.Location = new Point(10, 10);
            this.btnBack.Size = new Size(100, 30);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

            // Title
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(63, 81, 181);
            this.lblTitle.Text = "Thông tin phòng";
            this.lblTitle.Location = new Point(130, 10);

            // Room code
            this.lblRoomCode.AutoSize = true;
            this.lblRoomCode.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
            this.lblRoomCode.ForeColor = Color.FromArgb(33, 33, 33);
            this.lblRoomCode.Location = new Point(130, 40);

            // Room type & floor
            this.lblRoomType.AutoSize = true;
            this.lblRoomType.Font = new Font("Segoe UI", 10F);
            this.lblRoomType.ForeColor = Color.Gray;
            this.lblRoomType.Location = new Point(130, 80);

            this.lblFloor.AutoSize = true;
            this.lblFloor.Font = new Font("Segoe UI", 10F);
            this.lblFloor.ForeColor = Color.Gray;
            this.lblFloor.Location = new Point(230, 80);

            // Trạng thái label
            this.lblCurrentStatus.AutoSize = true;
            this.lblCurrentStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblCurrentStatus.Location = new Point(430, 15);
            this.lblCurrentStatus.Text = "Trạng thái phòng";

            this.lblCurrentStatusDesc.AutoSize = true;
            this.lblCurrentStatusDesc.Font = new Font("Segoe UI", 10F);
            this.lblCurrentStatusDesc.ForeColor = Color.Gray;
            this.lblCurrentStatusDesc.Location = new Point(430, 35);
            this.lblCurrentStatusDesc.Text = "Trống";

            int stTop = 60;
            int stWidth = 110;
            int stHeight = 40;
            int stLeft = 430;
            int stSpacing = 10;

            // Nút Trống
            this.btnStatusTrong.Text = "Trống";
            this.btnStatusTrong.Location = new Point(stLeft, stTop);
            this.btnStatusTrong.Size = new Size(stWidth, stHeight);
            this.btnStatusTrong.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.btnStatusTrong.Click += new System.EventHandler(this.btnStatusTrong_Click);

            // Nút Có khách
            this.btnStatusCoKhach.Text = "Có khách";
            this.btnStatusCoKhach.Location = new Point(stLeft + (stWidth + stSpacing), stTop);
            this.btnStatusCoKhach.Size = new Size(stWidth, stHeight);
            this.btnStatusCoKhach.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.btnStatusCoKhach.Click += new System.EventHandler(this.btnStatusCoKhach_Click);

            // Nút Chưa dọn
            this.btnStatusChuaDon.Text = "Chưa dọn";
            this.btnStatusChuaDon.Location = new Point(stLeft + 2 * (stWidth + stSpacing), stTop);
            this.btnStatusChuaDon.Size = new Size(stWidth, stHeight);
            this.btnStatusChuaDon.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.btnStatusChuaDon.Click += new System.EventHandler(this.btnStatusChuaDon_Click);

            // Nút Đã đặt
            this.btnStatusDaDat.Text = "Đã đặt";
            this.btnStatusDaDat.Location = new Point(stLeft + 3 * (stWidth + stSpacing), stTop);
            this.btnStatusDaDat.Size = new Size(stWidth, stHeight);
            this.btnStatusDaDat.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.btnStatusDaDat.Click += new System.EventHandler(this.btnStatusDaDat_Click);

            // Ghi chú
            this.lblGhiChu.AutoSize = true;
            this.lblGhiChu.Font = new Font("Segoe UI", 9F);
            this.lblGhiChu.Text = "Ghi chú";
            this.lblGhiChu.Location = new Point(130, 110);

            this.txtGhiChu.Font = new Font("Segoe UI", 9F);
            this.txtGhiChu.Location = new Point(130, 128);
            this.txtGhiChu.Multiline = true;
            this.txtGhiChu.Size = new Size(840, 50);
            this.txtGhiChu.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // === Group Thuê ===
            this.grpThue.Text = "Thông tin thuê phòng";
            this.grpThue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpThue.Location = new Point(20, 190);
            this.grpThue.Size = new Size(460, 170);
            this.grpThue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.rdoDem.Text = "Đêm";
            this.rdoDem.Location = new Point(20, 25);
            this.rdoDem.AutoSize = true;
            this.rdoDem.CheckedChanged += new System.EventHandler(this.rdoDem_CheckedChanged);

            this.rdoNgay.Text = "Ngày";
            this.rdoNgay.Location = new Point(90, 25);
            this.rdoNgay.AutoSize = true;
            this.rdoNgay.Enabled = false;
            this.rdoNgay.Visible = false;
            this.rdoNgay.CheckedChanged += new System.EventHandler(this.rdoNgay_CheckedChanged);

            this.rdoGio.Text = "Giờ";
            this.rdoGio.Location = new Point(170, 25);
            this.rdoGio.AutoSize = true;
            this.rdoGio.CheckedChanged += new System.EventHandler(this.rdoGio_CheckedChanged);

            this.lblSoLuong.Text = "Số đêm";
            this.lblSoLuong.Location = new Point(20, 70);
            this.lblSoLuong.AutoSize = true;

            this.nudSoLuong.Location = new Point(90, 66);
            this.nudSoLuong.Minimum = 1;
            this.nudSoLuong.Maximum = 999;
            this.nudSoLuong.Value = 1;
            this.nudSoLuong.Size = new Size(80, 23);
            this.nudSoLuong.ValueChanged += new System.EventHandler(this.nudSoLuong_ValueChanged);

            this.grpThue.Controls.Add(this.rdoDem);
            this.grpThue.Controls.Add(this.rdoNgay);
            this.grpThue.Controls.Add(this.rdoGio);
            this.grpThue.Controls.Add(this.lblSoLuong);
            this.grpThue.Controls.Add(this.nudSoLuong);

            // === Group Khách ===
            this.grpKhach.Text = "Thông tin khách (bắt buộc khi thuê đêm)";
            this.grpKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpKhach.Location = new Point(500, 190);
            this.grpKhach.Size = new Size(470, 170);
            this.grpKhach.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblTenKhach.Text = "Tên khách";
            this.lblTenKhach.Location = new Point(20, 30);
            this.lblTenKhach.AutoSize = true;

            this.txtTenKhach.Location = new Point(100, 27);
            this.txtTenKhach.Size = new Size(350, 23);

            this.lblCCCD.Text = "Số CCCD";
            this.lblCCCD.Location = new Point(20, 60);
            this.lblCCCD.AutoSize = true;

            this.txtCCCD.Location = new Point(100, 57);
            this.txtCCCD.Size = new Size(350, 23);

            this.btnChonAnh.Text = "Chọn ảnh CCCD";
            this.btnChonAnh.Location = new Point(20, 95);
            this.btnChonAnh.Size = new Size(120, 27);
            this.btnChonAnh.Click += new System.EventHandler(this.btnChonAnh_Click);

            this.picCCCD.Location = new Point(150, 90);
            this.picCCCD.Size = new Size(300, 70);
            this.picCCCD.BorderStyle = BorderStyle.FixedSingle;
            this.picCCCD.SizeMode = PictureBoxSizeMode.Zoom;

            this.grpKhach.Controls.Add(this.lblTenKhach);
            this.grpKhach.Controls.Add(this.txtTenKhach);
            this.grpKhach.Controls.Add(this.lblCCCD);
            this.grpKhach.Controls.Add(this.txtCCCD);
            this.grpKhach.Controls.Add(this.btnChonAnh);
            this.grpKhach.Controls.Add(this.picCCCD);

            // === Group Nước uống ===
            this.grpDichVu.Text = "Nước uống";
            this.grpDichVu.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpDichVu.Location = new Point(20, 370);
            this.grpDichVu.Size = new Size(260, 150);
            this.grpDichVu.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            this.lblNuocNgot.Text = "Nước ngọt";
            this.lblNuocNgot.Location = new Point(20, 30);
            this.lblNuocNgot.AutoSize = true;

            this.nudNuocNgot.Location = new Point(100, 26);
            this.nudNuocNgot.Minimum = 0;
            this.nudNuocNgot.Maximum = 999;
            this.nudNuocNgot.ValueChanged += new System.EventHandler(this.nudNuocNgot_ValueChanged);

            this.lblNuocNgotGia.Text = "20.000 đ / chai";
            this.lblNuocNgotGia.Font = new Font("Segoe UI", 8F);
            this.lblNuocNgotGia.ForeColor = Color.Gray;
            this.lblNuocNgotGia.Location = new Point(100, 50);
            this.lblNuocNgotGia.AutoSize = true;

            this.lblNuocSuoi.Text = "Nước suối";
            this.lblNuocSuoi.Location = new Point(20, 85);
            this.lblNuocSuoi.AutoSize = true;

            this.nudNuocSuoi.Location = new Point(100, 81);
            this.nudNuocSuoi.Minimum = 0;
            this.nudNuocSuoi.Maximum = 999;
            this.nudNuocSuoi.ValueChanged += new System.EventHandler(this.nudNuocSuoi_ValueChanged);

            this.lblNuocSuoiGia.Text = "10.000 đ / chai";
            this.lblNuocSuoiGia.Font = new Font("Segoe UI", 8F);
            this.lblNuocSuoiGia.ForeColor = Color.Gray;
            this.lblNuocSuoiGia.Location = new Point(100, 105);
            this.lblNuocSuoiGia.AutoSize = true;

            this.grpDichVu.Controls.Add(this.lblNuocNgot);
            this.grpDichVu.Controls.Add(this.nudNuocNgot);
            this.grpDichVu.Controls.Add(this.lblNuocNgotGia);
            this.grpDichVu.Controls.Add(this.lblNuocSuoi);
            this.grpDichVu.Controls.Add(this.nudNuocSuoi);
            this.grpDichVu.Controls.Add(this.lblNuocSuoiGia);

            // === Group Thanh toán ===
            this.grpThanhTien.Text = "Thanh toán";
            this.grpThanhTien.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpThanhTien.Location = new Point(300, 370);
            this.grpThanhTien.Size = new Size(360, 150);
            this.grpThanhTien.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            this.labelTienPhong.Text = "Tiền phòng:";
            this.labelTienPhong.Location = new Point(20, 25);
            this.labelTienPhong.AutoSize = true;

            this.lblTienPhong.Text = "0 đ";
            this.lblTienPhong.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            this.lblTienPhong.Location = new Point(130, 25);
            this.lblTienPhong.AutoSize = true;

            this.labelTienDichVu.Text = "Tiền nước:";
            this.labelTienDichVu.Location = new Point(20, 50);
            this.labelTienDichVu.AutoSize = true;

            this.lblTienDichVu.Text = "0 đ";
            this.lblTienDichVu.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            this.lblTienDichVu.Location = new Point(130, 50);
            this.lblTienDichVu.AutoSize = true;

            this.labelDaThu.Text = "Đã thu (x1000 đ):";
            this.labelDaThu.Location = new Point(20, 75);
            this.labelDaThu.AutoSize = true;

            this.txtTienDaThu.Location = new Point(130, 72);
            this.txtTienDaThu.Size = new Size(80, 23);
            this.txtTienDaThu.Text = "";
            this.txtTienDaThu.TextChanged += new System.EventHandler(this.txtTienDaThu_TextChanged);

            this.lblTienDaThu.Text = "0 đ";
            this.lblTienDaThu.Font = new Font("Segoe UI", 8.5F);
            this.lblTienDaThu.ForeColor = Color.Gray;
            this.lblTienDaThu.Location = new Point(220, 75);
            this.lblTienDaThu.AutoSize = true;

            this.labelTongTien.Text = "Tổng tiền (còn lại):";
            this.labelTongTien.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.labelTongTien.Location = new Point(20, 100);
            this.labelTongTien.AutoSize = true;

            this.lblTongTien.Text = "0 đ";
            this.lblTongTien.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            this.lblTongTien.ForeColor = Color.FromArgb(244, 67, 54);
            this.lblTongTien.Location = new Point(170, 98);
            this.lblTongTien.AutoSize = true;

            this.labelGoiYThu.Text = "Gợi ý thu:";
            this.labelGoiYThu.Location = new Point(20, 125);
            this.labelGoiYThu.AutoSize = true;

            // 3 nút gợi ý
            this.btnGoiY1.Location = new Point(130, 121);
            this.btnGoiY1.Size = new Size(70, 23);
            this.btnGoiY1.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.btnGoiY1.FlatStyle = FlatStyle.Flat;
            this.btnGoiY1.FlatAppearance.BorderSize = 0;
            this.btnGoiY1.BackColor = Color.FromArgb(227, 242, 253);
            this.btnGoiY1.ForeColor = Color.FromArgb(25, 118, 210);
            this.btnGoiY1.Text = "";
            this.btnGoiY1.Visible = false;
            this.btnGoiY1.Click += new System.EventHandler(this.btnGoiY_Click);

            this.btnGoiY2.Location = new Point(210, 121);
            this.btnGoiY2.Size = new Size(70, 23);
            this.btnGoiY2.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.btnGoiY2.FlatStyle = FlatStyle.Flat;
            this.btnGoiY2.FlatAppearance.BorderSize = 0;
            this.btnGoiY2.BackColor = Color.FromArgb(227, 242, 253);
            this.btnGoiY2.ForeColor = Color.FromArgb(25, 118, 210);
            this.btnGoiY2.Text = "";
            this.btnGoiY2.Visible = false;
            this.btnGoiY2.Click += new System.EventHandler(this.btnGoiY_Click);

            this.btnGoiY3.Location = new Point(290, 121);
            this.btnGoiY3.Size = new Size(70, 23);
            this.btnGoiY3.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.btnGoiY3.FlatStyle = FlatStyle.Flat;
            this.btnGoiY3.FlatAppearance.BorderSize = 0;
            this.btnGoiY3.BackColor = Color.FromArgb(227, 242, 253);
            this.btnGoiY3.ForeColor = Color.FromArgb(25, 118, 210);
            this.btnGoiY3.Text = "";
            this.btnGoiY3.Visible = false;
            this.btnGoiY3.Click += new System.EventHandler(this.btnGoiY_Click);

            this.grpThanhTien.Controls.Add(this.labelTienPhong);
            this.grpThanhTien.Controls.Add(this.lblTienPhong);
            this.grpThanhTien.Controls.Add(this.labelTienDichVu);
            this.grpThanhTien.Controls.Add(this.lblTienDichVu);
            this.grpThanhTien.Controls.Add(this.labelDaThu);
            this.grpThanhTien.Controls.Add(this.txtTienDaThu);
            this.grpThanhTien.Controls.Add(this.lblTienDaThu);
            this.grpThanhTien.Controls.Add(this.labelTongTien);
            this.grpThanhTien.Controls.Add(this.lblTongTien);
            this.grpThanhTien.Controls.Add(this.labelGoiYThu);
            this.grpThanhTien.Controls.Add(this.btnGoiY1);
            this.grpThanhTien.Controls.Add(this.btnGoiY2);
            this.grpThanhTien.Controls.Add(this.btnGoiY3);

            // Thời gian bắt đầu / hiện tại / tổng
            int tTop = 380;
            this.lblStartCaption.Text = "Bắt đầu:";
            this.lblStartCaption.Location = new Point(680, tTop);
            this.lblStartCaption.AutoSize = true;

            this.lblStartTime.Location = new Point(740, tTop);
            this.lblStartTime.AutoSize = true;

            this.lblEndCaption.Text = "Hiện tại:";
            this.lblEndCaption.Location = new Point(680, tTop + 20);
            this.lblEndCaption.AutoSize = true;

            this.lblEndTime.Location = new Point(740, tTop + 20);
            this.lblEndTime.AutoSize = true;

            this.lblDurationCaption.Text = "Thời gian:";
            this.lblDurationCaption.Location = new Point(680, tTop + 40);
            this.lblDurationCaption.AutoSize = true;

            this.lblDuration.Location = new Point(740, tTop + 40);
            this.lblDuration.AutoSize = true;

            // Buttons dưới
            this.btnTinhTien.Text = "Tính tiền";
            this.btnTinhTien.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnTinhTien.BackColor = Color.FromArgb(3, 155, 229);
            this.btnTinhTien.ForeColor = Color.White;
            this.btnTinhTien.FlatStyle = FlatStyle.Flat;
            this.btnTinhTien.FlatAppearance.BorderSize = 0;
            this.btnTinhTien.Location = new Point(680, 430);
            this.btnTinhTien.Size = new Size(100, 32);
            this.btnTinhTien.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnTinhTien.Click += new System.EventHandler(this.btnTinhTien_Click);

            this.btnLuu.Text = "Lưu";
            this.btnLuu.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnLuu.BackColor = Color.FromArgb(76, 175, 80);
            this.btnLuu.ForeColor = Color.White;
            this.btnLuu.FlatStyle = FlatStyle.Flat;
            this.btnLuu.FlatAppearance.BorderSize = 0;
            this.btnLuu.Location = new Point(680, 470);
            this.btnLuu.Size = new Size(100, 32);
            this.btnLuu.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);

            this.btnHuy.Text = "Hủy";
            this.btnHuy.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnHuy.BackColor = Color.FromArgb(189, 189, 189);
            this.btnHuy.ForeColor = Color.White;
            this.btnHuy.FlatStyle = FlatStyle.Flat;
            this.btnHuy.FlatAppearance.BorderSize = 0;
            this.btnHuy.Location = new Point(790, 470);
            this.btnHuy.Size = new Size(100, 32);
            this.btnHuy.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnHuy.Click += new System.EventHandler(this.btnHuy_Click);

            // Add controls
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblRoomCode);
            this.Controls.Add(this.lblRoomType);
            this.Controls.Add(this.lblFloor);

            this.Controls.Add(this.lblCurrentStatus);
            this.Controls.Add(this.lblCurrentStatusDesc);
            this.Controls.Add(this.btnStatusTrong);
            this.Controls.Add(this.btnStatusCoKhach);
            this.Controls.Add(this.btnStatusChuaDon);
            this.Controls.Add(this.btnStatusDaDat);

            this.Controls.Add(this.lblGhiChu);
            this.Controls.Add(this.txtGhiChu);

            this.Controls.Add(this.grpThue);
            this.Controls.Add(this.grpKhach);
            this.Controls.Add(this.grpDichVu);
            this.Controls.Add(this.grpThanhTien);

            this.Controls.Add(this.lblStartCaption);
            this.Controls.Add(this.lblStartTime);
            this.Controls.Add(this.lblEndCaption);
            this.Controls.Add(this.lblEndTime);
            this.Controls.Add(this.lblDurationCaption);
            this.Controls.Add(this.lblDuration);

            this.Controls.Add(this.btnTinhTien);
            this.Controls.Add(this.btnLuu);
            this.Controls.Add(this.btnHuy);

            ((System.ComponentModel.ISupportInitialize)(this.nudSoLuong)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocNgot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocSuoi)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCCCD)).EndInit();

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
