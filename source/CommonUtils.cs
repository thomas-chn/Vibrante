using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PointF = System.Drawing.PointF;

namespace Vibrante
{
    internal static class CommonUtils
    {
        
        // Round a value to the nearest/next/previous multiple of another value
        public static int RoundToNearestMultiple(float value, float multiple)
        {
            return (int)(Math.Round(value / multiple) * multiple);
        }
        public static int RoundToNextMultiple(float value, float multiple)
        {
            return (int)(Math.Ceiling(value / multiple) * multiple);
        }
        public static int RoundToPreviousMultiple(float value, float multiple)
        {
            return (int)(Math.Floor(value / multiple) * multiple);
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
        public static float GetMaxY(List<PointF> points)
        {
            float maxY = 0;
            foreach (PointF point in points)
            {
                maxY = Math.Max(maxY, point.Y);
            }
            return maxY;
        }

        public static byte[] PointListToBytes(List<PointF> points)
        {
            byte[] bytes = new byte[points.Count * 8];
            for (int i = 0; i < points.Count; i++)
            {
                byte[] xBytes = BitConverter.GetBytes(points[i].X);
                byte[] yBytes = BitConverter.GetBytes(points[i].Y);
                xBytes.CopyTo(bytes, i * 8);
                yBytes.CopyTo(bytes, i * 8 + 4);
            }
            return bytes;
        }

        public static List<PointF> BytesToPointList(byte[] bytes)
        {
            List<PointF> points = new List<PointF>();
            for (int i = 0; i < bytes.Length; i += 8)
            {
                float x = BitConverter.ToSingle(bytes, i);
                float y = BitConverter.ToSingle(bytes, i + 4);
                points.Add(new PointF(x, y));
            }
            return points;
        }
        
    }
}
