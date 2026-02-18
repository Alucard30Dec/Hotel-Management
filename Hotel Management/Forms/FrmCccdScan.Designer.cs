using System;
using System.Drawing;
using System.Windows.Forms;

namespace HotelManagement.Forms
{
    partial class FrmCccdScan
    {
        private System.ComponentModel.IContainer components = null;

        private ComboBox cboCamera;
        private Button btnRefreshCamera;
        private Button btnStartCamera;
        private Button btnStopCamera;
        private Button btnCaptureRecognize;
        private Button btnRetake;

        private PictureBox picPreview;
        private SplitContainer splitMain;
        private TextBox txtRawQr;
        private TextBox txtRawOcr;

        private TextBox txtDocNumber;
        private TextBox txtFullName;
        private ComboBox cboGender;
        private DateTimePicker dtpDob;
        private TextBox txtNationality;
        private TextBox txtAddressRaw;
        private TextBox txtProvince;
        private TextBox txtWard;
        private TextBox txtAddressDetail;

        private Button btnApply;
        private Button btnCancel;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Text = "Quét CCCD";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(1200, 760);
            this.MinimumSize = new Size(1000, 680);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 4) };
            cboCamera = new ComboBox { Left = 8, Top = 8, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            btnRefreshCamera = new Button { Left = 296, Top = 8, Width = 90, Text = "Làm mới" };
            btnStartCamera = new Button { Left = 392, Top = 8, Width = 90, Text = "Start" };
            btnStopCamera = new Button { Left = 488, Top = 8, Width = 90, Text = "Stop" };
            btnCaptureRecognize = new Button { Left = 584, Top = 8, Width = 160, Text = "Chụp & Nhận dạng" };
            btnRetake = new Button { Left = 750, Top = 8, Width = 90, Text = "Chụp lại" };
            lblStatus = new Label { Left = 850, Top = 12, Width = 320, Text = "Sẵn sàng", AutoEllipsis = true };

            topPanel.Controls.Add(cboCamera);
            topPanel.Controls.Add(btnRefreshCamera);
            topPanel.Controls.Add(btnStartCamera);
            topPanel.Controls.Add(btnStopCamera);
            topPanel.Controls.Add(btnCaptureRecognize);
            topPanel.Controls.Add(btnRetake);
            topPanel.Controls.Add(lblStatus);

            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 120,
                Panel2MinSize = 120
            };

            picPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            splitMain.Panel1.Padding = new Padding(8);
            splitMain.Panel1.Controls.Add(picPreview);

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(8)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10f));

            var grpRaw = new GroupBox { Text = "Dữ liệu thô", Dock = DockStyle.Fill };
            var rawLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8) };
            rawLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rawLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            rawLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rawLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 75f));
            rawLayout.Controls.Add(new Label { Text = "QR raw", Dock = DockStyle.Fill, AutoSize = true }, 0, 0);
            txtRawQr = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            rawLayout.Controls.Add(txtRawQr, 0, 1);
            rawLayout.Controls.Add(new Label { Text = "OCR raw", Dock = DockStyle.Fill, AutoSize = true }, 0, 2);
            txtRawOcr = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            rawLayout.Controls.Add(txtRawOcr, 0, 3);
            grpRaw.Controls.Add(rawLayout);

            var grpParsed = new GroupBox { Text = "Kết quả parse (cho phép sửa)", Dock = DockStyle.Fill };
            var parsedLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5, Padding = new Padding(8) };
            for (int i = 0; i < 4; i++) parsedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            for (int i = 0; i < 5; i++) parsedLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            txtDocNumber = new TextBox();
            txtFullName = new TextBox();
            cboGender = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboGender.Items.AddRange(new object[] { "", "Nam", "Nữ", "Khác" });
            dtpDob = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
            txtNationality = new TextBox();
            txtAddressRaw = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical };
            txtProvince = new TextBox();
            txtWard = new TextBox();
            txtAddressDetail = new TextBox();

            AddParsedField(parsedLayout, "Số giấy tờ", txtDocNumber, 0, 0, 2);
            AddParsedField(parsedLayout, "Họ tên", txtFullName, 2, 0, 2);
            AddParsedField(parsedLayout, "Giới tính", cboGender, 0, 1, 1);
            AddParsedField(parsedLayout, "Ngày sinh", dtpDob, 1, 1, 1);
            AddParsedField(parsedLayout, "Quốc tịch", txtNationality, 2, 1, 2);
            AddParsedField(parsedLayout, "Địa chỉ raw", txtAddressRaw, 0, 2, 4);
            AddParsedField(parsedLayout, "Tỉnh/Thành", txtProvince, 0, 3, 2);
            AddParsedField(parsedLayout, "Phường/Xã", txtWard, 2, 3, 2);
            AddParsedField(parsedLayout, "Địa chỉ chi tiết", txtAddressDetail, 0, 4, 4);

            grpParsed.Controls.Add(parsedLayout);

            var bottomPanel = new Panel { Dock = DockStyle.Fill };
            btnApply = new Button { Text = "Áp dụng vào form", Width = 150, Height = 32, Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Left = 260, Top = 8 };
            btnCancel = new Button { Text = "Đóng", Width = 100, Height = 32, Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Left = 420, Top = 8, DialogResult = DialogResult.Cancel };
            bottomPanel.Controls.Add(btnApply);
            bottomPanel.Controls.Add(btnCancel);

            rightLayout.Controls.Add(grpRaw, 0, 0);
            rightLayout.Controls.Add(grpParsed, 0, 1);
            rightLayout.Controls.Add(bottomPanel, 0, 2);

            splitMain.Panel2.Controls.Add(rightLayout);

            this.Controls.Add(splitMain);
            this.Controls.Add(topPanel);
            this.AcceptButton = btnApply;
            this.CancelButton = btnCancel;
        }

        private static void AddParsedField(TableLayoutPanel table, string labelText, Control input, int col, int row, int colSpan)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4) };
            var label = new Label { Text = labelText, Dock = DockStyle.Top, Height = 18 };
            input.Dock = DockStyle.Fill;
            panel.Controls.Add(input);
            panel.Controls.Add(label);

            table.Controls.Add(panel, col, row);
            if (colSpan > 1) table.SetColumnSpan(panel, colSpan);
        }
    }
}
