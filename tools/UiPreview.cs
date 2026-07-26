using System;
using System.Drawing;
using System.Globalization;
using System.IO;
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
            if (HasArgument(args, "--english"))
            {
                Thread.CurrentThread.CurrentCulture =
                    new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo("en-US");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form preview = BuildPreview();
            float scale = GetScale(args);
            if (scale > 1F)
            {
                preview.Scale(new SizeF(scale, scale));
            }

            string capturePath = GetArgumentValue(args, "--capture=");
            if (!String.IsNullOrEmpty(capturePath))
            {
                preview.Shown += delegate
                {
                    preview.BeginInvoke((MethodInvoker)delegate
                    {
                        CaptureAndClose(preview, capturePath);
                    });
                };
            }

            Application.Run(preview);
        }

        private static bool HasArgument(string[] args, string expected)
        {
            if (args == null)
            {
                return false;
            }

            for (int index = 0; index < args.Length; index++)
            {
                if (String.Equals(
                    args[index],
                    expected,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetArgumentValue(string[] args, string prefix)
        {
            if (args == null)
            {
                return null;
            }

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] != null
                    && args[index].StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return args[index].Substring(prefix.Length);
                }
            }

            return null;
        }

        private static float GetScale(string[] args)
        {
            string value = GetArgumentValue(args, "--scale=");
            int percent;
            if (!Int32.TryParse(value, out percent))
            {
                return 1F;
            }

            percent = Math.Max(100, Math.Min(250, percent));
            return percent / 100F;
        }

        private static void CaptureAndClose(Form form, string capturePath)
        {
            string fullPath = Path.GetFullPath(capturePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (Bitmap bitmap = new Bitmap(
                form.Width,
                form.Height))
            {
                form.DrawToBitmap(
                    bitmap,
                    new Rectangle(Point.Empty, form.Size));
                bitmap.Save(
                    fullPath,
                    System.Drawing.Imaging.ImageFormat.Png);
            }

            form.Close();
        }

        private static Form BuildPreview()
        {
            UiText text = new UiText();
            Form form = new Form();
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.BackColor = GlassTheme.BackgroundBottom;
            form.ClientSize = new Size(640, 560);
            form.Font = text.CreateUiFont(9F, FontStyle.Regular);
            form.MaximizeBox = false;
            form.MinimumSize = new Size(560, 430);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Text = "ExtLume · UI preview";

            AuroraBackgroundPanel background = new AuroraBackgroundPanel();
            background.Dock = DockStyle.Fill;
            background.Padding = new Padding(18);
            form.Controls.Add(background);

            TableLayoutPanel root = new TableLayoutPanel();
            root.BackColor = Color.Transparent;
            root.ColumnCount = 1;
            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            root.Dock = DockStyle.Fill;
            root.Margin = new Padding(0);
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            background.Controls.Add(root);

            GlassPanel header = BuildHeader(text);
            root.Controls.Add(header, 0, 0);

            FlowLayoutPanel list = new FlowLayoutPanel();
            list.AutoScroll = true;
            list.BackColor = Color.Transparent;
            list.Dock = DockStyle.Fill;
            list.FlowDirection = FlowDirection.TopDown;
            list.Margin = new Padding(0);
            list.Padding = new Padding(0, 4, 0, 2);
            list.WrapContents = false;
            list.HandleCreated += delegate
            {
                GlassTheme.ApplyDarkScrollBars(list);
            };
            root.Controls.Add(list, 0, 1);

            MonitorCard hardware = new MonitorCard(
                CreateMonitor(
                    "preview-hardware",
                    "H25T7-3",
                    BrightnessControlKind.HardwareVcp,
                    25),
                text);
            MonitorCard software = new MonitorCard(
                CreateMonitor(
                    "preview-software",
                    "DELL U2723QE",
                    BrightnessControlKind.SoftwareDimming,
                    70),
                text);
            list.Controls.Add(hardware);
            list.Controls.Add(software);
            ResizeCards(list);
            list.Resize += delegate { ResizeCards(list); };

            GlassPanel statusPanel = new GlassPanel();
            statusPanel.CornerRadius = 15;
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.Margin = new Padding(0, 7, 0, 0);
            Label status = new Label();
            status.AutoEllipsis = true;
            status.BackColor = Color.Transparent;
            status.Dock = DockStyle.Fill;
            status.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            status.ForeColor = GlassTheme.TextSecondary;
            status.Padding = new Padding(15, 0, 15, 0);
            status.Text = "●  " + text.DisplaysReady(2);
            status.TextAlign = ContentAlignment.MiddleLeft;
            statusPanel.Controls.Add(status);
            root.Controls.Add(statusPanel, 0, 2);
            return form;
        }

        private static GlassPanel BuildHeader(UiText text)
        {
            GlassPanel header = new GlassPanel();
            header.AccentGlow = true;
            header.CornerRadius = 26;
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0, 0, 0, 12);
            header.Padding = new Padding(22, 15, 22, 14);
            header.StrongSurface = true;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.BackColor = Color.Transparent;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 122F));
            layout.Dock = DockStyle.Fill;
            layout.Margin = new Padding(0);
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label eyebrow = CreateLabel(
                text.Eyebrow,
                text.CreateUiFont(8F, FontStyle.Bold),
                GlassTheme.Accent);
            eyebrow.Margin = new Padding(1, 0, 8, 0);
            Label heading = CreateLabel(
                text.Heading,
                text.CreateUiFont(18F, FontStyle.Bold),
                GlassTheme.TextPrimary);
            heading.Margin = new Padding(0, 0, 8, 0);
            Label intro = CreateLabel(
                text.Intro,
                text.CreateUiFont(9F, FontStyle.Regular),
                GlassTheme.TextSecondary);
            intro.Margin = new Padding(1, 0, 0, 0);

            GlassButton button = new GlassButton();
            button.Dock = DockStyle.Fill;
            button.Font = text.CreateUiFont(9F, FontStyle.Bold);
            button.Margin = new Padding(10, 8, 0, 10);
            button.Text = text.Refresh;

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(heading, 0, 1);
            layout.Controls.Add(button, 1, 0);
            layout.SetRowSpan(button, 2);
            layout.Controls.Add(intro, 0, 2);
            layout.SetColumnSpan(intro, 2);
            header.Controls.Add(layout);
            return header;
        }

        private static Label CreateLabel(
            string value,
            Font font,
            Color color)
        {
            Label label = new Label();
            label.AutoEllipsis = true;
            label.BackColor = Color.Transparent;
            label.Dock = DockStyle.Fill;
            label.Font = font;
            label.ForeColor = color;
            label.Text = value;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static MonitorDescriptor CreateMonitor(
            string id,
            string name,
            BrightnessControlKind controlKind,
            int percent)
        {
            DisplayTarget target = new DisplayTarget
            {
                Id = id,
                FriendlyName = name,
                IsExternal = true,
                Bounds = new Rectangle(1920, 0, 1920, 1080)
            };
            return new MonitorDescriptor
            {
                Id = id,
                DisplayName = name,
                Target = target,
                ControlKind = controlKind,
                MinimumRaw = 0,
                MaximumRaw = 100,
                CurrentRaw = (uint)percent,
                CurrentPercent = percent
            };
        }

        private static void ResizeCards(FlowLayoutPanel list)
        {
            int width = list.ClientSize.Width
                - list.Padding.Left
                - list.Padding.Right
                - SystemInformation.VerticalScrollBarWidth
                - 4;
            width = Math.Max(430, width);
            for (int index = 0; index < list.Controls.Count; index++)
            {
                list.Controls[index].Width = width;
            }
        }
    }
}
