using System;
using System.Drawing;
using System.Windows.Forms;

namespace AltCoD.UI.WinForms
{
    using BCL.Drawing;

    public static class RGB
    {
        /// <summary>
        /// Attempt to find "a" color with a decent contrast between the two supplied colors
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>one of the arbitrary colors that offer a suitable contrast</returns>
        /// <remarks>contrast quantity is defined as the "relative luminance" ... see https://www.w3.org/TR/WCAG21/relative-luminance.html
        /// </remarks>
        public static Color FindBestContrastColor(Color left, Color right)
        {
            return findBestContrastColor(left, right, useDefault: false, Color.Empty, 20);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="contrast">the minimal expected contrast ratio</param>
        /// <param name="default">a default color that may be used as a 1th order color candidate if its contrast ratio
        /// is suitable</param>
        /// @internal algorithm is fully clunky and loosly defined: 
        /// - firstly we test the contrast ratio of a color darker than the lighter and a color lighter than the darker
        /// - we assume a good contrast arbitrarily defined with the value 20
        /// - if any acceptable value isn't found, we use later inverted colors
        /// - we loop till 20 attempts (darken and lighten) to seek a suitable value
        /// - 10 attempts aren't enough (we can easily find some crappy combinations)
        private static Color findBestContrastColor(Color left, Color right, bool useDefault, Color @default, float contrast)
        {
            bool darker = left.GetBrightness() < right.GetBrightness();

            Color dark = darker ? left : right;
            Color light = darker ? right : left;

            Color c1 = ControlPaint.Dark(light, 0.2f);
            Color c2 = ControlPaint.Light(dark, 0.2f);
            Color c = @default;

            float d1 = c1.ContrastWith(left) + c1.ContrastWith(right);
            float d2 = c2.ContrastWith(left) + c2.ContrastWith(right);

            float d = 1, best;
            if (useDefault)
            {
                d = c.ContrastWith(left) + c.ContrastWith(right);
                best = Math.Max(d1, Math.Max(d2, d));
            }
            else
            {
                best = Math.Max(d1, d2);
            }

            if (best < contrast)
            {
                c1 = c1.InvertColor();
                c2 = c2.InvertColor();

                int loop = 0;
                while (best < contrast && loop++ < 20)
                {
                    d1 = c1.ContrastWith(left) + c1.ContrastWith(right);
                    d2 = c2.ContrastWith(left) + c2.ContrastWith(right);

                    if(useDefault)
                        best = Math.Max(d1, Math.Max(d2, d));
                    else
                        best = Math.Max(d2, d2);

                    c1 = ControlPaint.Dark(c1, 0.1f);
                    c2 = ControlPaint.Light(c2, 0.1f);
                }
            }

            Color color;
            if(useDefault) color = d1 > d2 ? d1 >= d ? c1 : c : d2 >= d ? c2 : c;
            else color = d1 > d2 ? c1 : c2;

            return color;
        }
    }
}
