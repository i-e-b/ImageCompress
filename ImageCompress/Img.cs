using System.Drawing;
using System.Linq;

namespace ImageCompress
{
    public static class Img
    {
        /// <summary>
        /// Pick a dark color by average-of-4
        /// </summary>
        public static Color LowerColor_AveOf4(Color[] cols)
        {
            long R=0, G=0, B=0;
            for (int i = 0; i < 4; i++)
            {
                R+=cols[i].R;
                G+=cols[i].G;
                B+=cols[i].B;
            }
            return Color.FromArgb((int)(R/4), (int)(G/4), (int)(B/4));
        }

        /// <summary>
        /// Pick dark color by YUV decaying
        /// </summary>
        public static Color LowerColor(Color[] cols)
        {
            const double fac = 2.59285714285714;
            double Y=0, cb=0, cr=0;
            for (int i = 0; i < 8; i++)
            {
                var x = Ycbcr_32(cols[i]);
                Y  += x[0] / (i+1);
                cb += x[1] / (i+1);
                cr += x[2] / (i+1);
            }
            return Color_32(Y / fac, cb / fac, cr / fac);
        }
        
        /// <summary>
        /// Pick a light color by average-of-4
        /// </summary>
        public static Color UpperColor_AveOf4(Color[] cols)
        {
            long R=0, G=0, B=0;
            for (int i = 12; i < 16; i++)
            {
                R+=cols[i].R;
                G+=cols[i].G;
                B+=cols[i].B;
            }
            return Color.FromArgb((int)(R/4), (int)(G/4), (int)(B/4));
        }
        
        /// <summary>
        /// Pick light color by YUV decaying
        /// </summary>
        public static Color UpperColor(Color[] cols)
        {
            const double fac = 2.59285714285714;
            double Y=0, cb=0, cr=0;
            for (int i = 0; i < 8; i++)
            {
                var x = Ycbcr_32(cols[15 - i]);
                Y  += x[0] / (i+1);
                cb += x[1] / (i+1);
                cr += x[2] / (i+1);
            }
            return Color_32(Y / fac, cb / fac, cr / fac);
        }
        

        /// <summary>
        /// Lossless conversion to Ycbcr
        /// </summary>
        public static double[] Ycbcr_32(Color c)
        {
            var Y = 16 + (0.257 * c.R + 0.504 * c.G + 0.098 * c.B);
            var cb = 128 + (-0.148 * c.R + -0.291 * c.G + 0.439 * c.B);
            var cr = 128 + (0.439 * c.R + -0.368 * c.G + -0.071 * c.B);

            return new []{ Y, cb, cr };
        }

        /// <summary>
        /// Lossless conversion from Ycbcr 
        /// </summary>
        public static Color Color_32(double Y, double cb, double cr)
        {
            var R = clip(1.164 * (Y - 16) + 0.0 * (cb - 128) + 1.596 * (cr - 128));
            var G = clip(1.164 * (Y - 16) + -0.392 * (cb - 128) + -0.813 * (cr - 128));
            var B = clip(1.164 * (Y - 16) + 2.017 * (cb - 128) + 0.0 * (cr - 128));

            return Color.FromArgb(R, G, B);
        }
        
        public static int clip(double v) {
            if (v > 255) return 255;
            if (v < 0) return 0;
            return (int)v;
        }


        /// <summary>
        /// Capture 4x4 array of brightness into a linear list of 16 elements
        /// </summary>
        public static Color[] CaptureColors(Bitmap input, int x, int y)
        {
            return new[]{
                input.GetPixel(x, y),  input.GetPixel(x+1, y),  input.GetPixel(x+2, y),  input.GetPixel(x+3, y),
                input.GetPixel(x, y+1),input.GetPixel(x+1, y+1),input.GetPixel(x+2, y+1),input.GetPixel(x+3, y+1),
                input.GetPixel(x, y+2),input.GetPixel(x+1, y+2),input.GetPixel(x+2, y+2),input.GetPixel(x+3, y+2),
                input.GetPixel(x, y+3),input.GetPixel(x+1, y+3),input.GetPixel(x+2, y+3),input.GetPixel(x+3, y+3)
            };
        }

        /// <summary>
        /// Capture 4x4 array of brightness into a linear list of 16 elements
        /// </summary>
        public static byte[] CalcBrightness(Color[] input)
        {
            return input.Select(Brightness).ToArray();
        }

        /// <summary>
        /// YCrCb brightness
        /// </summary>
        public static byte Brightness(Color c){
            return (byte)(16+(0.257 * c.R + 0.504 * c.G + 0.098 * c.B));
        }
    }
}