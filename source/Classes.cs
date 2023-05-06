using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Vibrante.UserControls;

using PointF = System.Drawing.PointF;

namespace Vibrante.Classes
{
    class Interpolation
    {
        /// <summary>
        /// Name of the interpolation.
        /// </summary>
        internal string name;

        /// <summary>
        /// Interpolation function between two points.
        /// </summary>
        internal interpolationFunction function;

        /// <summary>
        /// Custom line drawing function used by UpdateCanvas instead of the usual function.
        /// </summary>
        internal drawLineFunction customLineDrawingFunction = null;

        /// <summary>
        /// Interpolation function between two points.
        /// </summary>
        /// <param name="point1">First point.</param>
        /// <param name="point2">Second point.</param>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>Result of the interpolation (y).</returns>
        internal delegate float interpolationFunction(PointF point1, PointF point2, float x);

        /// <summary>
        /// Create elements to draw a line between two points.
        /// </summary>
        /// <param name="point1">First point.</param>
        /// <param name="point2">Second point.</param>
        /// <returns>Array of elements to draw the line.</returns>
        internal delegate FrameworkElement[] drawLineFunction(PointF point1, PointF point2, Brush color);
    }

    class ComposerTab
    {
        /// <summary>
        /// Unity symbol, displayed after the values.
        /// </summary>
        internal string unitSymbol;

        /// <summary>
        /// Color used for tab items.
        /// </summary>
        internal System.Windows.Media.SolidColorBrush mainColor;

        /// <summary>
        /// Vertical zoom factor. For example, 0.5 is two times bigger and 2 is two times smaller.
        /// </summary>
        internal float verticalZoom = 1;

        /// <summary>
        /// Minimum value displayed (at the bottom of the canvas).
        /// </summary>
        internal float verticalPosition = 0;

        /// <summary>
        /// Number of units between each graduation.
        /// </summary>
        internal float unitsPerGrad;

        /// <summary>
        /// Number of pixels between each graduation
        /// </summary>
        internal float pixelsPerGrad;

        /// <summary>
        /// Minimum accessible value.
        /// </summary>
        internal float minValue;

        /// <summary>
        /// Maximum accessible value.
        /// </summary>
        internal float maxValue;

        
        /// <summary>
        /// Precalculated coefficient: pixelsPerGrad / unitsPerGrad.
        /// </summary>
        internal float unitToPixelCoeff;

        /// <summary>
        /// Precalculated coefficient: unitsPerGrad / pixelsPerGrad.
        /// </summary>
        internal float pixelToUnitCoeff;


        /// <summary>
        /// List containing all points. Must be always sorted according to the time position.
        /// </summary>
        internal List<PointF> pointList = new List<PointF>();

        /// <summary>
        /// Index of the point currently clicked. Null if no point is clicked.
        /// </summary>
        internal int? clickedPointIndex = null;

        /// <summary>
        /// Indicates the constant value of the tab. Null if the value is not constant.
        /// </summary>
        internal float? constantValue = null;

        /// <summary>
        /// Interpolation function to use between points.
        /// </summary>
        internal Interpolation interpolation;

        /// <summary>
        /// Indicates if the tab has changed since the last canvas render.
        /// </summary>
        internal bool hasChanged = false;

        /// <summary>
        /// Update <see cref="unitToPixelCoeff"/> and <see cref="pixelToUnitCoeff"/> based on <see cref="pixelsPerGrad"/> and <see cref="unitsPerGrad"/>.
        /// </summary>
        internal void UpdateCoefficients()
        {
            unitToPixelCoeff = pixelsPerGrad / unitsPerGrad;
            pixelToUnitCoeff = 1 / unitToPixelCoeff;
        }

        /// <summary>
        /// Convert a value from the tab unit to pixels.
        /// </summary>
        internal float UnitToPixel(float value)
        {
            return value * unitToPixelCoeff / verticalZoom;
        }

        /// <summary>
        /// Convert a value from pixels to the tab unit.
        /// </summary>
        internal float PixelToUnit(float value)
        {
            return value * pixelToUnitCoeff * verticalZoom;
        }
    
        internal float GetValueFromPointList(float x, ref int start_index)
        {
            if (pointList.Count > 0)
            {
                if (x < pointList.First().X)
                {
                    return pointList.First().Y;
                }
                else if (x >= pointList.Last().X)
                {
                    return pointList.Last().Y;
                }
                else
                {
                    PointF pointAfter = pointList[start_index + 1];

                    // If the 2nd point has been passed, increment the index of the current point until the 2nd point is after again
                    while (pointAfter.X < x)
                    {
                        start_index += 1;
                        pointAfter = pointList[start_index + 1];
                    }

                    PointF pointBefore = pointList[start_index];

                    return interpolation.function(pointBefore, pointAfter, x);
                }

            }
            else
            {
                return float.NaN;
            }
            
        }


    }
}
