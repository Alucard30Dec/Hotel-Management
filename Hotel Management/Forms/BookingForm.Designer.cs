using System.Windows.Forms;

namespace HotelManagement.Forms
{
    partial class BookingForm
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView dgvBookings;
        private Label lblDatPhongID;
        private TextBox txtDatPhongID;
        private Label lblKhachHang;
        private ComboBox cboKhachHang;
        private Label lblPhong;
        private ComboBox cboPhong;
        private Label lblNgayDen;
        private DateTimePicker dtpNgayDen;
        private Label lblNgayDiDK;
        private DateTimePicker dtpNgayDiDK;
        private Label lblTienCoc;
        private TextBox txtTienCoc;
        private Button btnCreate;
        private Button btnCheckIn;
        private Button btnCheckOut;
        private Button btnRefresh;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvBookings = new DataGridView();
            this.lblDatPhongID = new Label();
            this.txtDatPhongID = new TextBox();
            this.lblKhachHang = new Label();
            this.cboKhachHang = new ComboBox();
            this.lblPhong = new Label();
            this.cboPhong = new ComboBox();
            this.lblNgayDen = new Label();
            this.dtpNgayDen = new DateTimePicker();
            this.lblNgayDiDK = new Label();
            this.dtpNgayDiDK = new DateTimePicker();
            this.lblTienCoc = new Label();
            this.txtTienCoc = new TextBox();
            this.btnCreate = new Button();
            this.btnCheckIn = new Button();
            this.btnCheckOut = new Button();
            this.btnRefresh = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBookings)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvBookings
            // 
            this.dgvBookings.AllowUserToAddRows = false;
            this.dgvBookings.AllowUserToDeleteRows = false;
            this.dgvBookings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvBookings.Location = new System.Drawing.Point(320, 12);
            this.dgvBookings.MultiSelect = false;
            this.dgvBookings.Name = "dgvBookings";
            this.dgvBookings.ReadOnly = true;
            this.dgvBookings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvBookings.Size = new System.Drawing.Size(450, 300);
            this.dgvBookings.TabIndex = 0;
            this.dgvBookings.CellClick += new DataGridViewCellEventHandler(this.dgvBookings_CellClick);
            // 
            // lblDatPhongID
            // 
            this.lblDatPhongID.AutoSize = true;
            this.lblDatPhongID.Location = new System.Drawing.Point(20, 20);
            this.lblDatPhongID.Name = "lblDatPhongID";
            this.lblDatPhongID.Size = new System.Drawing.Size(54, 15);
            this.lblDatPhongID.TabIndex = 1;
            this.lblDatPhongID.Text = "Mã phiếu";
            // 
            // txtDatPhongID
            // 
            this.txtDatPhongID.Location = new System.Drawing.Point(110, 17);
            this.txtDatPhongID.Name = "txtDatPhongID";
            this.txtDatPhongID.ReadOnly = true;
            this.txtDatPhongID.Size = new System.Drawing.Size(170, 23);
            this.txtDatPhongID.TabIndex = 2;
            // 
            // lblKhachHang
            // 
            this.lblKhachHang.AutoSize = true;
            this.lblKhachHang.Location = new System.Drawing.Point(20, 60);
            this.lblKhachHang.Name = "lblKhachHang";
            this.lblKhachHang.Size = new System.Drawing.Size(70, 15);
            this.lblKhachHang.TabIndex = 3;
            this.lblKhachHang.Text = "Khách hàng";
            // 
            // cboKhachHang
            // 
            this.cboKhachHang.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboKhachHang.Location = new System.Drawing.Point(110, 57);
            this.cboKhachHang.Name = "cboKhachHang";
            this.cboKhachHang.Size = new System.Drawing.Size(170, 23);
            this.cboKhachHang.TabIndex = 4;
            // 
            // lblPhong
            // 
            this.lblPhong.AutoSize = true;
            this.lblPhong.Location = new System.Drawing.Point(20, 100);
            this.lblPhong.Name = "lblPhong";
            this.lblPhong.Size = new System.Drawing.Size(42, 15);
            this.lblPhong.TabIndex = 5;
            this.lblPhong.Text = "Phòng";
            // 
            // cboPhong
            // 
            this.cboPhong.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboPhong.Location = new System.Drawing.Point(110, 97);
            this.cboPhong.Name = "cboPhong";
            this.cboPhong.Size = new System.Drawing.Size(170, 23);
            this.cboPhong.TabIndex = 6;
            // 
            // lblNgayDen
            // 
            this.lblNgayDen.AutoSize = true;
            this.lblNgayDen.Location = new System.Drawing.Point(20, 140);
            this.lblNgayDen.Name = "lblNgayDen";
            this.lblNgayDen.Size = new System.Drawing.Size(58, 15);
            this.lblNgayDen.TabIndex = 7;
            this.lblNgayDen.Text = "Ngày đến";
            // 
            // dtpNgayDen
            // 
            this.dtpNgayDen.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtpNgayDen.Format = DateTimePickerFormat.Custom;
            this.dtpNgayDen.Location = new System.Drawing.Point(110, 137);
            this.dtpNgayDen.Name = "dtpNgayDen";
            this.dtpNgayDen.Size = new System.Drawing.Size(170, 23);
            this.dtpNgayDen.TabIndex = 8;
            // 
            // lblNgayDiDK
            // 
            this.lblNgayDiDK.AutoSize = true;
            this.lblNgayDiDK.Location = new System.Drawing.Point(20, 180);
            this.lblNgayDiDK.Name = "lblNgayDiDK";
            this.lblNgayDiDK.Size = new System.Drawing.Size(88, 15);
            this.lblNgayDiDK.TabIndex = 9;
            this.lblNgayDiDK.Text = "Ngày đi dự kiến";
            // 
            // dtpNgayDiDK
            // 
            this.dtpNgayDiDK.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtpNgayDiDK.Format = DateTimePickerFormat.Custom;
            this.dtpNgayDiDK.Location = new System.Drawing.Point(110, 177);
            this.dtpNgayDiDK.Name = "dtpNgayDiDK";
            this.dtpNgayDiDK.Size = new System.Drawing.Size(170, 23);
            this.dtpNgayDiDK.TabIndex = 10;
            // 
            // lblTienCoc
            // 
            this.lblTienCoc.AutoSize = true;
            this.lblTienCoc.Location = new System.Drawing.Point(20, 220);
            this.lblTienCoc.Name = "lblTienCoc";
            this.lblTienCoc.Size = new System.Drawing.Size(53, 15);
            this.lblTienCoc.TabIndex = 11;
            this.lblTienCoc.Text = "Tiền cọc";
            // 
            // txtTienCoc
            // 
            this.txtTienCoc.Location = new System.Drawing.Point(110, 217);
            this.txtTienCoc.Name = "txtTienCoc";
            this.txtTienCoc.Size = new System.Drawing.Size(170, 23);
            this.txtTienCoc.TabIndex = 12;
            this.txtTienCoc.Text = "0";
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(20, 260);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(60, 30);
            this.btnCreate.TabIndex = 13;
            this.btnCreate.Text = "Đặt";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnCheckIn
            // 
            this.btnCheckIn.Location = new System.Drawing.Point(90, 260);
            this.btnCheckIn.Name = "btnCheckIn";
            this.btnCheckIn.Size = new System.Drawing.Size(70, 30);
            this.btnCheckIn.TabIndex = 14;
            this.btnCheckIn.Text = "Check-in";
            this.btnCheckIn.UseVisualStyleBackColor = true;
            this.btnCheckIn.Click += new System.EventHandler(this.btnCheckIn_Click);
            // 
            // btnCheckOut
            // 
            this.btnCheckOut.Location = new System.Drawing.Point(170, 260);
            this.btnCheckOut.Name = "btnCheckOut";
            this.btnCheckOut.Size = new System.Drawing.Size(80, 30);
            this.btnCheckOut.TabIndex = 15;
            this.btnCheckOut.Text = "Check-out";
            this.btnCheckOut.UseVisualStyleBackColor = true;
            this.btnCheckOut.Click += new System.EventHandler(this.btnCheckOut_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(260, 260);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(70, 30);
            this.btnRefresh.TabIndex = 16;
            this.btnRefresh.Text = "Làm mới";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // BookingForm
            // 
            this.ClientSize = new System.Drawing.Size(784, 321);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnCheckOut);
            this.Controls.Add(this.btnCheckIn);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.txtTienCoc);
            this.Controls.Add(this.lblTienCoc);
            this.Controls.Add(this.dtpNgayDiDK);
            this.Controls.Add(this.lblNgayDiDK);
            this.Controls.Add(this.dtpNgayDen);
            this.Controls.Add(this.lblNgayDen);
            this.Controls.Add(this.cboPhong);
            this.Controls.Add(this.lblPhong);
            this.Controls.Add(this.cboKhachHang);
            this.Controls.Add(this.lblKhachHang);
            this.Controls.Add(this.txtDatPhongID);
            this.Controls.Add(this.lblDatPhongID);
            this.Controls.Add(this.dgvBookings);
            this.Name = "BookingForm";
            this.Text = "Đặt phòng";
            this.Load += new System.EventHandler(this.BookingForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvBookings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
