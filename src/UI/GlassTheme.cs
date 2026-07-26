using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ExtLume
{
    public static class GlassTheme
    {
        public static readonly Color BackgroundTop = Color.FromArgb(25, 27, 32);
        public static readonly Color BackgroundBottom = Color.FromArgb(8, 9, 12);
        public static readonly Color SurfaceTop = Color.FromArgb(58, 255, 255, 255);
        public static readonly Color SurfaceBottom = Color.FromArgb(20, 255, 255, 255);
        public static readonly Color SurfaceBorder = Color.FromArgb(66, 255, 255, 255);
        public static readonly Color TextPrimary = Color.FromArgb(248, 249, 250);
        public static readonly Color TextSecondary = Color.FromArgb(177, 182, 192);
        public static readonly Color Accent = Color.FromArgb(203, 255, 47);
        public static readonly Color AccentMuted = Color.FromArgb(112, 139, 37);
        public static readonly Color Success = Color.FromArgb(103, 224, 153);
        public static readonly Color Error = Color.FromArgb(255, 118, 126);
        public static readonly Color ButtonBackground = Color.FromArgb(52, 56, 51);

        public static GraphicsPath CreateRoundedPath(
            RectangleF bounds,
            float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = Math.Min(
                Math.Min(radius * 2F, bounds.Width),
                bounds.Height);
            if (diameter <= 1F)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            RectangleF arc = new RectangleF(
                bounds.X,
                bounds.Y,
                diameter,
                diameter);
            path.AddArc(arc, 180F, 90F);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270F, 90F);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0F, 90F);
            arc.X = bounds.X;
            path.AddArc(arc, 90F, 90F);
            path.CloseFigure();
            return path;
        }

        public static double ContrastRatio(Color first, Color second)
        {
            double firstLuminance = RelativeLuminance(first);
            double secondLuminance = RelativeLuminance(second);
            double lighter = Math.Max(firstLuminance, secondLuminance);
            double darker = Math.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05) / (darker + 0.05);
        }

        public static void ApplyDarkScrollBars(Control control)
        {
            if (control == null || !control.IsHandleCreated)
            {
                return;
            }

            WindowBackdrop.ApplyDarkScrollableControl(control.Handle);
        }

        private static double RelativeLuminance(Color color)
        {
            return (0.2126 * LinearChannel(color.R))
                + (0.7152 * LinearChannel(color.G))
                + (0.0722 * LinearChannel(color.B));
        }

        private static double LinearChannel(byte value)
        {
            double channel = value / 255.0;
            return channel <= 0.03928
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }

    public sealed class AuroraBackgroundPanel : Panel
    {
        public AuroraBackgroundPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            Graphics graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush background = new LinearGradientBrush(
                ClientRectangle,
                GlassTheme.BackgroundTop,
                GlassTheme.BackgroundBottom,
                100F))
            {
                graphics.FillRectangle(background, ClientRectangle);
            }

            DrawGlow(
                graphics,
                new RectangleF(
                    ClientSize.Width * 0.50F,
                    -ClientSize.Height * 0.34F,
                    ClientSize.Width * 0.75F,
                    ClientSize.Height * 0.72F),
                Color.FromArgb(118, 203, 255, 47));
            DrawGlow(
                graphics,
                new RectangleF(
                    -ClientSize.Width * 0.34F,
                    ClientSize.Height * 0.55F,
                    ClientSize.Width * 0.78F,
                    ClientSize.Height * 0.66F),
                Color.FromArgb(70, 85, 136, 255));
        }

        private static void DrawGlow(
            Graphics graphics,
            RectangleF bounds,
            Color centerColor)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using (PathGradientBrush glow = new PathGradientBrush(path))
                {
                    glow.CenterColor = centerColor;
                    glow.SurroundColors = new[]
                    {
                        Color.FromArgb(0, centerColor.R, centerColor.G, centerColor.B)
                    };
                    graphics.FillPath(glow, path);
                }
            }
        }
    }

    public class GlassPanel : Panel
    {
        private int cornerRadius;
        private bool strongSurface;
        private bool accentGlow;

        public GlassPanel()
        {
            cornerRadius = 22;
            BackColor = Color.Transparent;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.UserPaint,
                true);
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        public bool StrongSurface
        {
            get { return strongSurface; }
            set
            {
                strongSurface = value;
                Invalidate();
            }
        }

        public bool AccentGlow
        {
            get { return accentGlow; }
            set
            {
                accentGlow = value;
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            base.OnPaintBackground(eventArgs);
            if (ClientSize.Width < 4 || ClientSize.Height < 4)
            {
                return;
            }

            Graphics graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DpiLayout.PaintScale(this, graphics);
            RectangleF shadowBounds = new RectangleF(
                2F * scale,
                4F * scale,
                ClientSize.Width - (4F * scale),
                ClientSize.Height - (6F * scale));
            using (GraphicsPath shadowPath = GlassTheme.CreateRoundedPath(
                shadowBounds,
                cornerRadius * scale))
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(56, 0, 0, 0)))
            {
                graphics.FillPath(shadow, shadowPath);
            }

            RectangleF surfaceBounds = new RectangleF(
                1F * scale,
                1F * scale,
                ClientSize.Width - (3F * scale),
                ClientSize.Height - (5F * scale));
            using (GraphicsPath surfacePath = GlassTheme.CreateRoundedPath(
                surfaceBounds,
                cornerRadius * scale))
            {
                Color top = strongSurface
                    ? Color.FromArgb(72, 255, 255, 255)
                    : GlassTheme.SurfaceTop;
                Color bottom = strongSurface
                    ? Color.FromArgb(27, 255, 255, 255)
                    : GlassTheme.SurfaceBottom;
                using (LinearGradientBrush surface = new LinearGradientBrush(
                    surfaceBounds,
                    top,
                    bottom,
                    100F))
                {
                    graphics.FillPath(surface, surfacePath);
                }

                if (accentGlow)
                {
                    RectangleF glowBounds = new RectangleF(
                        surfaceBounds.Right - (220F * scale),
                        surfaceBounds.Top - (110F * scale),
                        270F * scale,
                        210F * scale);
                    using (GraphicsPath glowPath = new GraphicsPath())
                    {
                        glowPath.AddEllipse(glowBounds);
                        using (PathGradientBrush glow =
                            new PathGradientBrush(glowPath))
                        {
                            glow.CenterColor = Color.FromArgb(
                                50,
                                GlassTheme.Accent);
                            glow.SurroundColors = new[]
                            {
                                Color.FromArgb(0, GlassTheme.Accent)
                            };
                            graphics.SetClip(surfacePath);
                            graphics.FillPath(glow, glowPath);
                            graphics.ResetClip();
                        }
                    }
                }

                using (Pen border = new Pen(
                    GlassTheme.SurfaceBorder,
                    Math.Max(1F, scale)))
                {
                    graphics.DrawPath(border, surfacePath);
                }

                using (Pen highlight = new Pen(
                    Color.FromArgb(54, 255, 255, 255),
                    Math.Max(1F, scale)))
                {
                    RectangleF highlightBounds = surfaceBounds;
                    highlightBounds.Inflate(-scale, -scale);
                    using (GraphicsPath highlightPath =
                        GlassTheme.CreateRoundedPath(
                            highlightBounds,
                            Math.Max(0F, (cornerRadius - 1F) * scale)))
                    {
                        graphics.DrawPath(highlight, highlightPath);
                    }
                }
            }
        }
    }

    public sealed class GlassButton : Button
    {
        private bool pointerOver;
        private bool pointerDown;

        public GlassButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.UserPaint,
                true);
            BackColor = GlassTheme.ButtonBackground;
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = GlassTheme.TextPrimary;
            UseMnemonic = false;
        }

        protected override void OnSizeChanged(EventArgs eventArgs)
        {
            base.OnSizeChanged(eventArgs);
            UpdateWindowRegion();
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            UpdateWindowRegion();
        }

        protected override void OnMouseEnter(EventArgs eventArgs)
        {
            pointerOver = true;
            Invalidate();
            base.OnMouseEnter(eventArgs);
        }

        protected override void OnMouseLeave(EventArgs eventArgs)
        {
            pointerOver = false;
            pointerDown = false;
            Invalidate();
            base.OnMouseLeave(eventArgs);
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                pointerDown = true;
                Invalidate();
            }

            base.OnMouseDown(eventArgs);
        }

        protected override void OnMouseUp(MouseEventArgs eventArgs)
        {
            pointerDown = false;
            Invalidate();
            base.OnMouseUp(eventArgs);
        }

        protected override void OnEnabledChanged(EventArgs eventArgs)
        {
            Invalidate();
            base.OnEnabledChanged(eventArgs);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            Graphics graphics = eventArgs.Graphics;
            graphics.Clear(BackColor);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DpiLayout.PaintScale(this, graphics);
            RectangleF bounds = new RectangleF(
                scale,
                scale,
                ClientSize.Width - (3F * scale),
                ClientSize.Height - (3F * scale));
            Color fill;
            Color border;
            if (!Enabled)
            {
                fill = Color.FromArgb(18, 255, 255, 255);
                border = Color.FromArgb(32, 255, 255, 255);
            }
            else if (pointerDown)
            {
                fill = Color.FromArgb(72, GlassTheme.Accent);
                border = Color.FromArgb(180, GlassTheme.Accent);
            }
            else if (pointerOver)
            {
                fill = Color.FromArgb(44, 255, 255, 255);
                border = Color.FromArgb(90, 255, 255, 255);
            }
            else
            {
                fill = Color.FromArgb(25, 255, 255, 255);
                border = Color.FromArgb(60, 255, 255, 255);
            }

            using (GraphicsPath path = GlassTheme.CreateRoundedPath(
                bounds,
                Math.Max(8F * scale, ClientSize.Height / 2F)))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, Math.Max(1F, scale)))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            Color textColor = Enabled
                ? GlassTheme.TextPrimary
                : Color.FromArgb(105, GlassTheme.TextSecondary);
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                Rectangle.Round(bounds),
                textColor,
                TextFormatFlags.HorizontalCenter
                    | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.EndEllipsis
                    | TextFormatFlags.NoPrefix);

            if (Focused && ShowFocusCues)
            {
                Rectangle focus = Rectangle.Inflate(
                    Rectangle.Round(bounds),
                    -DpiLayout.ScaleLogical(4, DpiLayout.GetWindowDpi(this)),
                    -DpiLayout.ScaleLogical(4, DpiLayout.GetWindowDpi(this)));
                ControlPaint.DrawFocusRectangle(
                    graphics,
                    focus,
                    GlassTheme.Accent,
                    Color.Transparent);
            }
        }

        private void UpdateWindowRegion()
        {
            if (ClientSize.Width < 2 || ClientSize.Height < 2)
            {
                return;
            }

            using (GraphicsPath path = GlassTheme.CreateRoundedPath(
                new RectangleF(
                    0F,
                    0F,
                    ClientSize.Width,
                    ClientSize.Height),
                ClientSize.Height / 2F))
            {
                Region previous = Region;
                Region = new Region(path);
                if (previous != null)
                {
                    previous.Dispose();
                }
            }
        }
    }

    public sealed class GlassBadge : Control
    {
        private bool hardwareStyle;

        public GlassBadge()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.UserPaint,
                true);
            AutoSize = true;
            BackColor = Color.Transparent;
            ForeColor = GlassTheme.TextPrimary;
        }

        public bool HardwareStyle
        {
            get { return hardwareStyle; }
            set
            {
                hardwareStyle = value;
                Invalidate();
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int dpi = DpiLayout.GetWindowDpi(this);
            Size textSize = TextRenderer.MeasureText(
                String.IsNullOrEmpty(Text) ? " " : Text,
                Font,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            return new Size(
                textSize.Width + DpiLayout.ScaleLogical(22, dpi),
                Math.Max(
                    DpiLayout.ScaleLogical(26, dpi),
                    textSize.Height + DpiLayout.ScaleLogical(8, dpi)));
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            if (AutoSize)
            {
                Size = GetPreferredSize(Size.Empty);
            }
        }

        protected override void OnTextChanged(EventArgs eventArgs)
        {
            base.OnTextChanged(eventArgs);
            if (AutoSize)
            {
                Size = GetPreferredSize(Size.Empty);
            }

            Invalidate();
        }

        protected override void OnFontChanged(EventArgs eventArgs)
        {
            base.OnFontChanged(eventArgs);
            if (AutoSize)
            {
                Size = GetPreferredSize(Size.Empty);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            Graphics graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DpiLayout.PaintScale(this, graphics);
            RectangleF bounds = new RectangleF(
                0.5F * scale,
                0.5F * scale,
                ClientSize.Width - scale,
                ClientSize.Height - scale);
            Color fill = hardwareStyle
                ? Color.FromArgb(38, GlassTheme.Accent)
                : Color.FromArgb(30, 255, 255, 255);
            Color border = hardwareStyle
                ? Color.FromArgb(105, GlassTheme.Accent)
                : Color.FromArgb(54, 255, 255, 255);
            Color textColor = hardwareStyle
                ? GlassTheme.Accent
                : GlassTheme.TextSecondary;
            using (GraphicsPath path = GlassTheme.CreateRoundedPath(
                bounds,
                ClientSize.Height / 2F))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, Math.Max(1F, scale)))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                ClientRectangle,
                textColor,
                TextFormatFlags.HorizontalCenter
                    | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.NoPadding
                    | TextFormatFlags.NoPrefix);
        }
    }

    internal static class WindowBackdrop
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwaSystemBackdropType = 38;

        internal static void Apply(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int darkModeResult = SetAttribute(
                    windowHandle,
                    DwmwaUseImmersiveDarkMode,
                    1);
                if (darkModeResult != 0)
                {
                    SetAttribute(
                        windowHandle,
                        DwmwaUseImmersiveDarkModeLegacy,
                        1);
                }

                SetAttribute(windowHandle, DwmwaWindowCornerPreference, 2);
                SetAttribute(windowHandle, DwmwaSystemBackdropType, 2);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        internal static void ApplyDarkScrollableControl(IntPtr controlHandle)
        {
            if (controlHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                NativeMethods.SetWindowTheme(
                    controlHandle,
                    "DarkMode_Explorer",
                    null);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private static int SetAttribute(
            IntPtr windowHandle,
            int attribute,
            int value)
        {
            return NativeMethods.DwmSetWindowAttribute(
                windowHandle,
                attribute,
                ref value,
                sizeof(int));
        }
    }
}
