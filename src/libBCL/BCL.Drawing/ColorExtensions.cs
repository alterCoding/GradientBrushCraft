using System;
using System.Drawing;
using System.Globalization;

namespace AltCoD.BCL.Drawing
{
    public static class ColorExtensions
    {
        public static Color ToGrayScale(this Color color, bool lowContrast = false)
        {
            int level = (int)Math.Round(color.GetBrightness() * 255);

            if (lowContrast)
            {
                if (level > 255 / 2f) level -= 60;
                else level += 60;
            }

            return Color.FromArgb(level, level, level);
        }

        /// <summary>
        /// Compute the subjective distance between 2 colors, base on the relative luminance quantity
        /// (refer to https://www.w3.org/TR/2008/REC-WCAG20-20081211/#sRGB and https://www.w3.org/TR/WCAG21/relative-luminance.html)
        /// </summary>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        /// <remarks>there are better methods but they are far more complicated</remarks>
        public static float ContrastWith(this Color color1, Color color2)
        {
            Color lighter, darker;
            if(color1.GetBrightness() < color2.GetBrightness())
            {
                darker = color1;
                lighter = color2;
            }
            else
            {
                darker = color2;
                lighter = color1;
            }

            double rrgbl = lighter.R / 255.0;
            double grgbl = lighter.G / 255.0;
            double brgbl = lighter.B / 255.0;

            double rrgbd = darker.R / 255.0;
            double grgbd = darker.G / 255.0;
            double brgbd = darker.B / 255.0;

            //using a few magic numbers ...
            double rl = rrgbl <= 0.03928 ? rrgbl / 12.92 : Math.Pow((rrgbl + 0.055) / 1.055, 2.4);
            double gl = grgbl <= 0.03928 ? grgbl / 12.92 : Math.Pow((grgbl + 0.055) / 1.055, 2.4);
            double bl = brgbl <= 0.03928 ? brgbl / 12.92 : Math.Pow((brgbl + 0.055) / 1.055, 2.4);

            double rd = rrgbd <= 0.03928 ? rrgbd / 12.92 : Math.Pow((rrgbd + 0.055) / 1.055, 2.4);
            double gd = grgbd <= 0.03928 ? grgbd / 12.92 : Math.Pow((grgbd + 0.055) / 1.055, 2.4);
            double bd = brgbd <= 0.03928 ? brgbd / 12.92 : Math.Pow((brgbd + 0.055) / 1.055, 2.4);

            //luminance of the lighest
            double luml = 0.2126 * rl + 0.7152 * gl + 0.0722 * bl;
            double lumd = 0.2126 * rd + 0.7152 * gd + 0.0722 * bd;

            return (float)((luml + 0.05) / (lumd + 0.05));
        }

        public static Color InvertColor(this Color color)
        {
            return Color.FromArgb(color.A, (byte)~color.R, (byte)~color.G, (byte)~color.B);
        }

    }

    /// <summary>
    /// User input validation
    /// </summary>
    public class ColorValueInput
    {
        public Color Value { get; set; }

        /// <summary>
        /// required signature by framework to perform validation in a MaskedEditBox instance
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Parse(string value)
        {
            ColorValueInput result = null;

            value = value.Trim().ToLower();

            bool is_hex = false;
            if (value.EndsWith("h")) { is_hex = true; value = value.Substring(0, value.Length - 1); }
            else if (value.StartsWith("0x")) { is_hex = true; value = value.Substring(2); }

            bool is_color; int rgb;
            if (is_hex) is_color = int.TryParse(value, NumberStyles.HexNumber, null, out rgb);
            else is_color = int.TryParse(value, NumberStyles.Integer, null, out rgb);

            if (is_color) result = new ColorValueInput() { Value = Color.FromArgb(rgb) };

            return result;
        }
    }
}
