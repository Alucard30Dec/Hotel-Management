using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
#if USE_CCCD_EXTERNAL_LIBS
using Tesseract;
#endif

namespace HotelManagement.Services
{
    public sealed class OcrService : IDisposable
    {
#if USE_CCCD_EXTERNAL_LIBS
        private readonly object _engineLock = new object();
        private TesseractEngine _engine;
        private bool _disposed;

        public string RecognizeText(Bitmap sourceBitmap, CancellationToken cancellationToken)
        {
            if (sourceBitmap == null) return string.Empty;
            cancellationToken.ThrowIfCancellationRequested();

            EnsureEngine();

            using (var preprocessed = Preprocess(sourceBitmap))
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var pix = PixConverter.ToPix(preprocessed))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    lock (_engineLock)
                    {
                        using (var page = _engine.Process(pix, PageSegMode.Auto))
                        {
                            return page?.GetText()?.Trim() ?? string.Empty;
                        }
                    }
                }
            }
        }

        private void EnsureEngine()
        {
            if (_engine != null) return;

            lock (_engineLock)
            {
                if (_engine != null) return;

                var tessdataPath = ResolveTessdataPath();
                var language = ResolveLanguage(tessdataPath);

                _engine = new TesseractEngine(tessdataPath, language, EngineMode.Default);
                _engine.SetVariable("user_defined_dpi", "300");
            }
        }

        private static string ResolveTessdataPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var tessdataPath = Path.Combine(baseDir, "tessdata");

            if (!Directory.Exists(tessdataPath))
                throw new DirectoryNotFoundException("Không tìm thấy thư mục tessdata. Vui lòng thêm vie.traineddata/eng.traineddata.");

            return tessdataPath;
        }

        private static string ResolveLanguage(string tessdataPath)
        {
            var viePath = Path.Combine(tessdataPath, "vie.traineddata");
            var engPath = Path.Combine(tessdataPath, "eng.traineddata");

            var hasVie = File.Exists(viePath);
            var hasEng = File.Exists(engPath);

            if (hasVie && hasEng) return "vie+eng";
            if (hasVie) return "vie";
            if (hasEng) return "eng";

            throw new FileNotFoundException("Thiếu traineddata cho OCR. Cần ít nhất vie.traineddata hoặc eng.traineddata.");
        }

        private static Bitmap Preprocess(Bitmap input)
        {
            // 1) Crop vùng trung tâm để giảm nhiễu viền thẻ.
            using (var cropped = CropCenter(input, 0.92f, 0.88f))
            // 2) Grayscale + tăng tương phản nhẹ.
            using (var gray = ToGrayWithContrast(cropped, 1.25f))
            {
                // 3) Nhị phân hoá nhẹ để OCR ổn định hơn.
                return Threshold(gray, 145);
            }
        }

        private static Bitmap CropCenter(Bitmap source, float widthRatio, float heightRatio)
        {
            var w = Math.Max(1, (int)(source.Width * widthRatio));
            var h = Math.Max(1, (int)(source.Height * heightRatio));
            var x = Math.Max(0, (source.Width - w) / 2);
            var y = Math.Max(0, (source.Height - h) / 2);

            var rect = new Rectangle(x, y, Math.Min(w, source.Width - x), Math.Min(h, source.Height - y));
            return source.Clone(rect, source.PixelFormat);
        }

        private static Bitmap ToGrayWithContrast(Bitmap source, float contrast)
        {
            var gray = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(gray))
            using (var attrs = new ImageAttributes())
            {
                var adjusted = contrast;
                var t = (1.0f - adjusted) / 2.0f;
                var matrix = new ColorMatrix(new[]
                {
                    new[] { 0.299f * adjusted, 0.299f * adjusted, 0.299f * adjusted, 0f, 0f },
                    new[] { 0.587f * adjusted, 0.587f * adjusted, 0.587f * adjusted, 0f, 0f },
                    new[] { 0.114f * adjusted, 0.114f * adjusted, 0.114f * adjusted, 0f, 0f },
                    new[] { 0f, 0f, 0f, 1f, 0f },
                    new[] { t, t, t, 0f, 1f }
                });

                attrs.SetColorMatrix(matrix);
                g.DrawImage(source,
                    new Rectangle(0, 0, source.Width, source.Height),
                    0,
                    0,
                    source.Width,
                    source.Height,
                    GraphicsUnit.Pixel,
                    attrs);
            }

            return gray;
        }

        private static Bitmap Threshold(Bitmap source, byte threshold)
        {
            var bw = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var p = source.GetPixel(x, y);
                    var luminance = (byte)((p.R + p.G + p.B) / 3);
                    bw.SetPixel(x, y, luminance >= threshold ? Color.White : Color.Black);
                }
            }

            return bw;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_engineLock)
            {
                if (_engine != null)
                {
                    _engine.Dispose();
                    _engine = null;
                }
            }
        }
#else
        private bool _disposed;

        public string RecognizeText(Bitmap sourceBitmap, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sourceBitmap == null) return string.Empty;

            throw new NotSupportedException("Tính năng OCR cần thư viện Tesseract. Vui lòng restore NuGet và bật USE_CCCD_EXTERNAL_LIBS.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
#endif
    }
}
