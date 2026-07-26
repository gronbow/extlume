using System;
using Microsoft.Win32;

namespace ExtLume
{
    public sealed class AppSettings
    {
        private const string SoftwareLevelsSubKey = "SoftwareLevels";

        public int GetSoftwareLevel(string monitorId)
        {
            if (String.IsNullOrEmpty(monitorId))
            {
                return 100;
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    AppInfo.RegistryRoot + "\\" + SoftwareLevelsSubKey,
                    false))
                {
                    if (key == null)
                    {
                        return 100;
                    }

                    object value = key.GetValue(monitorId);
                    if (value == null)
                    {
                        return 100;
                    }

                    return BrightnessMath.ClampPercent(Convert.ToInt32(value));
                }
            }
            catch (Exception)
            {
                return 100;
            }
        }

        public void SetSoftwareLevel(string monitorId, int percent)
        {
            if (String.IsNullOrEmpty(monitorId))
            {
                return;
            }

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                AppInfo.RegistryRoot + "\\" + SoftwareLevelsSubKey))
            {
                if (key != null)
                {
                    key.SetValue(
                        monitorId,
                        BrightnessMath.ClampPercent(percent),
                        RegistryValueKind.DWord);
                }
            }
        }

        public bool IsStartWithWindowsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    false))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    string actual = Convert.ToString(key.GetValue(AppInfo.StartupValueName));
                    return String.Equals(
                        NormalizeStartupCommand(actual),
                        NormalizeStartupCommand(BuildStartupCommand()),
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void SetStartWithWindows(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("The startup registry key is unavailable.");
                }

                if (enabled)
                {
                    key.SetValue(
                        AppInfo.StartupValueName,
                        BuildStartupCommand(),
                        RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(AppInfo.StartupValueName, false);
                }
            }
        }

        private static string BuildStartupCommand()
        {
            return "\"" + AppInfo.ExecutablePath + "\" --startup";
        }

        private static string NormalizeStartupCommand(string command)
        {
            return (command ?? String.Empty).Trim();
        }
    }
}
