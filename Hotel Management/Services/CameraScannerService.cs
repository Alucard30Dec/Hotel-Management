using System;
using System.Collections.Generic;
using System.Drawing;
#if USE_CCCD_EXTERNAL_LIBS
using AForge.Video;
using AForge.Video.DirectShow;
#endif

namespace HotelManagement.Services
{
    public sealed class CameraDeviceInfo
    {
        public string MonikerString { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName ?? "Unknown camera";
        }
    }

    public sealed class CameraScannerService : IDisposable
    {
#if USE_CCCD_EXTERNAL_LIBS
        public bool SupportsCamera => true;
        private readonly object _frameLock = new object();
        private VideoCaptureDevice _videoDevice;
        private Bitmap _latestFrame;
        private bool _disposed;

        public event EventHandler<Bitmap> FrameUpdated;

        public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras()
        {
            var list = new List<CameraDeviceInfo>();
            try
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                for (int i = 0; i < videoDevices.Count; i++)
                {
                    var device = videoDevices[i];
                    list.Add(new CameraDeviceInfo
                    {
                        MonikerString = device.MonikerString,
                        DisplayName = device.Name
                    });
                }
            }
            catch
            {
                // Return empty list; UI handles fallback manual input.
            }

            return list;
        }

        public bool IsRunning
        {
            get
            {
                return _videoDevice != null && _videoDevice.IsRunning;
            }
        }

        public void Start(string monikerString)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(monikerString))
                throw new ArgumentException("Camera moniker is required.", nameof(monikerString));

            Stop();

            var device = new VideoCaptureDevice(monikerString);
            device.NewFrame += OnVideoNewFrame;
            _videoDevice = device;
            _videoDevice.Start();
        }

        public void Stop()
        {
            if (_videoDevice != null)
            {
                try
                {
                    _videoDevice.NewFrame -= OnVideoNewFrame;
                    if (_videoDevice.IsRunning)
                    {
                        _videoDevice.SignalToStop();
                        _videoDevice.WaitForStop();
                    }
                }
                catch
                {
                    // Ignore shutdown exception and continue cleanup.
                }
                finally
                {
                    _videoDevice = null;
                }
            }

            lock (_frameLock)
            {
                if (_latestFrame != null)
                {
                    _latestFrame.Dispose();
                    _latestFrame = null;
                }
            }
        }

        public Bitmap GetLatestFrameClone()
        {
            lock (_frameLock)
            {
                if (_latestFrame == null) return null;
                return (Bitmap)_latestFrame.Clone();
            }
        }

        private void OnVideoNewFrame(object sender, NewFrameEventArgs e)
        {
            if (_disposed || e?.Frame == null) return;

            Bitmap latestClone = null;
            Bitmap eventClone = null;

            try
            {
                latestClone = (Bitmap)e.Frame.Clone();

                lock (_frameLock)
                {
                    if (_latestFrame != null)
                    {
                        _latestFrame.Dispose();
                    }

                    _latestFrame = (Bitmap)latestClone.Clone();
                }

                eventClone = latestClone;
                latestClone = null;
            }
            catch
            {
                if (eventClone != null)
                {
                    eventClone.Dispose();
                    eventClone = null;
                }
            }
            finally
            {
                if (latestClone != null)
                {
                    latestClone.Dispose();
                }
            }

            if (eventClone == null) return;

            var handler = FrameUpdated;
            if (handler == null)
            {
                eventClone.Dispose();
                return;
            }

            try
            {
                handler(this, eventClone);
            }
            catch
            {
                eventClone.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CameraScannerService));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
#else
        public bool SupportsCamera => false;
        private bool _disposed;

        public event EventHandler<Bitmap> FrameUpdated;

        public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras()
        {
            return new List<CameraDeviceInfo>();
        }

        public bool IsRunning
        {
            get { return false; }
        }

        public void Start(string monikerString)
        {
            ThrowIfDisposed();
            throw new NotSupportedException("Tính năng camera cần thư viện AForge. Vui lòng restore NuGet và bật USE_CCCD_EXTERNAL_LIBS.");
        }

        public void Stop()
        {
            // No-op in fallback mode.
        }

        public Bitmap GetLatestFrameClone()
        {
            return null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CameraScannerService));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
#endif
    }
}
