using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExtLume
{
    internal sealed class DimmingOverlayForm : Form
    {
        private const int WsExTransparent = 0x00000020;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExLayered = 0x00080000;
        private const int WsExNoActivate = 0x08000000;
        private const int WmNcHitTest = 0x0084;
        private static readonly IntPtr HtTransparent = new IntPtr(-1);

        internal DimmingOverlayForm(Rectangle bounds)
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            Bounds = bounds;
            ControlBox = false;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExTransparent
                    | WsExToolWindow
                    | WsExLayered
                    | WsExNoActivate;
                return parameters;
            }
        }

        internal void SetLevel(Rectangle bounds, int percent)
        {
            Bounds = bounds;
            double opacity = BrightnessMath.SoftwareOpacity(percent);
            if (opacity <= 0.0)
            {
                Hide();
                return;
            }

            Opacity = opacity;
            if (!Visible)
            {
                Show();
            }

            TopMost = true;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmNcHitTest)
            {
                message.Result = HtTransparent;
                return;
            }

            base.WndProc(ref message);
        }
    }
}
