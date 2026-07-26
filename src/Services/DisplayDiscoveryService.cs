using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ExtLume
{
    public sealed class DisplayDiscoveryService
    {
        private const int MaximumQueryAttempts = 4;

        public DisplayDiscoveryResult DiscoverActiveTargets()
        {
            DisplayDiscoveryResult result = new DisplayDiscoveryResult();
            List<WmiMonitorRecord> wmiRecords = ReadWmiMonitorRecords(result.Warnings);

            try
            {
                result.Targets.AddRange(QueryDisplayConfiguration());
            }
            catch (Exception)
            {
                result.Warnings.Add("display-config-query-failed");
            }

            if (result.Targets.Count == 0)
            {
                result.Targets.AddRange(QueryScreenFallback(wmiRecords));
                if (result.Targets.Count > 0)
                {
                    result.Warnings.Add("display-config-fallback-used");
                }
            }

            EnrichFromWmi(result.Targets, wmiRecords);
            MonitorClassifier.ClassifyTargets(result.Targets);
            return result;
        }

        private static List<DisplayTarget> QueryDisplayConfiguration()
        {
            int pathElementSize = Marshal.SizeOf(typeof(NativeMethods.DisplayConfigPathInfo));
            List<DisplayTarget> targets = new List<DisplayTarget>();

            for (int attempt = 0; attempt < MaximumQueryAttempts; attempt++)
            {
                uint pathCount;
                uint modeCount;
                int sizeResult = NativeMethods.GetDisplayConfigBufferSizes(
                    NativeMethods.QdcOnlyActivePaths,
                    out pathCount,
                    out modeCount);

                if (sizeResult != NativeMethods.ErrorSuccess)
                {
                    throw new InvalidOperationException("GetDisplayConfigBufferSizes failed.");
                }

                if (pathCount == 0)
                {
                    return targets;
                }

                IntPtr pathBuffer = IntPtr.Zero;
                IntPtr modeBuffer = IntPtr.Zero;
                try
                {
                    pathBuffer = Marshal.AllocHGlobal(checked((int)pathCount * pathElementSize));
                    int modeBytes = checked((int)Math.Max(1U, modeCount) * InteropLayout.DisplayModeBufferElementSize);
                    modeBuffer = Marshal.AllocHGlobal(modeBytes);

                    uint returnedPathCount = pathCount;
                    uint returnedModeCount = modeCount;
                    int queryResult = NativeMethods.QueryDisplayConfig(
                        NativeMethods.QdcOnlyActivePaths,
                        ref returnedPathCount,
                        pathBuffer,
                        ref returnedModeCount,
                        modeBuffer,
                        IntPtr.Zero);

                    if (queryResult == NativeMethods.ErrorInsufficientBuffer)
                    {
                        continue;
                    }

                    if (queryResult != NativeMethods.ErrorSuccess)
                    {
                        throw new InvalidOperationException("QueryDisplayConfig failed.");
                    }

                    for (uint index = 0; index < returnedPathCount; index++)
                    {
                        IntPtr itemPointer = IntPtr.Add(pathBuffer, checked((int)index * pathElementSize));
                        NativeMethods.DisplayConfigPathInfo path =
                            (NativeMethods.DisplayConfigPathInfo)Marshal.PtrToStructure(
                                itemPointer,
                                typeof(NativeMethods.DisplayConfigPathInfo));

                        DisplayTarget target = CreateTarget(path);
                        if (target != null && !ContainsTarget(targets, target))
                        {
                            targets.Add(target);
                        }
                    }

                    return targets;
                }
                finally
                {
                    if (pathBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pathBuffer);
                    }

                    if (modeBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(modeBuffer);
                    }
                }
            }

            throw new InvalidOperationException("Display configuration changed repeatedly.");
        }

        private static DisplayTarget CreateTarget(NativeMethods.DisplayConfigPathInfo path)
        {
            NativeMethods.DisplayConfigSourceDeviceName sourceName =
                new NativeMethods.DisplayConfigSourceDeviceName();
            sourceName.Header = new NativeMethods.DisplayConfigDeviceInfoHeader
            {
                Type = NativeMethods.DisplayConfigGetSourceName,
                Size = (uint)Marshal.SizeOf(typeof(NativeMethods.DisplayConfigSourceDeviceName)),
                AdapterId = path.SourceInfo.AdapterId,
                Id = path.SourceInfo.Id
            };

            NativeMethods.DisplayConfigTargetDeviceName targetName =
                new NativeMethods.DisplayConfigTargetDeviceName();
            targetName.Header = new NativeMethods.DisplayConfigDeviceInfoHeader
            {
                Type = NativeMethods.DisplayConfigGetTargetName,
                Size = (uint)Marshal.SizeOf(typeof(NativeMethods.DisplayConfigTargetDeviceName)),
                AdapterId = path.TargetInfo.AdapterId,
                Id = path.TargetInfo.Id
            };

            int sourceResult = NativeMethods.DisplayConfigGetDeviceInfo(ref sourceName);
            int targetResult = NativeMethods.DisplayConfigGetDeviceInfo(ref targetName);
            string deviceName = sourceResult == NativeMethods.ErrorSuccess
                ? MonitorIdentity.Clean(sourceName.ViewGdiDeviceName)
                : String.Empty;
            string devicePath = targetResult == NativeMethods.ErrorSuccess
                ? MonitorIdentity.Clean(targetName.MonitorDevicePath)
                : String.Empty;
            string rawFriendlyName = targetResult == NativeMethods.ErrorSuccess
                ? MonitorIdentity.Clean(targetName.MonitorFriendlyDeviceName)
                : String.Empty;
            uint technology = targetResult == NativeMethods.ErrorSuccess
                ? targetName.OutputTechnology
                : path.TargetInfo.OutputTechnology;

            if (String.IsNullOrEmpty(deviceName))
            {
                return null;
            }

            string identitySource = devicePath
                + "|"
                + path.TargetInfo.AdapterId.HighPart
                + ":"
                + path.TargetInfo.AdapterId.LowPart
                + "|"
                + path.TargetInfo.Id;

            return new DisplayTarget
            {
                Id = MonitorIdentity.StableId(identitySource),
                DeviceName = deviceName,
                DevicePath = devicePath,
                FriendlyName = MonitorIdentity.NormalizeFriendlyName(rawFriendlyName, devicePath),
                OutputTechnology = technology,
                Bounds = FindScreenBounds(deviceName)
            };
        }

        private static bool ContainsTarget(List<DisplayTarget> targets, DisplayTarget candidate)
        {
            for (int index = 0; index < targets.Count; index++)
            {
                if (String.Equals(targets[index].Id, candidate.Id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Rectangle FindScreenBounds(string deviceName)
        {
            Screen[] screens = Screen.AllScreens;
            for (int index = 0; index < screens.Length; index++)
            {
                if (String.Equals(screens[index].DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return screens[index].Bounds;
                }
            }

            return Rectangle.Empty;
        }

        private static List<DisplayTarget> QueryScreenFallback(List<WmiMonitorRecord> wmiRecords)
        {
            List<DisplayTarget> targets = new List<DisplayTarget>();
            Screen[] screens = Screen.AllScreens;

            for (int index = 0; index < screens.Length; index++)
            {
                Screen screen = screens[index];
                NativeMethods.DisplayDevice device = new NativeMethods.DisplayDevice();
                device.Size = Marshal.SizeOf(typeof(NativeMethods.DisplayDevice));
                bool found = NativeMethods.EnumDisplayDevices(
                    screen.DeviceName,
                    0,
                    ref device,
                    0);

                string devicePath = found ? MonitorIdentity.Clean(device.DeviceId) : String.Empty;
                string friendly = found ? MonitorIdentity.Clean(device.DeviceString) : String.Empty;
                uint technology = MonitorClassifier.OutputOther;
                WmiMonitorRecord record = FindWmiRecord(devicePath, wmiRecords);
                if (record != null)
                {
                    technology = record.OutputTechnology;
                    if (MonitorIdentity.IsGenericName(friendly) && !String.IsNullOrEmpty(record.FriendlyName))
                    {
                        friendly = record.FriendlyName;
                    }
                }

                targets.Add(new DisplayTarget
                {
                    Id = MonitorIdentity.StableId(devicePath + "|" + screen.DeviceName),
                    DeviceName = screen.DeviceName,
                    DevicePath = devicePath,
                    FriendlyName = MonitorIdentity.NormalizeFriendlyName(friendly, devicePath),
                    OutputTechnology = technology,
                    Bounds = screen.Bounds
                });
            }

            return targets;
        }

        private static void EnrichFromWmi(
            List<DisplayTarget> targets,
            List<WmiMonitorRecord> records)
        {
            for (int index = 0; index < targets.Count; index++)
            {
                DisplayTarget target = targets[index];
                WmiMonitorRecord record = FindWmiRecord(target.DevicePath, records);
                if (record == null)
                {
                    continue;
                }

                if (target.OutputTechnology == MonitorClassifier.OutputOther)
                {
                    target.OutputTechnology = record.OutputTechnology;
                }

                if (MonitorIdentity.IsGenericName(target.FriendlyName)
                    && !String.IsNullOrEmpty(record.FriendlyName))
                {
                    target.FriendlyName = record.FriendlyName;
                }
            }
        }

        private static WmiMonitorRecord FindWmiRecord(
            string devicePath,
            List<WmiMonitorRecord> records)
        {
            string model = MonitorIdentity.ExtractModelCode(devicePath);
            if (String.IsNullOrEmpty(model))
            {
                return null;
            }

            for (int index = 0; index < records.Count; index++)
            {
                if (String.Equals(records[index].ModelCode, model, StringComparison.OrdinalIgnoreCase))
                {
                    return records[index];
                }
            }

            return null;
        }

        private static List<WmiMonitorRecord> ReadWmiMonitorRecords(List<string> warnings)
        {
            List<WmiMonitorRecord> records = new List<WmiMonitorRecord>();
            Dictionary<string, WmiMonitorRecord> byInstance =
                new Dictionary<string, WmiMonitorRecord>(StringComparer.OrdinalIgnoreCase);

            try
            {
                ManagementScope scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();

                using (ManagementObjectSearcher connectionSearcher =
                    new ManagementObjectSearcher(
                        scope,
                        new ObjectQuery("SELECT InstanceName, Active, VideoOutputTechnology FROM WmiMonitorConnectionParams")))
                using (ManagementObjectCollection connections = connectionSearcher.Get())
                {
                    foreach (ManagementObject connection in connections)
                    {
                        string instance = Convert.ToString(connection["InstanceName"]);
                        object activeValue = connection["Active"];
                        bool active = activeValue == null || Convert.ToBoolean(activeValue);
                        if (!active || String.IsNullOrEmpty(instance))
                        {
                            continue;
                        }

                        WmiMonitorRecord record = new WmiMonitorRecord();
                        record.InstanceName = instance;
                        record.ModelCode = MonitorIdentity.ExtractModelCode(instance);
                        record.OutputTechnology = Convert.ToUInt32(connection["VideoOutputTechnology"]);
                        byInstance[NormalizeWmiInstance(instance)] = record;
                        records.Add(record);
                    }
                }

                using (ManagementObjectSearcher nameSearcher =
                    new ManagementObjectSearcher(
                        scope,
                        new ObjectQuery("SELECT InstanceName, Active, UserFriendlyName FROM WmiMonitorID")))
                using (ManagementObjectCollection names = nameSearcher.Get())
                {
                    foreach (ManagementObject nameObject in names)
                    {
                        string instance = Convert.ToString(nameObject["InstanceName"]);
                        WmiMonitorRecord record;
                        if (!byInstance.TryGetValue(NormalizeWmiInstance(instance), out record))
                        {
                            continue;
                        }

                        record.FriendlyName = DecodeWmiCharacterArray(nameObject["UserFriendlyName"]);
                    }
                }
            }
            catch (Exception)
            {
                warnings.Add("wmi-monitor-metadata-unavailable");
            }

            return records;
        }

        private static string NormalizeWmiInstance(string instance)
        {
            string value = MonitorIdentity.Clean(instance);
            int suffixIndex = value.LastIndexOf('_');
            if (suffixIndex > 0)
            {
                value = value.Substring(0, suffixIndex);
            }

            return value;
        }

        private static string DecodeWmiCharacterArray(object value)
        {
            Array characters = value as Array;
            if (characters == null)
            {
                return String.Empty;
            }

            List<char> output = new List<char>();
            foreach (object item in characters)
            {
                ushort code = Convert.ToUInt16(item);
                if (code == 0)
                {
                    break;
                }

                output.Add((char)code);
            }

            return new string(output.ToArray()).Trim();
        }

        private sealed class WmiMonitorRecord
        {
            internal string InstanceName;
            internal string ModelCode;
            internal string FriendlyName;
            internal uint OutputTechnology;
        }
    }
}
