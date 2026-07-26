using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ExtLume
{
    public sealed class BrightnessSlider : Control
    {
        private int minimum;
        private int maximum;
        private int currentValue;
        private int smallChange;
        private int largeChange;
        private bool dragging;

        public event EventHandler ValueChanged;
        public event EventHandler Scroll;

        public BrightnessSlider()
        {
            minimum = 0;
            maximum = 100;
            smallChange = 1;
            largeChange = 10;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.Selectable
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.UserPaint,
                true);
            Size = new Size(240, 48);
            MinimumSize = new Size(120, 42);
            BackColor = Color.Transparent;
            TabStop = true;
            AccessibleRole = AccessibleRole.Slider;
        }

        public int Minimum
        {
            get { return minimum; }
            set
            {
                if (value >= maximum)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                minimum = value;
                SetValueCore(currentValue, false);
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return maximum; }
            set
            {
                if (value <= minimum)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                maximum = value;
                SetValueCore(currentValue, false);
                Invalidate();
            }
        }

        public int Value
        {
            get { return currentValue; }
            set { SetValueCore(value, false); }
        }

        public int SmallChange
        {
            get { return smallChange; }
            set { smallChange = Math.Max(1, value); }
        }

        public int LargeChange
        {
            get { return largeChange; }
            set { largeChange = Math.Max(1, value); }
        }

        public int ValueFromClientX(int clientX)
        {
            RectangleF track = GetTrackBounds();
            if (track.Width <= 0F)
            {
                return minimum;
            }

            double ratio = (clientX - track.Left) / track.Width;
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            return minimum + (int)Math.Round(
                ratio * (maximum - minimum),
                MidpointRounding.AwayFromZero);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Left
                || key == Keys.Right
                || key == Keys.Up
                || key == Keys.Down
                || key == Keys.Home
                || key == Keys.End
                || key == Keys.PageUp
                || key == Keys.PageDown)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs eventArgs)
        {
            int requested = currentValue;
            switch (eventArgs.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    requested -= smallChange;
                    break;
                case Keys.Right:
                case Keys.Up:
                    requested += smallChange;
                    break;
                case Keys.PageDown:
                    requested -= largeChange;
                    break;
                case Keys.PageUp:
                    requested += largeChange;
                    break;
                case Keys.Home:
                    requested = minimum;
                    break;
                case Keys.End:
                    requested = maximum;
                    break;
                default:
                    base.OnKeyDown(eventArgs);
                    return;
            }

            eventArgs.Handled = true;
            eventArgs.SuppressKeyPress = true;
            SetValueCore(requested, true);
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            if (eventArgs.Button == MouseButtons.Left && Enabled)
            {
                Focus();
                dragging = true;
                Capture = true;
                SetValueCore(ValueFromClientX(eventArgs.X), true);
            }

            base.OnMouseDown(eventArgs);
        }

        protected override void OnMouseMove(MouseEventArgs eventArgs)
        {
            if (dragging && Enabled)
            {
                SetValueCore(ValueFromClientX(eventArgs.X), true);
            }

            base.OnMouseMove(eventArgs);
        }

        protected override void OnMouseUp(MouseEventArgs eventArgs)
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                dragging = false;
                Capture = false;
            }

            base.OnMouseUp(eventArgs);
        }

        protected override void OnMouseCaptureChanged(EventArgs eventArgs)
        {
            if (!Capture)
            {
                dragging = false;
            }

            base.OnMouseCaptureChanged(eventArgs);
        }

        protected override void OnMouseWheel(MouseEventArgs eventArgs)
        {
            if (Enabled)
            {
                int direction = Math.Sign(eventArgs.Delta);
                if (direction != 0)
                {
                    SetValueCore(
                        currentValue + (direction * smallChange),
                        true);
                    HandledMouseEventArgs handled =
                        eventArgs as HandledMouseEventArgs;
                    if (handled != null)
                    {
                        handled.Handled = true;
                    }
                }
            }

            base.OnMouseWheel(eventArgs);
        }

        protected override void OnGotFocus(EventArgs eventArgs)
        {
            Invalidate();
            base.OnGotFocus(eventArgs);
        }

        protected override void OnLostFocus(EventArgs eventArgs)
        {
            Invalidate();
            base.OnLostFocus(eventArgs);
        }

        protected override void OnEnabledChanged(EventArgs eventArgs)
        {
            if (!Enabled)
            {
                dragging = false;
                Capture = false;
            }

            Invalidate();
            base.OnEnabledChanged(eventArgs);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            Graphics graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DpiLayout.PaintScale(this, graphics);
            RectangleF track = GetTrackBounds();
            float thumbRadius = 9F * scale;
            float ratio = (currentValue - minimum)
                / (float)(maximum - minimum);
            float thumbCenter = track.Left + (track.Width * ratio);

            using (GraphicsPath trackPath = GlassTheme.CreateRoundedPath(
                track,
                track.Height / 2F))
            using (SolidBrush trackBrush = new SolidBrush(
                Enabled
                    ? Color.FromArgb(45, 255, 255, 255)
                    : Color.FromArgb(25, 255, 255, 255)))
            {
                graphics.FillPath(trackBrush, trackPath);
            }

            RectangleF filled = new RectangleF(
                track.Left,
                track.Top,
                Math.Max(track.Height, thumbCenter - track.Left),
                track.Height);
            using (GraphicsPath filledPath = GlassTheme.CreateRoundedPath(
                filled,
                filled.Height / 2F))
            using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                filled,
                Enabled ? Color.FromArgb(172, 237, 255, 92) : GlassTheme.AccentMuted,
                Enabled ? GlassTheme.Accent : GlassTheme.AccentMuted,
                0F))
            {
                graphics.FillPath(fillBrush, filledPath);
            }

            RectangleF glowBounds = new RectangleF(
                thumbCenter - thumbRadius - (5F * scale),
                (ClientSize.Height / 2F) - thumbRadius - (5F * scale),
                (thumbRadius + (5F * scale)) * 2F,
                (thumbRadius + (5F * scale)) * 2F);
            using (SolidBrush glow = new SolidBrush(
                Enabled
                    ? Color.FromArgb(Focused ? 46 : 28, GlassTheme.Accent)
                    : Color.FromArgb(0, GlassTheme.Accent)))
            {
                graphics.FillEllipse(glow, glowBounds);
            }

            RectangleF thumbBounds = new RectangleF(
                thumbCenter - thumbRadius,
                (ClientSize.Height / 2F) - thumbRadius,
                thumbRadius * 2F,
                thumbRadius * 2F);
            using (SolidBrush thumb = new SolidBrush(
                Enabled ? GlassTheme.TextPrimary : Color.FromArgb(112, 116, 124)))
            using (Pen ring = new Pen(
                Enabled ? GlassTheme.Accent : GlassTheme.AccentMuted,
                (Focused ? 3F : 2F) * scale))
            {
                graphics.FillEllipse(thumb, thumbBounds);
                graphics.DrawEllipse(ring, thumbBounds);
            }

            if (Focused && ShowFocusCues)
            {
                Rectangle focusBounds = Rectangle.Inflate(
                    Rectangle.Round(track),
                    DpiLayout.ScaleLogical(
                        4,
                        DpiLayout.GetWindowDpi(this)),
                    DpiLayout.ScaleLogical(
                        8,
                        DpiLayout.GetWindowDpi(this)));
                ControlPaint.DrawFocusRectangle(
                    graphics,
                    focusBounds,
                    GlassTheme.Accent,
                    Color.Transparent);
            }
        }

        private RectangleF GetTrackBounds()
        {
            float scale = DpiLayout.ScaleFactor(
                DpiLayout.LogicalDpi,
                DpiLayout.GetWindowDpi(this));
            float horizontalInset = 13F * scale;
            float trackHeight = 7F * scale;
            return new RectangleF(
                horizontalInset,
                (ClientSize.Height - trackHeight) / 2F,
                Math.Max(1F, ClientSize.Width - (horizontalInset * 2F)),
                trackHeight);
        }

        private void SetValueCore(int requested, bool userInitiated)
        {
            int clamped = Math.Max(minimum, Math.Min(maximum, requested));
            if (clamped == currentValue)
            {
                return;
            }

            currentValue = clamped;
            Invalidate();
            EventHandler changed = ValueChanged;
            if (changed != null)
            {
                changed(this, EventArgs.Empty);
            }

            if (userInitiated)
            {
                EventHandler scrolled = Scroll;
                if (scrolled != null)
                {
                    scrolled(this, EventArgs.Empty);
                }
            }

            if (AccessibilityObject != null)
            {
                AccessibilityNotifyClients(
                    AccessibleEvents.ValueChange,
                    -1);
            }
        }
    }
}
