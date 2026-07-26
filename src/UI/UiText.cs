using System;
using System.Drawing;
using System.Globalization;

namespace ExtLume
{
    public sealed class UiText
    {
        private readonly bool chinese;

        public UiText()
            : this(null)
        {
        }

        public UiText(string languageOverride)
        {
            string language = String.IsNullOrEmpty(languageOverride)
                ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                : languageOverride;
            chinese = language.StartsWith(
                "zh",
                StringComparison.OrdinalIgnoreCase);
        }

        public bool IsChinese
        {
            get { return chinese; }
        }

        public string WindowTitle
        {
            get { return "ExtLume"; }
        }

        public string Heading
        {
            get { return chinese ? "外接显示器亮度" : "External brightness"; }
        }

        public string Eyebrow
        {
            get { return chinese ? "显示控制" : "DISPLAY CONTROL"; }
        }

        public string Intro
        {
            get
            {
                return chinese
                    ? "自动识别外接屏；硬件调光或软件调暗。"
                    : "Hardware control or software dimming.";
            }
        }

        public string Refresh
        {
            get { return chinese ? "重新扫描" : "Rescan"; }
        }

        public string RefreshAccessibleDescription
        {
            get
            {
                return chinese
                    ? "重新检测已连接的外接显示器。"
                    : "Detect connected external displays again.";
            }
        }

        public string Refreshing
        {
            get { return chinese ? "正在扫描显示器…" : "Scanning displays…"; }
        }

        public string NoExternalDisplay
        {
            get { return chinese ? "未检测到可调节的外接显示器" : "No external display detected"; }
        }

        public string NoExternalDetail
        {
            get
            {
                return chinese
                    ? "开启显示器，并选择“扩展”或“复制”。\r\n内置屏不会被修改。"
                    : "Use Extend or Duplicate, then rescan.\r\nThe built-in display is never changed.";
            }
        }

        public string DuplicateModeProtected
        {
            get { return chinese ? "复制模式已保护" : "Duplicate mode is protected"; }
        }

        public string DuplicateModeProtectedDetail
        {
            get
            {
                return chinese
                    ? "硬件 DDC/CI 不可用。\r\n切换到“扩展”后可安全使用软件调暗。"
                    : "Hardware DDC/CI is unavailable.\r\nUse Extend for safe software dimming.";
            }
        }

        public string BuiltInDisplayUnchanged
        {
            get { return chinese ? "内置屏未被修改" : "Built-in display unchanged"; }
        }

        public string HardwareDdc
        {
            get { return chinese ? "硬件 · DDC/CI" : "Hardware · DDC/CI"; }
        }

        public string SoftwareDimming
        {
            get { return chinese ? "软件调暗" : "Software dimming"; }
        }

        public string SoftwareDimmingNote
        {
            get
            {
                return chinese
                    ? "只调暗画面，背光和功耗不变。"
                    : "Image only; backlight power is unchanged.";
            }
        }

        public string HardwareNote
        {
            get
            {
                return chinese
                    ? "调节显示器真实背光。"
                    : "Adjusts the physical backlight.";
            }
        }

        public string Applying
        {
            get { return chinese ? "正在应用…" : "Applying…"; }
        }

        public string Applied
        {
            get { return chinese ? "已应用" : "Applied"; }
        }

        public string Ready
        {
            get { return chinese ? "就绪" : "Ready"; }
        }

        public string Open
        {
            get { return chinese ? "打开" : "Open"; }
        }

        public string Exit
        {
            get { return chinese ? "退出" : "Exit"; }
        }

        public string StartWithWindows
        {
            get { return chinese ? "开机启动" : "Start with Windows"; }
        }

        public string StartupFailed
        {
            get { return chinese ? "无法更新开机启动设置。" : "Could not update the startup setting."; }
        }

        public string HiddenInTray
        {
            get { return chinese ? "应用仍在系统托盘中运行。" : "The app is still running in the system tray."; }
        }

        public string DisplayDisconnected
        {
            get { return chinese ? "显示器连接已变化，正在重新扫描。" : "Display connection changed; rescanning."; }
        }

        public string WriteFailed
        {
            get
            {
                return chinese
                    ? "调节失败。请检查显示器 DDC/CI 设置、线缆或扩展坞。"
                    : "Adjustment failed. Check DDC/CI, the cable, or the dock.";
            }
        }

        public string UnexpectedError
        {
            get { return chinese ? "操作失败，请重新扫描后再试。" : "Operation failed. Rescan and try again."; }
        }

        public string BrightnessSliderDescription
        {
            get
            {
                return chinese
                    ? "使用左右方向键微调，Page Up 和 Page Down 每次调节 10%。"
                    : "Use arrow keys for fine adjustment, or Page Up and Page Down for 10 percent steps.";
            }
        }

        public string BrightnessSliderName(string displayName)
        {
            return chinese
                ? displayName + " 亮度"
                : displayName + " brightness";
        }

        public string DisplaysReady(int count)
        {
            return chinese
                ? "已识别 " + count + " 台外接显示器"
                : count + (count == 1 ? " external display ready" : " external displays ready");
        }

        public Font CreateUiFont(float size, FontStyle style)
        {
            string preferred = chinese ? "Microsoft YaHei UI" : "Segoe UI";
            try
            {
                return new Font(preferred, size, style, GraphicsUnit.Point);
            }
            catch (Exception)
            {
                return new Font(SystemFonts.MessageBoxFont.FontFamily, size, style, GraphicsUnit.Point);
            }
        }
    }
}
