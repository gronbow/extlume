using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ExtLume
{
    public static class MonitorIdentity
    {
        public static string StableId(string value)
        {
            string source = value ?? String.Empty;
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(source));
                StringBuilder builder = new StringBuilder(24);
                for (int index = 0; index < 12; index++)
                {
                    builder.Append(hash[index].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string NormalizeFriendlyName(string friendlyName, string devicePath)
        {
            string name = Clean(friendlyName);
            string model = ExtractModelCode(devicePath);

            if (IsGenericName(name) && !String.IsNullOrEmpty(model))
            {
                return model;
            }

            if (!String.IsNullOrEmpty(name))
            {
                return name;
            }

            if (!String.IsNullOrEmpty(model))
            {
                return model;
            }

            return "External display";
        }

        public static string ExtractModelCode(string devicePath)
        {
            string path = Clean(devicePath);
            if (String.IsNullOrEmpty(path))
            {
                return String.Empty;
            }

            string normalized = path.Replace('#', '\\');
            string[] parts = normalized.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < parts.Length - 1; index++)
            {
                if (String.Equals(parts[index], "DISPLAY", StringComparison.OrdinalIgnoreCase))
                {
                    return Clean(parts[index + 1]);
                }
            }

            return String.Empty;
        }

        public static bool IsGenericName(string name)
        {
            string cleaned = Clean(name);
            if (String.IsNullOrEmpty(cleaned))
            {
                return true;
            }

            string lower = cleaned.ToLowerInvariant();
            return lower == "generic pnp monitor"
                || lower == "generic non-pnp monitor"
                || lower == "pnp monitor"
                || lower == "display"
                || lower == "monitor"
                || lower == "unknown";
        }

        public static string Clean(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            return value.Replace("\0", String.Empty).Trim();
        }
    }

    public static class MonitorClassifier
    {
        public const uint OutputOther = 0xFFFFFFFF;
        public const uint OutputInternal = 0x80000000;
        public const uint OutputLvds = 6;
        public const uint OutputDisplayPortEmbedded = 11;
        public const uint OutputUdiEmbedded = 13;
        public const uint OutputMiracast = 15;
        public const uint OutputIndirectWired = 16;
        public const uint OutputIndirectVirtual = 17;
        public const uint OutputDisplayPortUsbTunnel = 18;

        public static bool IsInternalOutput(uint technology)
        {
            return technology == OutputInternal
                || technology == OutputLvds
                || technology == OutputDisplayPortEmbedded
                || technology == OutputUdiEmbedded;
        }

        public static bool IsVirtualOutput(uint technology)
        {
            return technology == OutputIndirectVirtual;
        }

        public static bool IsKnownExternalOutput(uint technology)
        {
            if (technology == OutputOther || IsInternalOutput(technology) || IsVirtualOutput(technology))
            {
                return false;
            }

            return technology <= OutputDisplayPortUsbTunnel;
        }

        public static void ClassifyTargets(IList<DisplayTarget> targets)
        {
            bool hasKnownInternal = false;
            for (int index = 0; index < targets.Count; index++)
            {
                DisplayTarget target = targets[index];
                target.IsInternal = IsInternalOutput(target.OutputTechnology);
                target.IsVirtual = IsVirtualOutput(target.OutputTechnology);
                target.IsExternal = IsKnownExternalOutput(target.OutputTechnology);
                hasKnownInternal = hasKnownInternal || target.IsInternal;
            }

            if (!hasKnownInternal || targets.Count < 2)
            {
                return;
            }

            for (int index = 0; index < targets.Count; index++)
            {
                DisplayTarget target = targets[index];
                if (!target.IsInternal
                    && !target.IsVirtual
                    && !target.IsExternal
                    && !target.Bounds.IsEmpty)
                {
                    target.IsExternal = true;
                }
            }
        }

        public static string OutputTechnologyName(uint technology)
        {
            switch (technology)
            {
                case 0:
                    return "HD15";
                case 4:
                    return "DVI";
                case 5:
                    return "HDMI";
                case OutputLvds:
                    return "LVDS";
                case 10:
                    return "DisplayPort";
                case OutputDisplayPortEmbedded:
                    return "Embedded DisplayPort";
                case 12:
                    return "UDI";
                case OutputUdiEmbedded:
                    return "Embedded UDI";
                case OutputMiracast:
                    return "Miracast";
                case OutputIndirectWired:
                    return "Indirect wired";
                case OutputIndirectVirtual:
                    return "Virtual";
                case OutputDisplayPortUsbTunnel:
                    return "USB-C DisplayPort";
                case OutputInternal:
                    return "Internal";
                default:
                    return "Other";
            }
        }
    }
}
