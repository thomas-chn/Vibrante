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
    }
}
