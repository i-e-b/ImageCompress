using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ImageCompress
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TestSlice();
        }

        private void TestSlice()
        {

            // Plan:
            //     Each 4x4 block, sort pixels by brightness. Average the top and bottom (half/quarter)
            //     Each pixel, pick 1 for brighter, 0 for darker.
            // Then,
            //     Try setting colors in middle of encoded blocks, and smoothly transition between them. (BEFORE picking encoding)

            if (outputPictureBox.Image != null) {
                outputPictureBox.Image.Dispose();
            }
            Bitmap input = new Bitmap(Image.FromFile("Machu.png"));
            {
                for (int y = 0; y < input.Height - 1; y += 4)
                {
                    for (int x = 0; x < input.Width - 1; x += 4)
                    {
                        var cols = CaptureColors(input, x, y);
                        var bl = CalcBrightness(cols);
                        Array.Sort(bl, cols);

                        var ave = bl.Average(b=>b);
                        var loCol = LowerColor(cols);//(bl[0]+bl[1]+bl[2]+bl[3]) / 4;
                        var hiCol = UpperColor(cols);//(bl[12]+bl[13]+bl[14]+bl[15]) / 4;

                        for (int ox = 0; ox < 4; ox++)
                        for (int oy = 0; oy < 4; oy++)
                        {
                            var obr = Brightness(input.GetPixel(x+ox, y+oy));
                            input.SetPixel(x+ox, y+oy,
                                (obr > ave) ? hiCol : loCol
                                );
                        }
                    }
                }
            }
            outputPictureBox.Image = input;
            Width = input.Width + 18;
            Height = input.Height + 41;
        }

        private Color LowerColor(Color[] cols)
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

        private Color UpperColor(Color[] cols)
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
        /// Capture 4x4 array of brightness into a linear list of 16 elements
        /// </summary>
        private Color[] CaptureColors(Bitmap input, int x, int y)
        {
            return new[]{
                input.GetPixel(x, y),input.GetPixel(x+1, y),input.GetPixel(x+2, y),input.GetPixel(x+3, y),
               input.GetPixel(x, y+1),input.GetPixel(x+1, y+1),input.GetPixel(x+2, y+1),input.GetPixel(x+3, y+1),
                input.GetPixel(x, y+2),input.GetPixel(x+1, y+2),input.GetPixel(x+2, y+2),input.GetPixel(x+3, y+2),
                input.GetPixel(x, y+3),input.GetPixel(x+1, y+3),input.GetPixel(x+2, y+3),input.GetPixel(x+3, y+3)
            };
        }

        /// <summary>
        /// Capture 4x4 array of brightness into a linear list of 16 elements
        /// </summary>
        private byte[] CalcBrightness(Color[] input)
        {
            return input.Select(Brightness).ToArray();
        }

        /// <summary>
        /// Ycrcb brightness
        /// </summary>
        private static byte Brightness(Color c){
            int rx = (c.R * 65) >> 8;
            int gx = (c.G * 129) >> 8;
            int bx = (c.R * 25) >> 8;
            return (byte)(16+rx+gx+bx);
            /*
            var raw = ((c.R * 0.0996) * (c.G * 0.195) + (c.B * 0.038));
            return (raw > 255) ? (byte)255 : (byte)raw;
            */
        }
    }
}
