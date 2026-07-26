using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ExtLume
{
    public sealed class DdcBrightnessService
    {
        public List<MonitorDescriptor> Probe(DisplayTarget target)
        {
            List<DisplayTarget> targets = new List<DisplayTarget>();
            if (target != null)
            {
                targets.Add(target);
            }

            return ProbeGroup(targets);
        }

        public List<MonitorDescriptor> ProbeGroup(IList<DisplayTarget> targets)
        {
            List<MonitorDescriptor> descriptors = new List<MonitorDescriptor>();
            if (targets == null || targets.Count == 0)
            {
                return descriptors;
            }

            DisplayTarget firstTarget = targets[0];
            if (firstTarget == null || !firstTarget.IsExternal || firstTarget.IsVirtual)
            {
                return descriptors;
            }

            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                if (targets[targetIndex] == null
                    || !targets[targetIndex].IsExternal
                    || targets[targetIndex].IsVirtual
                    || !String.Equals(
                        targets[targetIndex].DeviceName,
                        firstTarget.DeviceName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return descriptors;
                }
            }

            using (PhysicalMonitorLease lease = PhysicalMonitorLease.Open(firstTarget.DeviceName))
            {
                if (lease != null)
                {
                    string[] physicalDescriptions =
                        new string[lease.Monitors.Length];
                    for (int index = 0; index < lease.Monitors.Length; index++)
                    {
                        physicalDescriptions[index] =
                            lease.Monitors[index].Description;
                    }

                    int[] targetMappings =
                        MapPhysicalTargets(physicalDescriptions, targets);
                    for (int index = 0; index < lease.Monitors.Length; index++)
                    {
                        NativeMethods.PhysicalMonitor physical = lease.Monitors[index];
                        int mappedTargetIndex = targetMappings[index];
                        if (mappedTargetIndex < 0)
                        {
                            continue;
                        }

                        DisplayTarget mappedTarget = targets[mappedTargetIndex];
                        IMonitorHardwareAdapter adapter =
                            new NativeMonitorHardwareAdapter(physical.Handle);
                        uint minimum;
                        uint current;
                        uint maximum;

                        if (adapter.TryReadHighLevel(
                            out minimum,
                            out current,
                            out maximum))
                        {
                            descriptors.Add(CreateHardwareDescriptor(
                                mappedTarget,
                                physical,
                                index,
                                HasDuplicateName(targets, mappedTarget.FriendlyName),
                                BrightnessControlKind.HardwareHighLevel,
                                minimum,
                                current,
                                maximum));
                            continue;
                        }

                        if (adapter.TryReadVcp(out current, out maximum))
                        {
                            descriptors.Add(CreateHardwareDescriptor(
                                mappedTarget,
                                physical,
                                index,
                                HasDuplicateName(targets, mappedTarget.FriendlyName),
                                BrightnessControlKind.HardwareVcp,
                                0,
                                current,
                                maximum));
                        }
                    }
                }
            }

            if (descriptors.Count == 0
                && SoftwareDimmingService.IsSafeGroup(targets))
            {
                descriptors.Add(CreateSoftwareDescriptor(targets));
            }

            return descriptors;
        }

        public BrightnessResult SetBrightness(MonitorDescriptor descriptor, int percent)
        {
            if (descriptor == null || descriptor.Target == null || !descriptor.UsesHardware)
            {
                return BrightnessResult.Fail(BrightnessError.ControlUnavailable, 0);
            }

            try
            {
                using (PhysicalMonitorLease lease = PhysicalMonitorLease.Open(descriptor.Target.DeviceName))
                {
                    if (lease == null
                        || descriptor.PhysicalIndex < 0
                        || descriptor.PhysicalIndex >= lease.Monitors.Length)
                    {
                        return BrightnessResult.Fail(
                            BrightnessError.DisplayDisconnected,
                            Marshal.GetLastWin32Error());
                    }

                    NativeMethods.PhysicalMonitor physical = lease.Monitors[descriptor.PhysicalIndex];
                    string actualDescription = MonitorIdentity.Clean(physical.Description);
                    string expectedDescription = MonitorIdentity.Clean(descriptor.PhysicalDescription);
                    if (!String.IsNullOrEmpty(expectedDescription)
                        && !String.Equals(
                            expectedDescription,
                            actualDescription,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return BrightnessResult.Fail(BrightnessError.DisplayDisconnected, 0);
                    }

                    if (descriptor.ControlKind == BrightnessControlKind.HardwareHighLevel)
                    {
                        return SetHighLevel(
                            new NativeMonitorHardwareAdapter(physical.Handle),
                            percent);
                    }

                    if (descriptor.ControlKind == BrightnessControlKind.HardwareVcp)
                    {
                        return SetVcp(
                            new NativeMonitorHardwareAdapter(physical.Handle),
                            percent);
                    }

                    return BrightnessResult.Fail(BrightnessError.ControlUnavailable, 0);
                }
            }
            catch (Exception)
            {
                return BrightnessResult.Fail(BrightnessError.Unexpected, 0);
            }
        }

        private static MonitorDescriptor CreateHardwareDescriptor(
            DisplayTarget target,
            NativeMethods.PhysicalMonitor physical,
            int physicalIndex,
            bool appendIndex,
            BrightnessControlKind kind,
            uint minimum,
            uint current,
            uint maximum)
        {
            string description = MonitorIdentity.Clean(physical.Description);
            string displayName = target.FriendlyName;
            if (MonitorIdentity.IsGenericName(displayName)
                && !MonitorIdentity.IsGenericName(description))
            {
                displayName = description;
            }

            if (appendIndex)
            {
                displayName = displayName + " (" + (physicalIndex + 1) + ")";
            }

            return new MonitorDescriptor
            {
                Id = MonitorIdentity.StableId(
                    target.Id + "|physical|" + physicalIndex + "|" + description),
                DisplayName = displayName,
                Target = target,
                ControlKind = kind,
                PhysicalIndex = physicalIndex,
                PhysicalDescription = description,
                MinimumRaw = minimum,
                MaximumRaw = maximum,
                CurrentRaw = current,
                CurrentPercent = BrightnessMath.RawToPercent(minimum, maximum, current)
            };
        }

        private static MonitorDescriptor CreateSoftwareDescriptor(
            IList<DisplayTarget> targets)
        {
            DisplayTarget first = targets[0];
            List<string> names = new List<string>();
            string identity = String.Empty;
            for (int index = 0; index < targets.Count; index++)
            {
                identity = identity + "|" + targets[index].Id;
                string name = targets[index].FriendlyName;
                bool exists = false;
                for (int nameIndex = 0; nameIndex < names.Count; nameIndex++)
                {
                    if (String.Equals(names[nameIndex], name, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    names.Add(name);
                }
            }

            string displayName = names.Count == 0
                ? first.FriendlyName
                : String.Join(" / ", names.ToArray());
            return new MonitorDescriptor
            {
                Id = MonitorIdentity.StableId(identity + "|software"),
                DisplayName = displayName,
                Target = first,
                ControlKind = BrightnessControlKind.SoftwareDimming,
                PhysicalIndex = -1,
                MinimumRaw = 0,
                MaximumRaw = 100,
                CurrentRaw = 100,
                CurrentPercent = 100
            };
        }

        internal static int[] MapPhysicalTargets(
            IList<string> physicalDescriptions,
            IList<DisplayTarget> targets)
        {
            int physicalCount = physicalDescriptions == null
                ? 0
                : physicalDescriptions.Count;
            int[] mappings = new int[physicalCount];
            for (int index = 0; index < mappings.Length; index++)
            {
                mappings[index] = -1;
            }

            if (targets == null || targets.Count == 0 || physicalCount == 0)
            {
                return mappings;
            }

            bool[] usedTargets = new bool[targets.Count];
            for (int physicalIndex = 0;
                physicalIndex < physicalCount;
                physicalIndex++)
            {
                string physicalKey =
                    NormalizeNameForMatch(physicalDescriptions[physicalIndex]);
                if (String.IsNullOrEmpty(physicalKey))
                {
                    continue;
                }

                int matchIndex = -1;
                int matchCount = 0;
                for (int targetIndex = 0;
                    targetIndex < targets.Count;
                    targetIndex++)
                {
                    if (usedTargets[targetIndex])
                    {
                        continue;
                    }

                    string friendlyKey =
                        NormalizeNameForMatch(targets[targetIndex].FriendlyName);
                    string modelKey = NormalizeNameForMatch(
                        MonitorIdentity.ExtractModelCode(
                            targets[targetIndex].DevicePath));
                    if (NamesMatch(physicalKey, friendlyKey)
                        || NamesMatch(physicalKey, modelKey))
                    {
                        matchIndex = targetIndex;
                        matchCount++;
                    }
                }

                if (matchCount == 1)
                {
                    mappings[physicalIndex] = matchIndex;
                    usedTargets[matchIndex] = true;
                }
            }

            for (int targetIndex = 0;
                targetIndex < targets.Count;
                targetIndex++)
            {
                if (targets[targetIndex].SharesSourceWithInternal)
                {
                    return mappings;
                }
            }

            if (physicalCount != targets.Count)
            {
                return mappings;
            }

            for (int physicalIndex = 0;
                physicalIndex < physicalCount;
                physicalIndex++)
            {
                if (mappings[physicalIndex] < 0
                    && !usedTargets[physicalIndex])
                {
                    mappings[physicalIndex] = physicalIndex;
                    usedTargets[physicalIndex] = true;
                }
            }

            Queue<int> remainingTargets = new Queue<int>();
            for (int targetIndex = 0;
                targetIndex < targets.Count;
                targetIndex++)
            {
                if (!usedTargets[targetIndex])
                {
                    remainingTargets.Enqueue(targetIndex);
                }
            }

            for (int physicalIndex = 0;
                physicalIndex < physicalCount;
                physicalIndex++)
            {
                if (mappings[physicalIndex] < 0
                    && remainingTargets.Count > 0)
                {
                    mappings[physicalIndex] = remainingTargets.Dequeue();
                }
            }

            return mappings;
        }

        private static string NormalizeNameForMatch(string name)
        {
            string value = MonitorIdentity.Clean(name).ToUpperInvariant();
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < value.Length; index++)
            {
                if (Char.IsLetterOrDigit(value[index]))
                {
                    builder.Append(value[index]);
                }
            }

            return builder.ToString();
        }

        private static bool NamesMatch(string first, string second)
        {
            if (String.IsNullOrEmpty(first) || String.IsNullOrEmpty(second))
            {
                return false;
            }

            if (first.Length < 3 || second.Length < 3)
            {
                return false;
            }

            return first.IndexOf(second, StringComparison.Ordinal) >= 0
                || second.IndexOf(first, StringComparison.Ordinal) >= 0;
        }

        private static bool HasDuplicateName(
            IList<DisplayTarget> targets,
            string friendlyName)
        {
            int count = 0;
            for (int index = 0; index < targets.Count; index++)
            {
                if (String.Equals(
                    targets[index].FriendlyName,
                    friendlyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count > 1;
        }

        internal static BrightnessResult SetHighLevel(
            IMonitorHardwareAdapter adapter,
            int percent)
        {
            uint minimum;
            uint current;
            uint maximum;
            if (!adapter.TryReadHighLevel(
                out minimum,
                out current,
                out maximum))
            {
                return BrightnessResult.Fail(
                    BrightnessError.ReadFailed,
                    adapter.LastError);
            }

            if (maximum <= minimum)
            {
                return BrightnessResult.Fail(BrightnessError.InvalidRange, 0);
            }

            uint requested = BrightnessMath.PercentToRaw(minimum, maximum, percent);
            if (!adapter.SetHighLevel(requested))
            {
                return BrightnessResult.Fail(
                    BrightnessError.WriteFailed,
                    adapter.LastError);
            }

            uint verifiedMinimum;
            uint verifiedCurrent;
            uint verifiedMaximum;
            if (!adapter.TryReadHighLevel(
                out verifiedMinimum,
                out verifiedCurrent,
                out verifiedMaximum))
            {
                return BrightnessResult.Fail(
                    BrightnessError.ReadFailed,
                    adapter.LastError);
            }

            return BrightnessResult.Ok(
                BrightnessMath.RawToPercent(
                    verifiedMinimum,
                    verifiedMaximum,
                    verifiedCurrent));
        }

        internal static BrightnessResult SetVcp(
            IMonitorHardwareAdapter adapter,
            int percent)
        {
            uint current;
            uint maximum;
            if (!adapter.TryReadVcp(out current, out maximum))
            {
                return BrightnessResult.Fail(
                    BrightnessError.ReadFailed,
                    adapter.LastError);
            }

            uint requested = BrightnessMath.PercentToRaw(0, maximum, percent);
            if (!adapter.SetVcp(requested))
            {
                return BrightnessResult.Fail(
                    BrightnessError.WriteFailed,
                    adapter.LastError);
            }

            uint verified;
            uint verifiedMaximum;
            if (!adapter.TryReadVcp(out verified, out verifiedMaximum))
            {
                return BrightnessResult.Fail(
                    BrightnessError.ReadFailed,
                    adapter.LastError);
            }

            return BrightnessResult.Ok(
                BrightnessMath.RawToPercent(0, verifiedMaximum, verified));
        }

        private sealed class PhysicalMonitorLease : IDisposable
        {
            private bool disposed;
            internal NativeMethods.PhysicalMonitor[] Monitors { get; private set; }

            private PhysicalMonitorLease(NativeMethods.PhysicalMonitor[] monitors)
            {
                Monitors = monitors;
            }

            internal static PhysicalMonitorLease Open(string deviceName)
            {
                IntPtr matchingMonitor = IntPtr.Zero;
                NativeMethods.MonitorEnumProc callback = delegate(
                    IntPtr monitor,
                    IntPtr deviceContext,
                    ref NativeMethods.Rect rectangle,
                    IntPtr data)
                {
                    NativeMethods.MonitorInfoEx info = new NativeMethods.MonitorInfoEx();
                    info.Size = (uint)Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx));
                    if (NativeMethods.GetMonitorInfo(monitor, ref info)
                        && String.Equals(
                            MonitorIdentity.Clean(info.DeviceName),
                            MonitorIdentity.Clean(deviceName),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        matchingMonitor = monitor;
                    }

                    return true;
                };

                NativeMethods.EnumDisplayMonitors(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    callback,
                    IntPtr.Zero);

                if (matchingMonitor == IntPtr.Zero)
                {
                    return null;
                }

                uint count;
                if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(
                    matchingMonitor,
                    out count)
                    || count == 0
                    || count > 32)
                {
                    return null;
                }

                NativeMethods.PhysicalMonitor[] monitors =
                    new NativeMethods.PhysicalMonitor[count];
                if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(
                    matchingMonitor,
                    count,
                    monitors))
                {
                    return null;
                }

                return new PhysicalMonitorLease(monitors);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                if (Monitors != null && Monitors.Length > 0)
                {
                    NativeMethods.DestroyPhysicalMonitors(
                        (uint)Monitors.Length,
                        Monitors);
                }
            }
        }
    }

    internal interface IMonitorHardwareAdapter
    {
        int LastError { get; }

        bool TryReadHighLevel(
            out uint minimum,
            out uint current,
            out uint maximum);

        bool SetHighLevel(uint value);

        bool TryReadVcp(out uint current, out uint maximum);

        bool SetVcp(uint value);
    }

    internal sealed class NativeMonitorHardwareAdapter : IMonitorHardwareAdapter
    {
        private readonly IntPtr handle;

        internal NativeMonitorHardwareAdapter(IntPtr physicalMonitorHandle)
        {
            handle = physicalMonitorHandle;
        }

        public int LastError { get; private set; }

        public bool TryReadHighLevel(
            out uint minimum,
            out uint current,
            out uint maximum)
        {
            minimum = 0;
            current = 0;
            maximum = 0;
            uint capabilities;
            uint colorTemperatures;
            if (!NativeMethods.GetMonitorCapabilities(
                handle,
                out capabilities,
                out colorTemperatures))
            {
                CaptureLastError();
                return false;
            }

            if ((capabilities & NativeMethods.McCapsBrightness) == 0)
            {
                LastError = 0;
                return false;
            }

            if (!NativeMethods.GetMonitorBrightness(
                handle,
                out minimum,
                out current,
                out maximum))
            {
                CaptureLastError();
                return false;
            }

            LastError = 0;
            return maximum > minimum;
        }

        public bool SetHighLevel(uint value)
        {
            bool success = NativeMethods.SetMonitorBrightness(handle, value);
            if (!success)
            {
                CaptureLastError();
            }
            else
            {
                LastError = 0;
            }

            return success;
        }

        public bool TryReadVcp(out uint current, out uint maximum)
        {
            current = 0;
            maximum = 0;
            NativeMethods.McVcpCodeType codeType;
            bool success = NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                handle,
                NativeMethods.BrightnessVcpCode,
                out codeType,
                out current,
                out maximum);
            if (!success)
            {
                CaptureLastError();
                return false;
            }

            LastError = 0;
            return maximum > 0;
        }

        public bool SetVcp(uint value)
        {
            bool success = NativeMethods.SetVCPFeature(
                handle,
                NativeMethods.BrightnessVcpCode,
                value);
            if (!success)
            {
                CaptureLastError();
            }
            else
            {
                LastError = 0;
            }

            return success;
        }

        private void CaptureLastError()
        {
            LastError = Marshal.GetLastWin32Error();
        }
    }
}
