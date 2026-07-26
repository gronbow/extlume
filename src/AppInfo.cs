using System;
using System.Diagnostics;
using System.IO;

namespace ExtLume
{
    public static class AppInfo
    {
        public const string ProductName = "ExtLume";
        public const string ShortName = "ExtLume";
        public const string Version = "0.2.0-beta.1";
        public const string MutexName = "Local\\ExtLume-5E7A3914-9368-4937-AD5C-50BFFCB4FD38";
        public const string ShowEventName = "Local\\ExtLume-Show-0DD3D353-A87C-4E7E-B050-60A3AA7879F3";
        public const string RegistryRoot = @"Software\ExtLume";
        public const string StartupValueName = "ExtLume";

        public static string ExecutablePath
        {
            get
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    if (process.MainModule != null && !String.IsNullOrEmpty(process.MainModule.FileName))
                    {
                        return process.MainModule.FileName;
                    }
                }

                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExtLume.exe");
            }
        }
    }
}
