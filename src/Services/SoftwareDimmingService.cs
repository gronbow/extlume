using System;
using System.Collections.Generic;

namespace ExtLume
{
    public sealed class SoftwareDimmingService : IDisposable
    {
        private readonly Dictionary<string, DimmingOverlayForm> overlays;
        private bool disposed;

        public SoftwareDimmingService()
        {
            overlays = new Dictionary<string, DimmingOverlayForm>(StringComparer.Ordinal);
        }

        public int ActiveOverlayCount
        {
            get { return overlays.Count; }
        }

        public static bool IsSafeTarget(DisplayTarget target)
        {
            return target != null
                && target.IsExternal
                && !target.IsVirtual
                && !target.SharesSourceWithInternal
                && !target.Bounds.IsEmpty;
        }

        public static bool IsSafeGroup(IList<DisplayTarget> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Count; index++)
            {
                if (!IsSafeTarget(targets[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetLevel(DisplayTarget target, int percent)
        {
            if (disposed || !IsSafeTarget(target))
            {
                return;
            }

            DimmingOverlayForm overlay;
            if (!overlays.TryGetValue(target.Id, out overlay) || overlay.IsDisposed)
            {
                overlay = new DimmingOverlayForm(target.Bounds);
                overlays[target.Id] = overlay;
            }

            overlay.SetLevel(target.Bounds, percent);
        }

        public void Clear()
        {
            foreach (KeyValuePair<string, DimmingOverlayForm> pair in overlays)
            {
                pair.Value.Close();
                pair.Value.Dispose();
            }

            overlays.Clear();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Clear();
        }
    }
}
