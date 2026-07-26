using System;
using System.Collections.Generic;
using System.Drawing;

namespace ExtLume
{
    public enum BrightnessControlKind
    {
        HardwareHighLevel,
        HardwareVcp,
        SoftwareDimming
    }

    public enum BrightnessError
    {
        None,
        DisplayDisconnected,
        ControlUnavailable,
        InvalidRange,
        ReadFailed,
        WriteFailed,
        Unexpected
    }

    public sealed class DisplayTarget
    {
        public string Id { get; set; }
        public string DeviceName { get; set; }
        public string FriendlyName { get; set; }
        public string DevicePath { get; set; }
        public uint OutputTechnology { get; set; }
        public bool IsInternal { get; set; }
        public bool IsExternal { get; set; }
        public bool IsVirtual { get; set; }
        public bool SharesSourceWithInternal { get; set; }
        public Rectangle Bounds { get; set; }

        public DisplayTarget()
        {
            Id = String.Empty;
            DeviceName = String.Empty;
            FriendlyName = String.Empty;
            DevicePath = String.Empty;
            Bounds = Rectangle.Empty;
        }
    }

    public sealed class MonitorDescriptor
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public DisplayTarget Target { get; set; }
        public BrightnessControlKind ControlKind { get; set; }
        public int PhysicalIndex { get; set; }
        public string PhysicalDescription { get; set; }
        public uint MinimumRaw { get; set; }
        public uint MaximumRaw { get; set; }
        public uint CurrentRaw { get; set; }
        public int CurrentPercent { get; set; }

        public bool UsesHardware
        {
            get { return ControlKind != BrightnessControlKind.SoftwareDimming; }
        }

        public MonitorDescriptor()
        {
            Id = String.Empty;
            DisplayName = String.Empty;
            PhysicalDescription = String.Empty;
            PhysicalIndex = -1;
        }
    }

    public sealed class BrightnessResult
    {
        public bool Success { get; private set; }
        public int Percent { get; private set; }
        public BrightnessError Error { get; private set; }
        public int NativeError { get; private set; }

        private BrightnessResult()
        {
        }

        public static BrightnessResult Ok(int percent)
        {
            return new BrightnessResult
            {
                Success = true,
                Percent = BrightnessMath.ClampPercent(percent),
                Error = BrightnessError.None
            };
        }

        public static BrightnessResult Fail(BrightnessError error, int nativeError)
        {
            return new BrightnessResult
            {
                Success = false,
                Percent = 0,
                Error = error,
                NativeError = nativeError
            };
        }
    }

    public sealed class DisplayDiscoveryResult
    {
        public List<DisplayTarget> Targets { get; private set; }
        public List<string> Warnings { get; private set; }

        public DisplayDiscoveryResult()
        {
            Targets = new List<DisplayTarget>();
            Warnings = new List<string>();
        }
    }

    public sealed class MonitorRefreshResult
    {
        public List<MonitorDescriptor> Monitors { get; private set; }
        public List<string> Warnings { get; private set; }
        public int ActiveDisplayCount { get; set; }
        public int ExternalDisplayCount { get; set; }

        public MonitorRefreshResult()
        {
            Monitors = new List<MonitorDescriptor>();
            Warnings = new List<string>();
        }
    }
}
