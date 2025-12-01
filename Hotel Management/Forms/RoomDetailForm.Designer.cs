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

        // trạng thái
        private Label lblCurrentStatus;
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
        private Label lblNgayDen;
        private Label lblNgayDi;
        private DateTimePicker dtpNgayDen;
        private DateTimePicker dtpNgayDi;
        private Label lblSoGio;
        private NumericUpDown nudSoGio;

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
        private Label labelTongTien;
        private Label lblTienPhong;
        private Label lblTienDichVu;
        private Label lblTongTien;

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
            this.lblNgayDen = new Label();
            this.lblNgayDi = new Label();
            this.dtpNgayDen = new DateTimePicker();
            this.dtpNgayDi = new DateTimePicker();
            this.lblSoGio = new Label();
            this.nudSoGio = new NumericUpDown();

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
            this.labelTongTien = new Label();
            this.lblTienPhong = new Label();
            this.lblTienDichVu = new Label();
            this.lblTongTien = new Label();

            this.lblStartCaption = new Label();
            this.lblEndCaption = new Label();
            this.lblDurationCaption = new Label();
            this.lblStartTime = new Label();
            this.lblEndTime = new Label();
            this.lblDuration = new Label();

            this.btnTinhTien = new Button();
            this.btnLuu = new Button();
            this.btnHuy = new Button();

            ((System.ComponentModel.ISupportInitialize)(this.nudSoGio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocNgot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocSuoi)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCCCD)).BeginInit();

            this.SuspendLayout();

            // Form
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 520);
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
            this.btnBack.Size = new Size(90, 28);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

            // Title
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(63, 81, 181);
            this.lblTitle.Text = "Thông tin phòng";
            this.lblTitle.Location = new Point(120, 12);

            this.lblRoomCode.AutoSize = true;
            this.lblRoomCode.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            this.lblRoomCode.Location = new Point(120, 40);

            this.lblRoomType.AutoSize = true;
            this.lblRoomType.Font = new Font("Segoe UI", 10F);
            this.lblRoomType.ForeColor = Color.Gray;
            this.lblRoomType.Location = new Point(120, 75);

            this.lblFloor.AutoSize = true;
            this.lblFloor.Font = new Font("Segoe UI", 10F);
            this.lblFloor.ForeColor = Color.Gray;
            this.lblFloor.Location = new Point(220, 75);

            // ===== Trạng thái =====
            this.lblCurrentStatus.AutoSize = true;
            this.lblCurrentStatus.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            this.lblCurrentStatus.ForeColor = Color.FromArgb(55, 71, 79);
            this.lblCurrentStatus.Text = "Trạng thái phòng:";
            this.lblCurrentStatus.Location = new Point(350, 16);

            int stTop = 46;
            int stLeft = 350;
            int stWidth = 110;
            int stHeight = 36;
            int stGap = 8;

            // Trống
            this.btnStatusTrong.Text = "Trống";
            this.btnStatusTrong.Location = new Point(stLeft, stTop);
            this.btnStatusTrong.Size = new Size(stWidth, stHeight);
            this.btnStatusTrong.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStatusTrong.Click += new System.EventHandler(this.btnStatusTrong_Click);

            // Có khách
            this.btnStatusCoKhach.Text = "Có khách";
            this.btnStatusCoKhach.Location = new Point(stLeft + (stWidth + stGap), stTop);
            this.btnStatusCoKhach.Size = new Size(stWidth, stHeight);
            this.btnStatusCoKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStatusCoKhach.Click += new System.EventHandler(this.btnStatusCoKhach_Click);

            // Chưa dọn
            this.btnStatusChuaDon.Text = "Chưa dọn";
            this.btnStatusChuaDon.Location = new Point(stLeft + 2 * (stWidth + stGap), stTop);
            this.btnStatusChuaDon.Size = new Size(stWidth, stHeight);
            this.btnStatusChuaDon.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStatusChuaDon.Click += new System.EventHandler(this.btnStatusChuaDon_Click);

            // Đã đặt
            this.btnStatusDaDat.Text = "Đã đặt";
            this.btnStatusDaDat.Location = new Point(stLeft + 3 * (stWidth + stGap), stTop);
            this.btnStatusDaDat.Size = new Size(stWidth, stHeight);
            this.btnStatusDaDat.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStatusDaDat.Click += new System.EventHandler(this.btnStatusDaDat_Click);

            // Ghi chú
            this.lblGhiChu.AutoSize = true;
            this.lblGhiChu.Font = new Font("Segoe UI", 9F);
            this.lblGhiChu.Text = "Ghi chú";
            this.lblGhiChu.Location = new Point(120, 100);

            this.txtGhiChu.Font = new Font("Segoe UI", 9F);
            this.txtGhiChu.Location = new Point(170, 97);
            this.txtGhiChu.Width = 700;
            this.txtGhiChu.Height = 50;
            this.txtGhiChu.Multiline = true;

            // Group thuê
            this.grpThue.Text = "Thông tin thuê phòng";
            this.grpThue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpThue.Location = new Point(20, 155);
            this.grpThue.Size = new Size(420, 180);

            this.rdoDem.Text = "Đêm";
            this.rdoDem.Location = new Point(20, 25);
            this.rdoDem.CheckedChanged += new System.EventHandler(this.rdoDem_CheckedChanged);

            this.rdoNgay.Text = "Ngày";
            this.rdoNgay.Location = new Point(90, 25);
            this.rdoNgay.CheckedChanged += new System.EventHandler(this.rdoNgay_CheckedChanged);

            this.rdoGio.Text = "Giờ";
            this.rdoGio.Location = new Point(160, 25);
            this.rdoGio.CheckedChanged += new System.EventHandler(this.rdoGio_CheckedChanged);

            this.lblNgayDen.Text = "Ngày đến";
            this.lblNgayDen.Location = new Point(20, 60);

            this.dtpNgayDen.Format = DateTimePickerFormat.Custom;
            this.dtpNgayDen.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtpNgayDen.Location = new Point(100, 56);
            this.dtpNgayDen.Width = 200;

            this.lblNgayDi.Text = "Ngày đi";
            this.lblNgayDi.Location = new Point(20, 90);
            this.dtpNgayDi.Format = DateTimePickerFormat.Custom;
            this.dtpNgayDi.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtpNgayDi.Location = new Point(100, 86);
            this.dtpNgayDi.Width = 200;

            this.lblSoGio.Text = "Số giờ";
            this.lblSoGio.Location = new Point(20, 120);

            this.nudSoGio.Location = new Point(100, 116);
            this.nudSoGio.Minimum = 1;
            this.nudSoGio.Maximum = 24;
            this.nudSoGio.Value = 1;

            this.grpThue.Controls.Add(this.rdoDem);
            this.grpThue.Controls.Add(this.rdoNgay);
            this.grpThue.Controls.Add(this.rdoGio);
            this.grpThue.Controls.Add(this.lblNgayDen);
            this.grpThue.Controls.Add(this.dtpNgayDen);
            this.grpThue.Controls.Add(this.lblNgayDi);
            this.grpThue.Controls.Add(this.dtpNgayDi);
            this.grpThue.Controls.Add(this.lblSoGio);
            this.grpThue.Controls.Add(this.nudSoGio);

            // Group khách
            this.grpKhach.Text = "Thông tin khách (bắt buộc khi thuê ngày / đêm)";
            this.grpKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpKhach.Location = new Point(460, 155);
            this.grpKhach.Size = new Size(410, 180);

            this.lblTenKhach.Text = "Tên khách";
            this.lblTenKhach.Location = new Point(15, 30);
            this.txtTenKhach.Location = new Point(90, 27);
            this.txtTenKhach.Width = 300;

            this.lblCCCD.Text = "Số CCCD";
            this.lblCCCD.Location = new Point(15, 60);
            this.txtCCCD.Location = new Point(90, 57);
            this.txtCCCD.Width = 300;

            this.btnChonAnh.Text = "Chọn ảnh CCCD";
            this.btnChonAnh.Location = new Point(15, 90);
            this.btnChonAnh.Size = new Size(120, 25);
            this.btnChonAnh.Click += new System.EventHandler(this.btnChonAnh_Click);

            this.picCCCD.Location = new Point(150, 90);
            this.picCCCD.Size = new Size(240, 80);
            this.picCCCD.BorderStyle = BorderStyle.FixedSingle;
            this.picCCCD.SizeMode = PictureBoxSizeMode.Zoom;

            this.grpKhach.Controls.Add(this.lblTenKhach);
            this.grpKhach.Controls.Add(this.txtTenKhach);
            this.grpKhach.Controls.Add(this.lblCCCD);
            this.grpKhach.Controls.Add(this.txtCCCD);
            this.grpKhach.Controls.Add(this.btnChonAnh);
            this.grpKhach.Controls.Add(this.picCCCD);

            // Group nước
            this.grpDichVu.Text = "Nước uống";
            this.grpDichVu.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpDichVu.Location = new Point(20, 345);
            this.grpDichVu.Size = new Size(260, 130);

            this.lblNuocNgot.Text = "Nước ngọt";
            this.lblNuocNgot.Location = new Point(15, 30);
            this.nudNuocNgot.Location = new Point(90, 26);
            this.nudNuocNgot.Minimum = 0;
            this.nudNuocNgot.Maximum = 100;
            this.nudNuocNgot.ValueChanged += new System.EventHandler(this.nudNuocNgot_ValueChanged);

            this.lblNuocNgotGia.Text = "20.000 đ / chai";
            this.lblNuocNgotGia.Font = new Font("Segoe UI", 8F);
            this.lblNuocNgotGia.ForeColor = Color.Gray;
            this.lblNuocNgotGia.Location = new Point(90, 50);

            this.lblNuocSuoi.Text = "Nước suối";
            this.lblNuocSuoi.Location = new Point(15, 75);
            this.nudNuocSuoi.Location = new Point(90, 71);
            this.nudNuocSuoi.Minimum = 0;
            this.nudNuocSuoi.Maximum = 100;
            this.nudNuocSuoi.ValueChanged += new System.EventHandler(this.nudNuocSuoi_ValueChanged);

            this.lblNuocSuoiGia.Text = "10.000 đ / chai";
            this.lblNuocSuoiGia.Font = new Font("Segoe UI", 8F);
            this.lblNuocSuoiGia.ForeColor = Color.Gray;
            this.lblNuocSuoiGia.Location = new Point(90, 95);

            this.grpDichVu.Controls.Add(this.lblNuocNgot);
            this.grpDichVu.Controls.Add(this.nudNuocNgot);
            this.grpDichVu.Controls.Add(this.lblNuocNgotGia);
            this.grpDichVu.Controls.Add(this.lblNuocSuoi);
            this.grpDichVu.Controls.Add(this.nudNuocSuoi);
            this.grpDichVu.Controls.Add(this.lblNuocSuoiGia);

            // Group thanh toán
            this.grpThanhTien.Text = "Thanh toán";
            this.grpThanhTien.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpThanhTien.Location = new Point(300, 345);
            this.grpThanhTien.Size = new Size(360, 130);

            this.labelTienPhong.Text = "Tiền phòng:";
            this.labelTienPhong.Location = new Point(20, 28);

            this.lblTienPhong.Text = "0 đ";
            this.lblTienPhong.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            this.lblTienPhong.Location = new Point(120, 28);

            this.labelTienDichVu.Text = "Tiền nước:";
            this.labelTienDichVu.Location = new Point(20, 50);

            this.lblTienDichVu.Text = "0 đ";
            this.lblTienDichVu.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            this.lblTienDichVu.Location = new Point(120, 50);

            this.labelTongTien.Text = "Tổng cộng:";
            this.labelTongTien.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.labelTongTien.Location = new Point(20, 75);

            this.lblTongTien.Text = "0 đ";
            this.lblTongTien.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            this.lblTongTien.ForeColor = Color.FromArgb(244, 67, 54);
            this.lblTongTien.Location = new Point(120, 75);

            this.grpThanhTien.Controls.Add(this.labelTienPhong);
            this.grpThanhTien.Controls.Add(this.lblTienPhong);
            this.grpThanhTien.Controls.Add(this.labelTienDichVu);
            this.grpThanhTien.Controls.Add(this.lblTienDichVu);
            this.grpThanhTien.Controls.Add(this.labelTongTien);
            this.grpThanhTien.Controls.Add(this.lblTongTien);

            // Thời gian bắt đầu / kết thúc / tổng
            int tTop = 350;
            this.lblStartCaption.Text = "Bắt đầu:";
            this.lblStartCaption.Location = new Point(680, tTop);
            this.lblStartTime.Location = new Point(740, tTop);
            this.lblStartTime.AutoSize = true;

            this.lblEndCaption.Text = "Hiện tại:";
            this.lblEndCaption.Location = new Point(680, tTop + 20);
            this.lblEndTime.Location = new Point(740, tTop + 20);
            this.lblEndTime.AutoSize = true;

            this.lblDurationCaption.Text = "Thời gian:";
            this.lblDurationCaption.Location = new Point(680, tTop + 40);
            this.lblDuration.Location = new Point(740, tTop + 40);
            this.lblDuration.AutoSize = true;

            // Buttons dưới
            this.btnTinhTien.Text = "Tính tiền";
            this.btnTinhTien.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnTinhTien.BackColor = Color.FromArgb(3, 155, 229);
            this.btnTinhTien.ForeColor = Color.White;
            this.btnTinhTien.FlatStyle = FlatStyle.Flat;
            this.btnTinhTien.FlatAppearance.BorderSize = 0;
            this.btnTinhTien.Location = new Point(680, 410);
            this.btnTinhTien.Size = new Size(90, 30);
            this.btnTinhTien.Click += new System.EventHandler(this.btnTinhTien_Click);

            this.btnLuu.Text = "Lưu";
            this.btnLuu.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnLuu.BackColor = Color.FromArgb(76, 175, 80);
            this.btnLuu.ForeColor = Color.White;
            this.btnLuu.FlatStyle = FlatStyle.Flat;
            this.btnLuu.FlatAppearance.BorderSize = 0;
            this.btnLuu.Location = new Point(680, 445);
            this.btnLuu.Size = new Size(90, 30);
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);

            this.btnHuy.Text = "Hủy";
            this.btnHuy.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnHuy.BackColor = Color.FromArgb(189, 189, 189);
            this.btnHuy.ForeColor = Color.White;
            this.btnHuy.FlatStyle = FlatStyle.Flat;
            this.btnHuy.FlatAppearance.BorderSize = 0;
            this.btnHuy.Location = new Point(780, 445);
            this.btnHuy.Size = new Size(90, 30);
            this.btnHuy.Click += new System.EventHandler(this.btnHuy_Click);

            // Add controls
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblRoomCode);
            this.Controls.Add(this.lblRoomType);
            this.Controls.Add(this.lblFloor);

            this.Controls.Add(this.lblCurrentStatus);
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

            ((System.ComponentModel.ISupportInitialize)(this.nudSoGio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocNgot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNuocSuoi)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCCCD)).EndInit();

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
