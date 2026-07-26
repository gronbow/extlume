using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExtLume
{
    internal static class DpiLayout
    {
        internal const int LogicalDpi = 96;

        internal static int GetWindowDpi(Control control)
        {
            if (control == null)
            {
                return LogicalDpi;
            }

            Control dpiOwner = control.TopLevelControl ?? control;
            if (!dpiOwner.IsHandleCreated)
            {
                return LogicalDpi;
            }

            try
            {
                uint dpi = NativeMethods.GetDpiForWindow(dpiOwner.Handle);
                return Normalize((int)dpi);
            }
            catch (DllNotFoundException)
            {
                return LogicalDpi;
            }
            catch (EntryPointNotFoundException)
            {
                return LogicalDpi;
            }
        }

        internal static int GetGraphicsDpi(Graphics graphics)
        {
            if (graphics == null)
            {
                return LogicalDpi;
            }

            return Normalize((int)Math.Round(graphics.DpiX));
        }

        internal static int GetSystemDpi()
        {
            try
            {
                using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                {
                    return GetGraphicsDpi(graphics);
                }
            }
            catch (Exception)
            {
                return LogicalDpi;
            }
        }

        internal static float ScaleFactor(int sourceDpi, int targetDpi)
        {
            int safeSource = Normalize(sourceDpi);
            int safeTarget = Normalize(targetDpi);
            return safeTarget / (float)safeSource;
        }

        internal static float PaintScale(
            Control control,
            Graphics graphics)
        {
            int dpi = control != null && control.IsHandleCreated
                ? GetWindowDpi(control)
                : GetGraphicsDpi(graphics);
            return ScaleFactor(LogicalDpi, dpi);
        }

        internal static int ScaleLogical(int value, int targetDpi)
        {
            return (int)Math.Round(
                value * ScaleFactor(LogicalDpi, targetDpi),
                MidpointRounding.AwayFromZero);
        }

        internal static void ScaleControl(
            Control control,
            int sourceDpi,
            int targetDpi)
        {
            if (control == null)
            {
                return;
            }

            float factor = ScaleFactor(sourceDpi, targetDpi);
            if (Math.Abs(factor - 1F) < 0.001F)
            {
                return;
            }

            control.Scale(new SizeF(factor, factor));
        }

        internal static void ScaleFonts(
            Control control,
            int sourceDpi,
            int targetDpi)
        {
            if (control == null)
            {
                return;
            }

            bool inheritsParentFont = control.Parent != null
                && Object.ReferenceEquals(
                    control.Font,
                    control.Parent.Font);
            for (int index = 0; index < control.Controls.Count; index++)
            {
                ScaleFonts(
                    control.Controls[index],
                    sourceDpi,
                    targetDpi);
            }

            float factor = ScaleFactor(sourceDpi, targetDpi);
            if (Math.Abs(factor - 1F) >= 0.001F
                && !inheritsParentFont
                && control.Font != null)
            {
                Font current = control.Font;
                float pointSize = current.SizeInPoints * factor;
                control.Font = new Font(
                    current.FontFamily,
                    Math.Max(1F, pointSize),
                    current.Style,
                    GraphicsUnit.Point,
                    current.GdiCharSet,
                    current.GdiVerticalFont);
            }
        }

        private static int Normalize(int dpi)
        {
            return dpi < LogicalDpi ? LogicalDpi : dpi;
        }
    }
}
