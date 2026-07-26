using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ExtLume
{
    public sealed class MainForm : Form
    {
        private readonly UiText text;
        private readonly MonitorManager monitorManager;
        private readonly SoftwareDimmingService dimmingService;
        private readonly AppSettings settings;
        private readonly SemaphoreSlim hardwareGate;
        private readonly EventWaitHandle showEvent;
        private readonly bool startHidden;
        private readonly FlowLayoutPanel monitorList;
        private readonly Label statusLabel;
        private readonly Button refreshButton;
        private readonly NotifyIcon trayIcon;
        private readonly ToolStripMenuItem startupMenuItem;
        private readonly System.Windows.Forms.Timer topologyTimer;
        private RegisteredWaitHandle showRegistration;
        private bool allowExit;
        private bool refreshInProgress;
        private bool refreshRequested;
        private bool trayNoticeShown;
        private int layoutDpi;
        private int fontDpi;
        private readonly int fontReferenceDpi;
        private const int DdcProbeTimeoutMilliseconds = 4000;
        private const int DdcWriteTimeoutMilliseconds = 5000;
        private const int DdcGateWaitMilliseconds = 350;
        private const int WmDpiChanged = 0x02E0;

        public MainForm(EventWaitHandle instanceShowEvent, bool shouldStartHidden)
            : this(instanceShowEvent, shouldStartHidden, null)
        {
        }

        public MainForm(
            EventWaitHandle instanceShowEvent,
            bool shouldStartHidden,
            string languageOverride)
        {
            text = new UiText(languageOverride);
            monitorManager = new MonitorManager();
            dimmingService = new SoftwareDimmingService();
            settings = new AppSettings();
            hardwareGate = new SemaphoreSlim(1, 1);
            showEvent = instanceShowEvent;
            startHidden = shouldStartHidden;
            layoutDpi = DpiLayout.LogicalDpi;
            fontReferenceDpi = DpiLayout.GetSystemDpi();
            fontDpi = fontReferenceDpi;

            AutoScaleMode = AutoScaleMode.None;
            BackColor = GlassTheme.BackgroundBottom;
            ClientSize = new Size(640, 560);
            DoubleBuffered = true;
            Font = text.CreateUiFont(9F, FontStyle.Regular);
            Icon = ExtractApplicationIcon();
            MaximizeBox = false;
            MinimumSize = new Size(560, 430);
            StartPosition = FormStartPosition.CenterScreen;
            Text = text.WindowTitle;

            AuroraBackgroundPanel background = new AuroraBackgroundPanel();
            background.Dock = DockStyle.Fill;
            background.Padding = new Padding(18);

            TableLayoutPanel root = new TableLayoutPanel();
            root.BackColor = Color.Transparent;
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.Dock = DockStyle.Fill;
            root.Margin = new Padding(0);
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            Panel header = BuildHeader();
            root.Controls.Add(header, 0, 0);

            monitorList = new FlowLayoutPanel();
            monitorList.AutoScroll = true;
            monitorList.BackColor = Color.Transparent;
            monitorList.Dock = DockStyle.Fill;
            monitorList.FlowDirection = FlowDirection.TopDown;
            monitorList.Margin = new Padding(0);
            monitorList.Padding = new Padding(0, 4, 0, 2);
            monitorList.WrapContents = false;
            monitorList.HandleCreated += delegate
            {
                GlassTheme.ApplyDarkScrollBars(monitorList);
            };
            monitorList.Resize += MonitorListResize;
            root.Controls.Add(monitorList, 0, 1);

            GlassPanel statusPanel = new GlassPanel();
            statusPanel.CornerRadius = 15;
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.Margin = new Padding(0, 7, 0, 0);

            statusLabel = new Label();
            statusLabel.AutoEllipsis = true;
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            statusLabel.ForeColor = GlassTheme.TextSecondary;
            statusLabel.Padding = new Padding(15, 0, 15, 0);
            statusLabel.Text = "●  " + text.Refreshing;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusPanel.Controls.Add(statusLabel);
            root.Controls.Add(statusPanel, 0, 2);
            background.Controls.Add(root);
            Controls.Add(background);

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem openItem = new ToolStripMenuItem(text.Open);
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            openItem.Click += delegate { ShowWindow(); };
            ToolStripMenuItem rescanItem = new ToolStripMenuItem(text.Refresh);
            rescanItem.Click += delegate
            {
                ShowWindow();
                RequestRefresh();
            };
            startupMenuItem = new ToolStripMenuItem(text.StartWithWindows);
            startupMenuItem.CheckOnClick = false;
            startupMenuItem.Click += StartupMenuItemClick;
            ToolStripMenuItem exitItem = new ToolStripMenuItem(text.Exit);
            exitItem.Click += delegate { ExitApplication(); };
            trayMenu.Items.Add(openItem);
            trayMenu.Items.Add(rescanItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(startupMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);
            trayMenu.Opening += delegate
            {
                startupMenuItem.Checked = settings.IsStartWithWindowsEnabled();
            };

            trayIcon = new NotifyIcon();
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Icon = Icon;
            trayIcon.Text = AppInfo.ShortName;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { ShowWindow(); };

            topologyTimer = new System.Windows.Forms.Timer();
            topologyTimer.Interval = 900;
            topologyTimer.Tick += delegate
            {
                topologyTimer.Stop();
                RequestRefresh();
            };

            refreshButton = FindRefreshButton(header);
            Load += MainFormLoad;
            Shown += MainFormShown;

            SystemEvents.DisplaySettingsChanged += SystemEventsDisplaySettingsChanged;
            SystemEvents.PowerModeChanged += SystemEventsPowerModeChanged;

            if (showEvent != null)
            {
                showRegistration = ThreadPool.RegisterWaitForSingleObject(
                    showEvent,
                    InstanceShowEventSignaled,
                    null,
                    Timeout.Infinite,
                    false);
            }
        }

        private Panel BuildHeader()
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

            Label eyebrow = new Label();
            eyebrow.AutoEllipsis = true;
            eyebrow.BackColor = Color.Transparent;
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = text.CreateUiFont(8F, FontStyle.Bold);
            eyebrow.ForeColor = GlassTheme.Accent;
            eyebrow.Margin = new Padding(1, 0, 8, 0);
            eyebrow.Text = text.Eyebrow;
            eyebrow.TextAlign = ContentAlignment.MiddleLeft;

            Label heading = new Label();
            heading.AutoEllipsis = true;
            heading.BackColor = Color.Transparent;
            heading.Dock = DockStyle.Fill;
            heading.Font = text.CreateUiFont(18F, FontStyle.Bold);
            heading.ForeColor = GlassTheme.TextPrimary;
            heading.Margin = new Padding(0, 0, 8, 0);
            heading.Text = text.Heading;
            heading.TextAlign = ContentAlignment.MiddleLeft;

            Label intro = new Label();
            intro.AutoEllipsis = true;
            intro.BackColor = Color.Transparent;
            intro.Dock = DockStyle.Fill;
            intro.Font = text.CreateUiFont(9F, FontStyle.Regular);
            intro.ForeColor = GlassTheme.TextSecondary;
            intro.Margin = new Padding(1, 0, 0, 0);
            intro.Text = text.Intro;
            intro.TextAlign = ContentAlignment.MiddleLeft;

            GlassButton button = new GlassButton();
            button.AccessibleDescription = text.RefreshAccessibleDescription;
            button.AccessibleName = text.Refresh;
            button.AutoSize = false;
            button.Dock = DockStyle.Fill;
            button.Font = text.CreateUiFont(9F, FontStyle.Bold);
            button.Margin = new Padding(10, 8, 0, 10);
            button.Name = "RefreshButton";
            button.Text = text.Refresh;
            button.Click += delegate { RequestRefresh(); };

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(heading, 0, 1);
            layout.Controls.Add(button, 1, 0);
            layout.SetRowSpan(button, 2);
            layout.Controls.Add(intro, 0, 2);
            layout.SetColumnSpan(intro, 2);
            header.Controls.Add(layout);
            return header;
        }

        private static Button FindRefreshButton(Control parent)
        {
            Control[] matches = parent.Controls.Find("RefreshButton", true);
            return matches.Length > 0 ? matches[0] as Button : null;
        }

        private async void MainFormLoad(object sender, EventArgs e)
        {
            startupMenuItem.Checked = settings.IsStartWithWindowsEnabled();
            await RefreshMonitorsAsync();
        }

        private void MainFormShown(object sender, EventArgs e)
        {
            if (startHidden)
            {
                Hide();
            }
        }

        private async void RequestRefresh()
        {
            if (refreshInProgress)
            {
                refreshRequested = true;
                return;
            }

            await RefreshMonitorsAsync();
        }

        private async Task RefreshMonitorsAsync()
        {
            if (refreshInProgress)
            {
                refreshRequested = true;
                return;
            }

            refreshInProgress = true;
            try
            {
                do
                {
                    refreshRequested = false;
                    if (refreshButton != null)
                    {
                        refreshButton.Enabled = false;
                    }

                    statusLabel.Text = "●  " + text.Refreshing;
                    MonitorRefreshResult refreshResult =
                        await BuildRefreshResultAsync();

                    if (!IsDisposed && !Disposing)
                    {
                        ApplyRefreshResult(refreshResult);
                    }
                }
                while (refreshRequested && !IsDisposed && !Disposing);
            }
            catch (Exception)
            {
                if (!IsDisposed && !Disposing)
                {
                    ShowEmptyState();
                    statusLabel.Text = "●  " + text.UnexpectedError;
                }
            }
            finally
            {
                refreshInProgress = false;
                if (refreshButton != null && !refreshButton.IsDisposed)
                {
                    refreshButton.Enabled = true;
                }
            }
        }

        private async Task<MonitorRefreshResult> BuildRefreshResultAsync()
        {
            DisplayDiscoveryResult discovery = await Task.Run(
                delegate { return monitorManager.DiscoverDisplays(); });
            MonitorRefreshResult result = new MonitorRefreshResult();
            result.ActiveDisplayCount = discovery.Targets.Count;
            result.Warnings.AddRange(discovery.Warnings);

            List<List<DisplayTarget>> groups =
                MonitorManager.GroupExternalTargets(discovery.Targets);
            for (int index = 0; index < groups.Count; index++)
            {
                List<DisplayTarget> group = groups[index];
                result.ExternalDisplayCount += group.Count;
                List<MonitorDescriptor> descriptors =
                    await ProbeTargetWithTimeoutAsync(group);
                if (descriptors == null)
                {
                    result.Warnings.Add("ddc-probe-timeout-or-failure");
                    MonitorDescriptor fallback =
                        monitorManager.CreateSoftwareDescriptorGroup(group);
                    if (fallback != null)
                    {
                        result.Monitors.Add(fallback);
                    }
                    else
                    {
                        result.Warnings.Add(
                            "software-dimming-blocked-for-internal-clone");
                    }
                }
                else if (descriptors.Count == 0)
                {
                    result.Warnings.Add(
                        "software-dimming-blocked-for-internal-clone");
                }
                else
                {
                    result.Monitors.AddRange(descriptors);
                }
            }

            return result;
        }

        private async Task<List<MonitorDescriptor>> ProbeTargetWithTimeoutAsync(
            IList<DisplayTarget> targets)
        {
            bool entered = await hardwareGate.WaitAsync(
                DdcGateWaitMilliseconds);
            if (!entered)
            {
                return null;
            }

            Task<List<MonitorDescriptor>> probeTask = Task.Run(
                delegate { return monitorManager.ProbeTargetGroup(targets); });
            Task completed = await Task.WhenAny(
                probeTask,
                Task.Delay(DdcProbeTimeoutMilliseconds));
            if (completed != probeTask)
            {
                ReleaseGateWhenTaskCompletes(probeTask);
                return null;
            }

            try
            {
                return await probeTask;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                hardwareGate.Release();
            }
        }

        private void ApplyRefreshResult(MonitorRefreshResult result)
        {
            monitorList.SuspendLayout();
            try
            {
                ClearMonitorCards();
                dimmingService.Clear();

                if (result.Monitors.Count == 0)
                {
                    if (result.ExternalDisplayCount > 0)
                    {
                        ShowProtectedDuplicateState();
                        statusLabel.Text = "●  " + text.BuiltInDisplayUnchanged;
                    }
                    else
                    {
                        ShowEmptyState();
                        statusLabel.Text = "●  " + text.NoExternalDisplay;
                    }

                    return;
                }

                for (int index = 0; index < result.Monitors.Count; index++)
                {
                    MonitorDescriptor monitor = result.Monitors[index];
                    if (monitor.ControlKind == BrightnessControlKind.SoftwareDimming)
                    {
                        int storedLevel = settings.GetSoftwareLevel(monitor.Id);
                        monitor.CurrentPercent = storedLevel;
                        monitor.CurrentRaw = (uint)storedLevel;
                        dimmingService.SetLevel(monitor.Target, storedLevel);
                    }

                    MonitorCard card = new MonitorCard(monitor, text);
                    DpiLayout.ScaleControl(
                        card,
                        DpiLayout.LogicalDpi,
                        layoutDpi);
                    DpiLayout.ScaleFonts(
                        card,
                        fontReferenceDpi,
                        layoutDpi);
                    card.Width = CalculateCardWidth();
                    card.BrightnessRequested += CardBrightnessRequested;
                    monitorList.Controls.Add(card);
                }

                statusLabel.Text = "●  " + text.DisplaysReady(
                    result.ExternalDisplayCount);
            }
            finally
            {
                monitorList.ResumeLayout(true);
                RefreshMonitorSurface();
            }
        }

        private async void CardBrightnessRequested(
            object sender,
            BrightnessRequestEventArgs eventArgs)
        {
            MonitorCard card = sender as MonitorCard;
            if (card == null || card.IsDisposed)
            {
                return;
            }

            MonitorDescriptor monitor = card.Monitor;
            int requested = eventArgs.Percent;
            if (monitor.ControlKind == BrightnessControlKind.SoftwareDimming)
            {
                try
                {
                    dimmingService.SetLevel(monitor.Target, requested);
                    settings.SetSoftwareLevel(monitor.Id, requested);
                    monitor.CurrentPercent = requested;
                    monitor.CurrentRaw = (uint)requested;
                    card.SetSuccess(requested);
                }
                catch (Exception)
                {
                    card.SetFailure(text.UnexpectedError);
                }

                return;
            }

            card.SetBusy(true);
            BrightnessResult result =
                await SetHardwareBrightnessWithTimeoutAsync(monitor, requested);

            if (card.IsDisposed || IsDisposed || Disposing)
            {
                return;
            }

            if (result.Success)
            {
                monitor.CurrentPercent = result.Percent;
                card.SetSuccess(result.Percent);
                return;
            }

            if (result.Error == BrightnessError.DisplayDisconnected)
            {
                card.SetFailure(text.DisplayDisconnected);
                ScheduleTopologyRefresh();
            }
            else if (result.Error == BrightnessError.WriteFailed
                || result.Error == BrightnessError.ReadFailed)
            {
                card.SetFailure(text.WriteFailed);
            }
            else
            {
                card.SetFailure(text.UnexpectedError);
            }
        }

        private async Task<BrightnessResult> SetHardwareBrightnessWithTimeoutAsync(
            MonitorDescriptor monitor,
            int requested)
        {
            bool entered = await hardwareGate.WaitAsync(
                DdcGateWaitMilliseconds);
            if (!entered)
            {
                return BrightnessResult.Fail(
                    BrightnessError.ControlUnavailable,
                    0);
            }

            Task<BrightnessResult> operationTask = Task.Run(
                delegate
                {
                    return monitorManager.SetHardwareBrightness(
                        monitor,
                        requested);
                });
            Task completed = await Task.WhenAny(
                operationTask,
                Task.Delay(DdcWriteTimeoutMilliseconds));
            if (completed != operationTask)
            {
                ReleaseGateWhenTaskCompletes(operationTask);
                return BrightnessResult.Fail(BrightnessError.Unexpected, 0);
            }

            try
            {
                return await operationTask;
            }
            catch (Exception)
            {
                return BrightnessResult.Fail(BrightnessError.Unexpected, 0);
            }
            finally
            {
                hardwareGate.Release();
            }
        }

        private async void ReleaseGateWhenTaskCompletes(Task operation)
        {
            try
            {
                await operation;
            }
            catch (Exception)
            {
            }
            finally
            {
                hardwareGate.Release();
            }
        }

        private void ShowEmptyState()
        {
            ShowMessageState(
                text.NoExternalDisplay,
                text.NoExternalDetail);
        }

        private void ShowProtectedDuplicateState()
        {
            ShowMessageState(
                text.DuplicateModeProtected,
                text.DuplicateModeProtectedDetail);
        }

        private void ShowMessageState(string titleText, string detailText)
        {
            ClearMonitorCards();
            dimmingService.Clear();

            GlassPanel empty = new GlassPanel();
            empty.CornerRadius = 24;
            empty.StrongSurface = true;
            empty.AccentGlow = true;
            empty.Margin = new Padding(0, 0, 0, 14);
            empty.Padding = new Padding(22, 18, 22, 18);
            empty.Size = new Size(CalculateCardWidth(), 180);

            TableLayoutPanel emptyLayout = new TableLayoutPanel();
            emptyLayout.BackColor = Color.Transparent;
            emptyLayout.ColumnCount = 1;
            emptyLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            emptyLayout.Dock = DockStyle.Fill;
            emptyLayout.Margin = new Padding(0);
            emptyLayout.RowCount = 2;
            emptyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            emptyLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            Label title = new Label();
            title.AutoEllipsis = true;
            title.BackColor = Color.Transparent;
            title.Dock = DockStyle.Fill;
            title.Font = text.CreateUiFont(12.5F, FontStyle.Bold);
            title.ForeColor = GlassTheme.TextPrimary;
            title.Text = titleText;
            title.TextAlign = ContentAlignment.MiddleLeft;

            Label detail = new Label();
            detail.AutoEllipsis = true;
            detail.BackColor = Color.Transparent;
            detail.Dock = DockStyle.Fill;
            detail.Font = text.CreateUiFont(9F, FontStyle.Regular);
            detail.ForeColor = GlassTheme.TextSecondary;
            detail.Text = detailText;
            detail.TextAlign = ContentAlignment.TopLeft;

            emptyLayout.Controls.Add(title, 0, 0);
            emptyLayout.Controls.Add(detail, 0, 1);
            empty.Controls.Add(emptyLayout);
            DpiLayout.ScaleControl(
                empty,
                DpiLayout.LogicalDpi,
                layoutDpi);
            DpiLayout.ScaleFonts(
                empty,
                fontReferenceDpi,
                layoutDpi);
            empty.Width = CalculateCardWidth();
            monitorList.Controls.Add(empty);
        }

        private void ClearMonitorCards()
        {
            while (monitorList.Controls.Count > 0)
            {
                Control control = monitorList.Controls[0];
                monitorList.Controls.RemoveAt(0);
                control.Dispose();
            }
        }

        private int CalculateCardWidth()
        {
            int width = monitorList.ClientSize.Width
                - monitorList.Padding.Left
                - monitorList.Padding.Right
                - SystemInformation.VerticalScrollBarWidth
                - 4;
            return Math.Max(
                DpiLayout.ScaleLogical(430, layoutDpi),
                width);
        }

        private void MonitorListResize(object sender, EventArgs e)
        {
            int width = CalculateCardWidth();
            for (int index = 0; index < monitorList.Controls.Count; index++)
            {
                monitorList.Controls[index].Width = width;
            }
        }

        private void RefreshMonitorSurface()
        {
            if (monitorList.IsDisposed)
            {
                return;
            }

            monitorList.PerformLayout();
            monitorList.Invalidate(true);
            Invalidate(true);
        }

        private void StartupMenuItemClick(object sender, EventArgs e)
        {
            try
            {
                bool desired = !settings.IsStartWithWindowsEnabled();
                settings.SetStartWithWindows(desired);
                startupMenuItem.Checked = settings.IsStartWithWindowsEnabled();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    text.StartupFailed,
                    text.WindowTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SystemEventsDisplaySettingsChanged(object sender, EventArgs e)
        {
            ScheduleTopologyRefresh();
        }

        private void SystemEventsPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                ScheduleTopologyRefresh();
            }
        }

        private void ScheduleTopologyRefresh()
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    topologyTimer.Stop();
                    topologyTimer.Start();
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void InstanceShowEventSignaled(object state, bool timedOut)
        {
            if (timedOut || IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke((MethodInvoker)delegate { ShowWindow(); });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ShowWindow()
        {
            if (!Visible)
            {
                Show();
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
            BringToFront();
        }

        private void ExitApplication()
        {
            allowExit = true;
            dimmingService.Dispose();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            ApplyLayoutDpi(DpiLayout.GetWindowDpi(this));
            WindowBackdrop.Apply(Handle);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmDpiChanged)
            {
                int targetDpi = (int)(message.WParam.ToInt64() & 0xFFFF);
                ApplyLayoutDpi(targetDpi);
                if (message.LParam != IntPtr.Zero)
                {
                    NativeMethods.Rect suggested =
                        (NativeMethods.Rect)Marshal.PtrToStructure(
                            message.LParam,
                            typeof(NativeMethods.Rect));
                    SetBounds(
                        suggested.Left,
                        suggested.Top,
                        suggested.Right - suggested.Left,
                        suggested.Bottom - suggested.Top,
                        BoundsSpecified.All);
                }

                message.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref message);
        }

        private void ApplyLayoutDpi(int targetDpi)
        {
            if (targetDpi == layoutDpi)
            {
                return;
            }

            SuspendLayout();
            try
            {
                DpiLayout.ScaleControl(this, layoutDpi, targetDpi);
                DpiLayout.ScaleFonts(this, fontDpi, targetDpi);
                layoutDpi = targetDpi;
                fontDpi = targetDpi;
            }
            finally
            {
                ResumeLayout(true);
            }

            RefreshMonitorSurface();
        }

        protected override void OnFormClosing(FormClosingEventArgs eventArgs)
        {
            if (!allowExit
                && eventArgs.CloseReason == CloseReason.UserClosing)
            {
                eventArgs.Cancel = true;
                Hide();
                if (!trayNoticeShown)
                {
                    trayNoticeShown = true;
                    trayIcon.BalloonTipTitle = text.WindowTitle;
                    trayIcon.BalloonTipText = text.HiddenInTray;
                    trayIcon.ShowBalloonTip(2500);
                }

                return;
            }

            base.OnFormClosing(eventArgs);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.DisplaySettingsChanged -= SystemEventsDisplaySettingsChanged;
                SystemEvents.PowerModeChanged -= SystemEventsPowerModeChanged;

                if (showRegistration != null)
                {
                    showRegistration.Unregister(null);
                    showRegistration = null;
                }

                topologyTimer.Stop();
                topologyTimer.Dispose();
                dimmingService.Dispose();
                trayIcon.Visible = false;
                trayIcon.Dispose();
                if (Icon != null)
                {
                    Icon.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private static Icon ExtractApplicationIcon()
        {
            try
            {
                Icon extracted = Icon.ExtractAssociatedIcon(AppInfo.ExecutablePath);
                if (extracted != null)
                {
                    return (Icon)extracted.Clone();
                }
            }
            catch (Exception)
            {
            }

            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
