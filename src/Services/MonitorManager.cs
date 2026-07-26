using System;
using System.Collections.Generic;

namespace ExtLume
{
    public sealed class MonitorManager
    {
        private readonly DisplayDiscoveryService discoveryService;
        private readonly DdcBrightnessService ddcService;

        public MonitorManager()
        {
            discoveryService = new DisplayDiscoveryService();
            ddcService = new DdcBrightnessService();
        }

        public MonitorRefreshResult Refresh()
        {
            MonitorRefreshResult result = new MonitorRefreshResult();
            DisplayDiscoveryResult discovery = DiscoverDisplays();
            result.ActiveDisplayCount = discovery.Targets.Count;
            result.Warnings.AddRange(discovery.Warnings);

            List<List<DisplayTarget>> groups = GroupExternalTargets(discovery.Targets);
            for (int index = 0; index < groups.Count; index++)
            {
                List<DisplayTarget> group = groups[index];
                result.ExternalDisplayCount += group.Count;
                try
                {
                    result.Monitors.AddRange(ProbeTargetGroup(group));
                }
                catch (Exception)
                {
                    result.Warnings.Add("ddc-probe-failed");
                    MonitorDescriptor fallback =
                        CreateSoftwareDescriptorGroup(group);
                    if (fallback != null)
                    {
                        result.Monitors.Add(fallback);
                    }
                    else
                    {
                        result.Warnings.Add("software-dimming-blocked-for-internal-clone");
                    }
                }
            }

            return result;
        }

        public DisplayDiscoveryResult DiscoverDisplays()
        {
            return discoveryService.DiscoverActiveTargets();
        }

        public List<MonitorDescriptor> ProbeTarget(DisplayTarget target)
        {
            return ddcService.Probe(target);
        }

        public List<MonitorDescriptor> ProbeTargetGroup(IList<DisplayTarget> targets)
        {
            return ddcService.ProbeGroup(targets);
        }

        public MonitorDescriptor CreateSoftwareDescriptor(DisplayTarget target)
        {
            List<DisplayTarget> targets = new List<DisplayTarget>();
            targets.Add(target);
            return CreateSoftwareDescriptorGroup(targets);
        }

        public MonitorDescriptor CreateSoftwareDescriptorGroup(IList<DisplayTarget> targets)
        {
            if (!SoftwareDimmingService.IsSafeGroup(targets))
            {
                return null;
            }

            DisplayTarget first = targets[0];
            string identity = String.Empty;
            List<string> names = new List<string>();
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

            return new MonitorDescriptor
            {
                Id = MonitorIdentity.StableId(identity + "|software"),
                DisplayName = String.Join(" / ", names.ToArray()),
                Target = first,
                ControlKind = BrightnessControlKind.SoftwareDimming,
                PhysicalIndex = -1,
                MinimumRaw = 0,
                MaximumRaw = 100,
                CurrentRaw = 100,
                CurrentPercent = 100
            };
        }

        public static List<List<DisplayTarget>> GroupExternalTargets(
            IList<DisplayTarget> targets)
        {
            List<List<DisplayTarget>> groups = new List<List<DisplayTarget>>();
            Dictionary<string, List<DisplayTarget>> byDevice =
                new Dictionary<string, List<DisplayTarget>>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> internalSources =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < targets.Count; index++)
            {
                DisplayTarget target = targets[index];
                if (target != null
                    && target.IsInternal
                    && !String.IsNullOrEmpty(target.DeviceName))
                {
                    internalSources.Add(target.DeviceName);
                }
            }

            for (int index = 0; index < targets.Count; index++)
            {
                DisplayTarget target = targets[index];
                if (target == null
                    || !target.IsExternal
                    || target.IsVirtual
                    || target.Bounds.IsEmpty)
                {
                    continue;
                }

                target.SharesSourceWithInternal =
                    internalSources.Contains(target.DeviceName);

                List<DisplayTarget> group;
                if (!byDevice.TryGetValue(target.DeviceName, out group))
                {
                    group = new List<DisplayTarget>();
                    byDevice[target.DeviceName] = group;
                    groups.Add(group);
                }

                group.Add(target);
            }

            return groups;
        }

        public BrightnessResult SetHardwareBrightness(
            MonitorDescriptor monitor,
            int percent)
        {
            return ddcService.SetBrightness(monitor, percent);
        }
    }
}
