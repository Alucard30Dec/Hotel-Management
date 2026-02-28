using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HotelManagement.Forms
{
    internal static class ToastNotifier
    {
        private sealed class ToastState
        {
            public ToolStripDropDown DropDown { get; set; }
            public Timer Timer { get; set; }
        }

        private static readonly Dictionary<Control, ToastState> ActiveToasts = new Dictionary<Control, ToastState>();

        public static void Show(Control owner, string message, bool isError = false, int durationMs = 3000)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var anchor = ResolveAnchor(owner);
            if (anchor == null) return;

            if (anchor.InvokeRequired)
            {
                anchor.BeginInvoke(new Action(() => Show(anchor, message, isError, durationMs)));
                return;
            }

            if (anchor.IsDisposed || !anchor.IsHandleCreated) return;

            CleanupDisposedAnchors();
            CloseCore(anchor);

            Color backColor = isError ? Color.FromArgb(211, 67, 67) : Color.FromArgb(46, 125, 97);
            var panel = new Panel
            {
                Size = new Size(360, 54),
                BackColor = backColor,
                Padding = new Padding(12, 8, 12, 8)
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            var label = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Text = message.Trim(),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(label);

            var host = new ToolStripControlHost(panel)
            {
                AutoSize = false,
                Size = panel.Size,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            var dropDown = new ToolStripDropDown
            {
                AutoClose = false,
                DropShadowEnabled = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            dropDown.Items.Add(host);

            Point location = new Point(Math.Max(8, anchor.ClientSize.Width - panel.Width - 16), 12);
            dropDown.Show(anchor, location, ToolStripDropDownDirection.BelowRight);

            var timer = new Timer { Interval = Math.Max(1200, durationMs) };
            timer.Tick += (s, e) => Close(anchor);
            timer.Start();

            ActiveToasts[anchor] = new ToastState
            {
                DropDown = dropDown,
                Timer = timer
            };
        }

        public static void Close(Control owner)
        {
            var anchor = ResolveAnchor(owner);
            if (anchor == null) return;

            if (anchor.InvokeRequired)
            {
                anchor.BeginInvoke(new Action(() => Close(anchor)));
                return;
            }

            CloseCore(anchor);
        }

        private static Control ResolveAnchor(Control owner)
        {
            if (owner == null) return Form.ActiveForm;
            if (owner is Form) return owner;
            return owner.FindForm() ?? owner;
        }

        private static void CloseCore(Control anchor)
        {
            if (anchor == null) return;
            if (!ActiveToasts.TryGetValue(anchor, out var state) || state == null) return;

            if (state.Timer != null)
            {
                state.Timer.Stop();
                state.Timer.Dispose();
                state.Timer = null;
            }

            if (state.DropDown != null)
            {
                if (!state.DropDown.IsDisposed)
                    state.DropDown.Close();
                state.DropDown.Dispose();
                state.DropDown = null;
            }

            ActiveToasts.Remove(anchor);
        }

        private static void CleanupDisposedAnchors()
        {
            var disposedAnchors = ActiveToasts.Keys.Where(k => k == null || k.IsDisposed).ToList();
            foreach (var anchor in disposedAnchors)
            {
                ActiveToasts.Remove(anchor);
            }
        }
    }
}
