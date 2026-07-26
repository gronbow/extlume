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

    public sealed class MonitorCard : UserControl
    {
        private readonly UiText text;
        private readonly Label nameLabel;
        private readonly Label methodLabel;
        private readonly Label percentLabel;
        private readonly Label explanationLabel;
        private readonly Label stateLabel;
        private readonly TrackBar slider;
        private readonly Timer debounceTimer;
        private bool suppressEvents;
        private int confirmedPercent;

        public event EventHandler<BrightnessRequestEventArgs> BrightnessRequested;

        public MonitorDescriptor Monitor { get; private set; }

        public MonitorCard(MonitorDescriptor monitor, UiText uiText)
        {
            Monitor = monitor;
            text = uiText;
            confirmedPercent = monitor.CurrentPercent;

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Margin = new Padding(0, 0, 0, 12);
            Padding = new Padding(14, 12, 14, 10);
            Size = new Size(500, 190);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            nameLabel = new Label();
            nameLabel.AutoEllipsis = true;
            nameLabel.Dock = DockStyle.Fill;
            nameLabel.Font = text.CreateUiFont(11.5F, FontStyle.Bold);
            nameLabel.Text = monitor.DisplayName;
            nameLabel.TextAlign = ContentAlignment.MiddleLeft;

            methodLabel = new Label();
            methodLabel.AutoSize = true;
            methodLabel.BackColor = monitor.UsesHardware
                ? Color.FromArgb(228, 244, 255)
                : Color.FromArgb(241, 241, 241);
            methodLabel.ForeColor = monitor.UsesHardware
                ? Color.FromArgb(0, 92, 153)
                : Color.FromArgb(72, 72, 72);
            methodLabel.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            methodLabel.Margin = new Padding(0, 2, 8, 2);
            methodLabel.Padding = new Padding(6, 2, 6, 2);
            methodLabel.Text = monitor.UsesHardware ? text.HardwareDdc : text.SoftwareDimming;

            percentLabel = new Label();
            percentLabel.Dock = DockStyle.Fill;
            percentLabel.Font = text.CreateUiFont(11.5F, FontStyle.Bold);
            percentLabel.ForeColor = Color.FromArgb(20, 116, 204);
            percentLabel.TextAlign = ContentAlignment.MiddleRight;

            explanationLabel = new Label();
            explanationLabel.AutoEllipsis = true;
            explanationLabel.Dock = DockStyle.Fill;
            explanationLabel.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            explanationLabel.ForeColor = Color.FromArgb(92, 92, 92);
            explanationLabel.Margin = new Padding(6, 0, 0, 0);
            explanationLabel.Text = monitor.UsesHardware
                ? text.HardwareNote
                : text.SoftwareDimmingNote;
            explanationLabel.TextAlign = ContentAlignment.MiddleLeft;

            slider = new TrackBar();
            slider.AutoSize = false;
            slider.Dock = DockStyle.Fill;
            slider.LargeChange = 10;
            slider.Maximum = 100;
            slider.Minimum = 0;
            slider.SmallChange = 1;
            slider.TickFrequency = 10;
            slider.TickStyle = TickStyle.BottomRight;
            slider.ValueChanged += SliderValueChanged;
            slider.Scroll += SliderScroll;

            stateLabel = new Label();
            stateLabel.Dock = DockStyle.Fill;
            stateLabel.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            stateLabel.ForeColor = Color.FromArgb(90, 90, 90);
            stateLabel.Text = text.Ready;
            stateLabel.TextAlign = ContentAlignment.MiddleLeft;

            layout.Controls.Add(nameLabel, 0, 0);
            layout.SetColumnSpan(nameLabel, 2);
            layout.Controls.Add(percentLabel, 2, 0);
            layout.Controls.Add(methodLabel, 0, 1);
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
            SetSliderValue(monitor.CurrentPercent);
        }

        public void SetBusy(bool busy)
        {
            slider.Enabled = !busy;
            stateLabel.ForeColor = Color.FromArgb(90, 90, 90);
            stateLabel.Text = busy ? text.Applying : text.Ready;
        }

        public void SetSuccess(int percent)
        {
            confirmedPercent = BrightnessMath.ClampPercent(percent);
            SetSliderValue(confirmedPercent);
            slider.Enabled = true;
            stateLabel.ForeColor = Color.FromArgb(36, 122, 72);
            stateLabel.Text = text.Applied;
        }

        public void SetFailure(string message)
        {
            SetSliderValue(confirmedPercent);
            slider.Enabled = true;
            stateLabel.ForeColor = Color.FromArgb(176, 45, 45);
            stateLabel.Text = message;
        }

        private void SliderValueChanged(object sender, EventArgs e)
        {
            percentLabel.Text = slider.Value + "%";
        }

        private void SliderScroll(object sender, EventArgs e)
        {
            if (suppressEvents)
            {
                return;
            }

            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private void DebounceTimerTick(object sender, EventArgs e)
        {
            debounceTimer.Stop();
            EventHandler<BrightnessRequestEventArgs> handler = BrightnessRequested;
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
