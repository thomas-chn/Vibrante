using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibrante
{
    internal static class CommonUtils
    {
        
        // Round a value to the nearest/next/previous multiple of another value
        public static double RoundToNearestMultiple(double value, double multiple)
        {
            return Math.Round(value / multiple) * multiple;
        }
        public static double RoundToNextMultiple(double value, double multiple)
        {
            return Math.Ceiling(value / multiple) * multiple;
        }
        public static double RoundToPreviousMultiple(double value, double multiple)
        {
            return Math.Floor(value / multiple) * multiple;
        }

        /// <summary>
        /// Clamp a value between a minimum and maximum value.
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// Return the maximum Y value of a list of points.
        /// </summary>
        public static double GetMaxY(List<System.Windows.Point> points)
        {
            double maxY = 0;
            foreach (System.Windows.Point point in points)
            {
                maxY = Math.Max(maxY, point.Y);
            }
            return maxY;
        }
    }
}
