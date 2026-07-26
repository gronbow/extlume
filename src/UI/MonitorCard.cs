using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExtLume
{
    public sealed class BrightnessRequestEventArgs : EventArgs
    {
        public int Percent { get; private set; }

        public BrightnessRequestEventArgs(int percent)
        {
            Percent = BrightnessMath.ClampPercent(percent);
        }
    }

    public sealed class MonitorCard : GlassPanel
    {
        private readonly UiText text;
        private readonly Label nameLabel;
        private readonly GlassBadge methodBadge;
        private readonly Label percentLabel;
        private readonly Label explanationLabel;
        private readonly Label stateLabel;
        private readonly BrightnessSlider slider;
        private readonly Timer debounceTimer;
        private bool suppressEvents;
        private int confirmedPercent;

        public event EventHandler<BrightnessRequestEventArgs> BrightnessRequested;

        public MonitorDescriptor Monitor { get; private set; }

        public MonitorCard(MonitorDescriptor monitor, UiText uiText)
        {
            if (monitor == null)
            {
                throw new ArgumentNullException("monitor");
            }

            if (uiText == null)
            {
                throw new ArgumentNullException("uiText");
            }

            Monitor = monitor;
            text = uiText;
            confirmedPercent = BrightnessMath.ClampPercent(
                monitor.CurrentPercent);

            AccentGlow = monitor.UsesHardware;
            CornerRadius = 24;
            StrongSurface = true;
            Margin = new Padding(0, 0, 0, 14);
            Padding = new Padding(20, 15, 20, 13);
            Size = new Size(540, 204);
            AccessibleName = monitor.DisplayName;
            AccessibleRole = AccessibleRole.Grouping;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.BackColor = Color.Transparent;
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            layout.Dock = DockStyle.Fill;
            layout.Margin = new Padding(0);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 59F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            nameLabel = new Label();
            nameLabel.AutoEllipsis = true;
            nameLabel.BackColor = Color.Transparent;
            nameLabel.Dock = DockStyle.Fill;
            nameLabel.Font = text.CreateUiFont(12.5F, FontStyle.Bold);
            nameLabel.ForeColor = GlassTheme.TextPrimary;
            nameLabel.Margin = new Padding(0, 0, 10, 0);
            nameLabel.Text = monitor.DisplayName;
            nameLabel.TextAlign = ContentAlignment.MiddleLeft;

            methodBadge = new GlassBadge();
            methodBadge.AutoSize = true;
            methodBadge.Font = text.CreateUiFont(8.2F, FontStyle.Regular);
            methodBadge.HardwareStyle = monitor.UsesHardware;
            methodBadge.Margin = new Padding(0, 3, 10, 3);
            methodBadge.Text = monitor.UsesHardware
                ? text.HardwareDdc
                : text.SoftwareDimming;

            percentLabel = new Label();
            percentLabel.BackColor = Color.Transparent;
            percentLabel.Dock = DockStyle.Fill;
            percentLabel.Font = text.CreateUiFont(20F, FontStyle.Bold);
            percentLabel.ForeColor = GlassTheme.Accent;
            percentLabel.Margin = new Padding(4, 0, 0, 0);
            percentLabel.TextAlign = ContentAlignment.MiddleRight;

            explanationLabel = new Label();
            explanationLabel.AutoEllipsis = true;
            explanationLabel.BackColor = Color.Transparent;
            explanationLabel.Dock = DockStyle.Fill;
            explanationLabel.Font = text.CreateUiFont(8.7F, FontStyle.Regular);
            explanationLabel.ForeColor = GlassTheme.TextSecondary;
            explanationLabel.Margin = new Padding(0);
            explanationLabel.Text = monitor.UsesHardware
                ? text.HardwareNote
                : text.SoftwareDimmingNote;
            explanationLabel.TextAlign = ContentAlignment.MiddleLeft;

            slider = new BrightnessSlider();
            slider.AccessibleDescription = text.BrightnessSliderDescription;
            slider.AccessibleName = text.BrightnessSliderName(
                monitor.DisplayName);
            slider.Dock = DockStyle.Fill;
            slider.LargeChange = 10;
            slider.Margin = new Padding(0, 4, 0, 2);
            slider.Maximum = 100;
            slider.Minimum = 0;
            slider.SmallChange = 1;
            slider.ValueChanged += SliderValueChanged;
            slider.Scroll += SliderScroll;

            stateLabel = new Label();
            stateLabel.AutoEllipsis = true;
            stateLabel.BackColor = Color.Transparent;
            stateLabel.Dock = DockStyle.Fill;
            stateLabel.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            stateLabel.ForeColor = GlassTheme.TextSecondary;
            stateLabel.Margin = new Padding(1, 0, 0, 0);
            stateLabel.Text = StatusText(text.Ready);
            stateLabel.TextAlign = ContentAlignment.MiddleLeft;

            layout.Controls.Add(nameLabel, 0, 0);
            layout.SetColumnSpan(nameLabel, 2);
            layout.Controls.Add(percentLabel, 2, 0);
            layout.Controls.Add(methodBadge, 0, 1);
            layout.Controls.Add(explanationLabel, 1, 1);
            layout.SetColumnSpan(explanationLabel, 2);
            layout.Controls.Add(slider, 0, 2);
            layout.SetColumnSpan(slider, 3);
            layout.Controls.Add(stateLabel, 0, 3);
            layout.SetColumnSpan(stateLabel, 3);
            Controls.Add(layout);

            debounceTimer = new Timer();
            debounceTimer.Interval = 220;
            debounceTimer.Tick += DebounceTimerTick;
            SetSliderValue(confirmedPercent);
        }

        public void SetBusy(bool busy)
        {
            slider.Enabled = !busy;
            stateLabel.ForeColor = busy
                ? GlassTheme.Accent
                : GlassTheme.TextSecondary;
            stateLabel.Text = StatusText(busy ? text.Applying : text.Ready);
        }

        public void SetSuccess(int percent)
        {
            confirmedPercent = BrightnessMath.ClampPercent(percent);
            SetSliderValue(confirmedPercent);
            slider.Enabled = true;
            stateLabel.ForeColor = GlassTheme.Success;
            stateLabel.Text = StatusText(text.Applied);
        }

        public void SetFailure(string message)
        {
            SetSliderValue(confirmedPercent);
            slider.Enabled = true;
            stateLabel.ForeColor = GlassTheme.Error;
            stateLabel.Text = StatusText(message);
        }

        private void SliderValueChanged(object sender, EventArgs eventArgs)
        {
            percentLabel.Text = slider.Value + "%";
        }

        private void SliderScroll(object sender, EventArgs eventArgs)
        {
            if (suppressEvents)
            {
                return;
            }

            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private void DebounceTimerTick(object sender, EventArgs eventArgs)
        {
            debounceTimer.Stop();
            EventHandler<BrightnessRequestEventArgs> handler =
                BrightnessRequested;
            if (handler != null)
            {
                handler(this, new BrightnessRequestEventArgs(slider.Value));
            }
        }

        private void SetSliderValue(int percent)
        {
            suppressEvents = true;
            try
            {
                slider.Value = BrightnessMath.ClampPercent(percent);
                percentLabel.Text = slider.Value + "%";
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private static string StatusText(string value)
        {
            return "●  " + value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                debounceTimer.Stop();
                debounceTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
