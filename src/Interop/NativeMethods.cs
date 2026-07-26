using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ExtLume
{
    internal static class NativeMethods
    {
        internal const uint QdcOnlyActivePaths = 0x00000002;
        internal const int ErrorSuccess = 0;
        internal const int ErrorInsufficientBuffer = 122;
        internal const uint DisplayConfigGetSourceName = 1;
        internal const uint DisplayConfigGetTargetName = 2;
        internal const uint McCapsBrightness = 0x00000002;
        internal const byte BrightnessVcpCode = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        internal struct Luid
        {
            internal uint LowPart;
            internal int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DisplayConfigPathSourceInfo
        {
            internal Luid AdapterId;
            internal uint Id;
            internal uint ModeInfoIndex;
            internal uint StatusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DisplayConfigRational
        {
            internal uint Numerator;
            internal uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DisplayConfigPathTargetInfo
        {
            internal Luid AdapterId;
            internal uint Id;
            internal uint ModeInfoIndex;
            internal uint OutputTechnology;
            internal uint Rotation;
            internal uint Scaling;
            internal DisplayConfigRational RefreshRate;
            internal uint ScanLineOrdering;
            [MarshalAs(UnmanagedType.Bool)]
            internal bool TargetAvailable;
            internal uint StatusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DisplayConfigPathInfo
        {
            internal DisplayConfigPathSourceInfo SourceInfo;
            internal DisplayConfigPathTargetInfo TargetInfo;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DisplayConfigDeviceInfoHeader
        {
            internal uint Type;
            internal uint Size;
            internal Luid AdapterId;
            internal uint Id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DisplayConfigSourceDeviceName
        {
            internal DisplayConfigDeviceInfoHeader Header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string ViewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DisplayConfigTargetDeviceName
        {
            internal DisplayConfigDeviceInfoHeader Header;
            internal uint Flags;
            internal uint OutputTechnology;
            internal ushort EdidManufactureId;
            internal ushort EdidProductCodeId;
            internal uint ConnectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            internal string MonitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string MonitorDevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MonitorInfoEx
        {
            internal uint Size;
            internal Rect Monitor;
            internal Rect Work;
            internal uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string DeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DisplayDevice
        {
            internal int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceString;
            internal uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct PhysicalMonitor
        {
            internal IntPtr Handle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string Description;
        }

        internal delegate bool MonitorEnumProc(
            IntPtr monitor,
            IntPtr deviceContext,
            ref Rect monitorRectangle,
            IntPtr data);

        internal enum McVcpCodeType
        {
            Momentary = 0,
            SetParameter = 1
        }

        [DllImport("user32.dll")]
        internal static extern int GetDisplayConfigBufferSizes(
            uint flags,
            out uint pathCount,
            out uint modeCount);

        [DllImport("user32.dll")]
        internal static extern int QueryDisplayConfig(
            uint flags,
            ref uint pathCount,
            IntPtr paths,
            ref uint modeCount,
            IntPtr modes,
            IntPtr currentTopologyId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int DisplayConfigGetDeviceInfo(
            ref DisplayConfigSourceDeviceName request);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int DisplayConfigGetDeviceInfo(
            ref DisplayConfigTargetDeviceName request);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(
            IntPtr deviceContext,
            IntPtr clippingRectangle,
            MonitorEnumProc callback,
            IntPtr data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(
            IntPtr monitor,
            ref MonitorInfoEx monitorInfo);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayDevices(
            string device,
            uint deviceIndex,
            ref DisplayDevice displayDevice,
            uint flags);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
            IntPtr monitor,
            out uint numberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPhysicalMonitorsFromHMONITOR(
            IntPtr monitor,
            uint physicalMonitorArraySize,
            [Out] PhysicalMonitor[] physicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyPhysicalMonitors(
            uint physicalMonitorArraySize,
            [In] PhysicalMonitor[] physicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorCapabilities(
            IntPtr physicalMonitor,
            out uint monitorCapabilities,
            out uint supportedColorTemperatures);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorBrightness(
            IntPtr physicalMonitor,
            out uint minimumBrightness,
            out uint currentBrightness,
            out uint maximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetMonitorBrightness(
            IntPtr physicalMonitor,
            uint newBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetVCPFeatureAndVCPFeatureReply(
            IntPtr physicalMonitor,
            byte vcpCode,
            out McVcpCodeType vcpCodeType,
            out uint currentValue,
            out uint maximumValue);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetVCPFeature(
            IntPtr physicalMonitor,
            byte vcpCode,
            uint newValue);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            ref int attributeValue,
            int attributeSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(
            IntPtr windowHandle,
            string subApplicationName,
            string subIdList);
    }

    public static class InteropLayout
    {
        public static int DisplayPathSize
        {
            get { return Marshal.SizeOf(typeof(NativeMethods.DisplayConfigPathInfo)); }
        }

        public static int SourceNameSize
        {
            get { return Marshal.SizeOf(typeof(NativeMethods.DisplayConfigSourceDeviceName)); }
        }

        public static int TargetNameSize
        {
            get { return Marshal.SizeOf(typeof(NativeMethods.DisplayConfigTargetDeviceName)); }
        }

        public static int DisplayModeBufferElementSize
        {
            get { return 64; }
        }
    }
}
