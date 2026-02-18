using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManagement.Services;

namespace HotelManagement.Forms
{
    public partial class FrmCccdScan : Form
    {
        private readonly CameraScannerService _cameraService = new CameraScannerService();
        private readonly QrDecoderService _qrDecoderService = new QrDecoderService();
        private readonly OcrService _ocrService = new OcrService();
        private readonly CccdParser _parser = new CccdParser();
        private readonly System.Windows.Forms.Timer _qrTimer = new System.Windows.Forms.Timer();

        private CancellationTokenSource _ocrCts;
        private int _qrDecodeBusy;

        public CccdInfo ResultInfo { get; private set; }

        public FrmCccdScan()
        {
            InitializeComponent();
            BindEvents();

            _qrTimer.Interval = 320;
            _qrTimer.Tick += QrTimer_Tick;
        }

        private void BindEvents()
        {
            Load += FrmCccdScan_Load;
            FormClosing += FrmCccdScan_FormClosing;
            Resize += FrmCccdScan_Resize;

            btnRefreshCamera.Click += BtnRefreshCamera_Click;
            btnStartCamera.Click += BtnStartCamera_Click;
            btnStopCamera.Click += BtnStopCamera_Click;
            btnCaptureRecognize.Click += BtnCaptureRecognize_Click;
            btnRetake.Click += BtnRetake_Click;
            btnApply.Click += BtnApply_Click;

            _cameraService.FrameUpdated += CameraService_FrameUpdated;
        }

        private void FrmCccdScan_Load(object sender, EventArgs e)
        {
            LoadCameraList();
            txtNationality.Text = "VNM - Việt Nam";
            BeginInvoke(new Action(AdjustSplitterDistanceSafe));
        }

        private void FrmCccdScan_Resize(object sender, EventArgs e)
        {
            AdjustSplitterDistanceSafe();
        }

        private void FrmCccdScan_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cleanup();
        }

        private void BtnRefreshCamera_Click(object sender, EventArgs e)
        {
            LoadCameraList();
        }

        private void LoadCameraList()
        {
            if (!_cameraService.SupportsCamera)
            {
                cboCamera.DataSource = null;
                cboCamera.Items.Clear();
                btnStartCamera.Enabled = false;
                btnCaptureRecognize.Enabled = true;
                SetStatus("Camera đang bị tắt theo cấu hình project (UseExternalCccdLibs=false).");
                return;
            }

            var cameras = _cameraService.GetAvailableCameras();
            cboCamera.DataSource = null;
            cboCamera.Items.Clear();

            if (cameras == null || cameras.Count == 0)
            {
                SetStatus("Không tìm thấy camera. Có thể dán OCR/QR thủ công rồi bấm Chụp & Nhận dạng.");
                btnStartCamera.Enabled = false;
                btnCaptureRecognize.Enabled = true;
                return;
            }

            cboCamera.DataSource = cameras;
            cboCamera.DisplayMember = nameof(CameraDeviceInfo.DisplayName);
            cboCamera.ValueMember = nameof(CameraDeviceInfo.MonikerString);
            cboCamera.SelectedIndex = 0;
            btnStartCamera.Enabled = true;
            btnCaptureRecognize.Enabled = true;
            SetStatus("Đã tải danh sách camera. Tự động dùng camera mặc định.");
            StartSelectedCamera(false);
        }

        private void BtnStartCamera_Click(object sender, EventArgs e)
        {
            StartSelectedCamera(true);
        }

