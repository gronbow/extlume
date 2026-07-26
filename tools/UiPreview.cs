using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using ExtLume;

namespace ExtLume.Tools
{
    internal static class UiPreview
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args != null
                && args.Length > 0
                && String.Equals(args[0], "--english", StringComparison.OrdinalIgnoreCase))
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            UiText text = new UiText();
            Form form = new Form();
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.BackColor = Color.FromArgb(246, 247, 249);
            form.ClientSize = new Size(570, 470);
            form.Font = text.CreateUiFont(9F, FontStyle.Regular);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Text = "UI preview";

            FlowLayoutPanel list = new FlowLayoutPanel();
            list.AutoScroll = true;
            list.Dock = DockStyle.Fill;
            list.FlowDirection = FlowDirection.TopDown;
            list.Padding = new Padding(16);
            list.WrapContents = false;
            form.Controls.Add(list);

            DisplayTarget firstTarget = new DisplayTarget
            {
                Id = "preview-hardware",
                FriendlyName = "H25T7-3",
                IsExternal = true,
                Bounds = new Rectangle(1920, 0, 1920, 1080)
            };
            MonitorCard hardware = new MonitorCard(
                new MonitorDescriptor
                {
                    Id = "preview-hardware",
                    DisplayName = "H25T7-3",
                    Target = firstTarget,
                    ControlKind = BrightnessControlKind.HardwareVcp,
                    MinimumRaw = 0,
                    MaximumRaw = 100,
                    CurrentRaw = 25,
                    CurrentPercent = 25
                },
                text);

            DisplayTarget secondTarget = new DisplayTarget
            {
                Id = "preview-software",
                FriendlyName = "DELL U2723QE",
                IsExternal = true,
                Bounds = new Rectangle(3840, 0, 2560, 1440)
            };
            MonitorCard software = new MonitorCard(
                new MonitorDescriptor
                {
                    Id = "preview-software",
                    DisplayName = "DELL U2723QE",
                    Target = secondTarget,
                    ControlKind = BrightnessControlKind.SoftwareDimming,
                    MinimumRaw = 0,
                    MaximumRaw = 100,
                    CurrentRaw = 70,
                    CurrentPercent = 70
                },
                text);

            hardware.Width = 510;
            software.Width = 510;
            list.Controls.Add(hardware);
            list.Controls.Add(software);
            Application.Run(form);
        }
    }
}
