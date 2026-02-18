using System;
using System.Drawing;
#if USE_CCCD_EXTERNAL_LIBS
using ZXing;
using ZXing.Common;
#endif

namespace HotelManagement.Services
{
    public sealed class QrDecoderService
    {
#if USE_CCCD_EXTERNAL_LIBS
        private readonly BarcodeReader _reader;

        public QrDecoderService()
        {
            _reader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };
        }

        public string TryDecodeQr(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            try
            {
                var result = _reader.Decode(bitmap);
                var text = result?.Text;
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch
            {
                return null;
            }
        }
#else
        public string TryDecodeQr(Bitmap bitmap)
        {
            return null;
        }
#endif
    }
}
