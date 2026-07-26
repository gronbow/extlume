using System;
using System.Collections.Generic;
using System.Drawing;
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
        private const int DdcProbeTimeoutMilliseconds = 4000;
        private const int DdcWriteTimeoutMilliseconds = 5000;
        private const int DdcGateWaitMilliseconds = 350;

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

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(246, 247, 249);
            ClientSize = new Size(570, 480);
            Font = text.CreateUiFont(9F, FontStyle.Regular);
            Icon = ExtractApplicationIcon();
            MaximizeBox = false;
            MinimumSize = new Size(520, 340);
            StartPosition = FormStartPosition.CenterScreen;
            Text = text.WindowTitle;

            TableLayoutPanel root = new TableLayoutPanel();
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            Panel header = BuildHeader();
            root.Controls.Add(header, 0, 0);

            monitorList = new FlowLayoutPanel();
            monitorList.AutoScroll = true;
            monitorList.BackColor = BackColor;
            monitorList.Dock = DockStyle.Fill;
            monitorList.FlowDirection = FlowDirection.TopDown;
            monitorList.Padding = new Padding(16, 10, 16, 10);
            monitorList.WrapContents = false;
            monitorList.Resize += MonitorListResize;
            root.Controls.Add(monitorList, 0, 1);

            statusLabel = new Label();
            statusLabel.AutoEllipsis = true;
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = text.CreateUiFont(8.5F, FontStyle.Regular);
            statusLabel.ForeColor = Color.FromArgb(92, 92, 92);
            statusLabel.Padding = new Padding(18, 0, 18, 0);
            statusLabel.Text = text.Refreshing;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(statusLabel, 0, 2);
            Controls.Add(root);

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
            Panel header = new Panel();
            header.BackColor = Color.White;
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(18, 12, 18, 10);

            Label heading = new Label();
            heading.AutoEllipsis = true;
            heading.Font = text.CreateUiFont(15F, FontStyle.Bold);
            heading.Location = new Point(18, 7);
            heading.Size = new Size(360, 48);
            heading.Text = text.Heading;
            heading.TextAlign = ContentAlignment.MiddleLeft;

            Label intro = new Label();
            intro.Font = text.CreateUiFont(8.8F, FontStyle.Regular);
            intro.ForeColor = Color.FromArgb(92, 92, 92);
            intro.Location = new Point(20, 70);
            intro.Size = new Size(530, 44);
            intro.Text = text.Intro;
            intro.TextAlign = ContentAlignment.MiddleLeft;

            Button button = new Button();
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button.AutoSize = false;
            button.BackColor = Color.White;
            button.FlatStyle = FlatStyle.System;
            button.Location = new Point(446, 31);
            button.Name = "RefreshButton";
            button.Size = new Size(104, 34);
            button.Text = text.Refresh;
            button.UseVisualStyleBackColor = true;
            button.Click += delegate { RequestRefresh(); };

            header.Controls.Add(heading);
            header.Controls.Add(intro);
            header.Controls.Add(button);
            header.Resize += delegate
            {
                button.Left = header.ClientSize.Width - button.Width - 18;
                intro.Width = Math.Max(180, header.ClientSize.Width - intro.Left - 18);
                heading.Width = Math.Max(180, button.Left - heading.Left - 12);
            };
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

                    statusLabel.Text = text.Refreshing;
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
                    statusLabel.Text = text.UnexpectedError;
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
            ClearMonitorCards();
            dimmingService.Clear();

            if (result.Monitors.Count == 0)
            {
                if (result.ExternalDisplayCount > 0)
                {
                    ShowProtectedDuplicateState();
                    statusLabel.Text = text.BuiltInDisplayUnchanged;
                }
                else
                {
                    ShowEmptyState();
                    statusLabel.Text = text.NoExternalDisplay;
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
                card.Width = CalculateCardWidth();
                card.BrightnessRequested += CardBrightnessRequested;
                monitorList.Controls.Add(card);
            }

            statusLabel.Text = text.DisplaysReady(result.ExternalDisplayCount);
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

            TableLayoutPanel empty = new TableLayoutPanel();
            empty.BackColor = Color.White;
            empty.BorderStyle = BorderStyle.FixedSingle;
            empty.ColumnCount = 1;
            empty.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            empty.Margin = new Padding(0);
            empty.Padding = new Padding(22, 18, 22, 18);
            empty.RowCount = 2;
            empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            empty.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            empty.Size = new Size(CalculateCardWidth(), 180);

            Label title = new Label();
            title.AutoEllipsis = true;
            title.Dock = DockStyle.Fill;
            title.Font = text.CreateUiFont(11.5F, FontStyle.Bold);
            title.Text = titleText;
            title.TextAlign = ContentAlignment.MiddleLeft;

            Label detail = new Label();
            detail.Dock = DockStyle.Fill;
            detail.Font = text.CreateUiFont(9F, FontStyle.Regular);
            detail.ForeColor = Color.FromArgb(92, 92, 92);
            detail.Text = detailText;
            detail.TextAlign = ContentAlignment.TopLeft;

            empty.Controls.Add(title, 0, 0);
            empty.Controls.Add(detail, 0, 1);
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
            return Math.Max(420, width);
        }

        private void MonitorListResize(object sender, EventArgs e)
        {
            int width = CalculateCardWidth();
            for (int index = 0; index < monitorList.Controls.Count; index++)
            {
                monitorList.Controls[index].Width = width;
            }
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
