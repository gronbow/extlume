using System;

namespace ExtLume
{
    public static class BrightnessMath
    {
        public static int ClampPercent(int percent)
        {
            if (percent < 0)
            {
                return 0;
            }

            if (percent > 100)
            {
                return 100;
            }

            return percent;
        }

        public static int RawToPercent(uint minimum, uint maximum, uint current)
        {
            if (maximum <= minimum)
            {
                return 0;
            }

            uint bounded = current;
            if (bounded < minimum)
            {
                bounded = minimum;
            }
            else if (bounded > maximum)
            {
                bounded = maximum;
            }

            double ratio = ((double)bounded - minimum) / ((double)maximum - minimum);
            return ClampPercent((int)Math.Round(ratio * 100.0, MidpointRounding.AwayFromZero));
        }

        public static uint PercentToRaw(uint minimum, uint maximum, int percent)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            int bounded = ClampPercent(percent);
            double raw = minimum + (((double)maximum - minimum) * bounded / 100.0);
            double rounded = Math.Round(raw, MidpointRounding.AwayFromZero);
            if (rounded < minimum)
            {
                return minimum;
            }

            if (rounded > maximum)
            {
                return maximum;
            }

            return (uint)rounded;
        }

        public static double SoftwareOpacity(int percent)
        {
            int bounded = ClampPercent(percent);
            return Math.Round((100 - bounded) / 100.0 * 0.85, 3);
        }
    }
}