        private void StartSelectedCamera(bool showErrorDialog)
        {
            try
            {
                var selected = cboCamera.SelectedItem as CameraDeviceInfo;
                if (selected == null)
                {
                    if (showErrorDialog)
                        MessageBox.Show("Vui lòng chọn camera.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _cameraService.Start(selected.MonikerString);
                _qrTimer.Start();
                SetStatus("Camera mặc định đang chạy.");
            }
            catch (Exception ex)
            {
                SetStatus("Không thể mở camera.");
                if (showErrorDialog)
                {
                    MessageBox.Show("Không thể mở camera: " + ex.Message,
                        "Lỗi camera",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void BtnStopCamera_Click(object sender, EventArgs e)
        {
            StopCamera();
        }

        private void StopCamera()
        {
            _qrTimer.Stop();
            _cameraService.Stop();
            SetStatus("Đã dừng camera.");
        }

        private void CameraService_FrameUpdated(object sender, Bitmap frame)
        {
            if (frame == null) return;

            if (IsDisposed)
            {
                frame.Dispose();
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => UpdatePreviewFrame(frame)));
                }
                catch
                {
                    frame.Dispose();
                }
                return;
            }

            UpdatePreviewFrame(frame);
        }

        private void UpdatePreviewFrame(Bitmap frame)
        {
            var old = picPreview.Image;
            picPreview.Image = frame;
            if (old != null) old.Dispose();
        }

        private void QrTimer_Tick(object sender, EventArgs e)
        {
            if (!_cameraService.IsRunning) return;
            if (Interlocked.Exchange(ref _qrDecodeBusy, 1) == 1) return;

            var frame = _cameraService.GetLatestFrameClone();
            if (frame == null)
            {
                Interlocked.Exchange(ref _qrDecodeBusy, 0);
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    return _qrDecoderService.TryDecodeQr(frame);
                }
                finally
                {
                    frame.Dispose();
                }
            })
            .ContinueWith(t =>
            {
                Interlocked.Exchange(ref _qrDecodeBusy, 0);

                if (IsDisposed || Disposing) return;
                if (t.IsFaulted || t.IsCanceled) return;

                var qr = t.Result;
                if (string.IsNullOrWhiteSpace(qr)) return;

                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        txtRawQr.Text = qr;
                        if (string.IsNullOrWhiteSpace(txtDocNumber.Text))
                        {
                            var parsed = _parser.Parse(string.Empty, qr);
                            txtDocNumber.Text = parsed.DocumentNumber ?? string.Empty;
                        }

                        SetStatus("Đã đọc QR.");
                    }));
                }
                catch
                {
                    // Ignore invoke exception during form closing.
                }
            }, TaskScheduler.Default);
        }

        private async void BtnCaptureRecognize_Click(object sender, EventArgs e)
        {
            await CaptureAndRecognizeAsync();
        }

        private async Task CaptureAndRecognizeAsync()
        {
            Bitmap captured = null;

            try
            {
                captured = _cameraService.GetLatestFrameClone();
                if (captured == null)
                {
                    var manualOcr = txtRawOcr.Text ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(manualOcr) && string.IsNullOrWhiteSpace(txtRawQr.Text))
                    {
                        MessageBox.Show("Chưa có khung hình camera. Hãy Start camera hoặc dán OCR/QR thủ công rồi thử lại.",
                            "Thiếu dữ liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    var parsedManual = _parser.Parse(manualOcr, txtRawQr.Text);
                    FillParsedFields(parsedManual);
                    SetStatus("Đã parse từ dữ liệu thủ công.");
                    return;
                }

                btnCaptureRecognize.Enabled = false;
                SetStatus("Đang OCR...");

                var localQr = _qrDecoderService.TryDecodeQr(captured);
                if (!string.IsNullOrWhiteSpace(localQr))
                    txtRawQr.Text = localQr;

                _ocrCts?.Cancel();
                _ocrCts?.Dispose();
                _ocrCts = new CancellationTokenSource();

                string rawOcr = await Task.Run(() => _ocrService.RecognizeText(captured, _ocrCts.Token), _ocrCts.Token);

                txtRawOcr.Text = rawOcr;

                var parsed = _parser.Parse(rawOcr, txtRawQr.Text);
                FillParsedFields(parsed);

                SetStatus("Nhận dạng xong. Kiểm tra và bấm Áp dụng.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Đã hủy OCR.");
            }
            catch (Exception ex)
            {
                var fallback = _parser.Parse(txtRawOcr.Text ?? string.Empty, txtRawQr.Text);
                FillParsedFields(fallback);

                SetStatus("OCR lỗi, vẫn có thể áp dụng số giấy tờ từ QR.");
                MessageBox.Show(
                    "Không thể OCR ảnh CCCD: " + ex.Message + "\nBạn vẫn có thể dùng dữ liệu QR hoặc nhập tay.",
                    "Lỗi OCR",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                btnCaptureRecognize.Enabled = true;
                if (captured != null) captured.Dispose();
            }
        }

        private void FillParsedFields(CccdInfo info)
        {
            if (info == null) return;

            txtDocNumber.Text = info.DocumentNumber ?? string.Empty;
            txtFullName.Text = info.FullName ?? string.Empty;
            cboGender.SelectedItem = NormalizeGender(info.Gender);

            if (info.DateOfBirth.HasValue)
            {
                dtpDob.Checked = true;
                dtpDob.Value = info.DateOfBirth.Value;
            }
            else
            {
                dtpDob.Checked = false;
            }

            txtNationality.Text = string.IsNullOrWhiteSpace(info.Nationality) ? "VNM - Việt Nam" : info.Nationality;
            txtAddressRaw.Text = info.AddressRaw ?? string.Empty;
            txtProvince.Text = info.Province ?? string.Empty;
            txtWard.Text = info.Ward ?? string.Empty;
            txtAddressDetail.Text = info.AddressDetail ?? string.Empty;
        }

        private void BtnRetake_Click(object sender, EventArgs e)
        {
            txtRawOcr.Clear();
            txtDocNumber.Clear();
            txtFullName.Clear();
            cboGender.SelectedIndex = 0;
            dtpDob.Checked = false;
            txtNationality.Text = "VNM - Việt Nam";
            txtAddressRaw.Clear();
            txtProvince.Clear();
            txtWard.Clear();
            txtAddressDetail.Clear();
            SetStatus("Đã reset dữ liệu parse. Có thể chụp lại.");
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            ResultInfo = BuildResultFromForm();
            DialogResult = DialogResult.OK;
            Close();
        }

        private CccdInfo BuildResultFromForm()
        {
            var info = new CccdInfo
            {
                DocumentNumber = SafeTrim(txtDocNumber.Text),
                FullName = SafeTrim(txtFullName.Text),
                Gender = NormalizeGender(cboGender.SelectedItem?.ToString()),
                DateOfBirth = dtpDob.Checked ? (DateTime?)dtpDob.Value.Date : null,
                Nationality = SafeTrim(txtNationality.Text),
                AddressRaw = SafeTrim(txtAddressRaw.Text),
                Province = SafeTrim(txtProvince.Text),
                Ward = SafeTrim(txtWard.Text),
                AddressDetail = SafeTrim(txtAddressDetail.Text),
                RawQr = SafeTrim(txtRawQr.Text),
                RawOcrText = txtRawOcr.Text
            };

            if (string.IsNullOrWhiteSpace(info.Nationality))
                info.Nationality = "VNM - Việt Nam";

            return info;
        }

        private static string SafeTrim(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        private static string NormalizeGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender)) return null;
            var g = gender.Trim().ToLowerInvariant();
            if (g.Contains("nam")) return "Nam";
            if (g.Contains("nữ") || g.Contains("nu")) return "Nữ";
            return "Khác";
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        private void AdjustSplitterDistanceSafe()
        {
            if (splitMain == null || splitMain.IsDisposed) return;

            int width = splitMain.ClientSize.Width;
            if (width <= 0) return;

            int minLeft = Math.Max(120, splitMain.Panel1MinSize);
            int minRight = Math.Max(120, splitMain.Panel2MinSize);
            int maxLeft = width - minRight;
            if (maxLeft <= minLeft) return;

            int desired = (int)(width * 0.52);
            int safeDistance = Math.Max(minLeft, Math.Min(desired, maxLeft));

            try
            {
                splitMain.SplitterDistance = safeDistance;
            }
            catch
            {
                // Ignore invalid resize race when form is being disposed.
            }
        }

        private void Cleanup()
        {
            try
            {
                _ocrCts?.Cancel();
                _ocrCts?.Dispose();
            }
            catch
            {
                // Ignore cleanup exception.
            }

            _qrTimer.Stop();
            _qrTimer.Dispose();

            _cameraService.FrameUpdated -= CameraService_FrameUpdated;
            _cameraService.Dispose();
            _ocrService.Dispose();

            if (picPreview.Image != null)
            {
                picPreview.Image.Dispose();
                picPreview.Image = null;
            }
        }
    }
}
