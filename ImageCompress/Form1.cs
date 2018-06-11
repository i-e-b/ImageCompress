using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
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
            if (outputPictureBox.Image != null) {
                outputPictureBox.Image.Dispose();
            }
            var input = new Bitmap(Image.FromFile("Machu.png"));

            var compr = CompressImage(input);

            using (var fs = File.Open("Output.czz", FileMode.Create, FileAccess.Write))
            using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
            {
                compr.WriteToStream(gz);
            }


            Text = "Raw = " + compr.ByteCount() + " bytes, gzip = " + new FileInfo("Output.czz").Length + " bytes";
            using (var fs = File.Open("Output.czz", FileMode.Open, FileAccess.Read))
            using (var gz = new GZipStream(fs, CompressionMode.Decompress)) {
                var final = CompressedImage.ReadFromStream(gz);
                RenderCompressedImage(final, input);
            }

            outputPictureBox.Image = input;
            Width = input.Width + 18;
            Height = input.Height + 41;
        }

        private void RenderCompressedImage(CompressedImage compr, Bitmap target)
        {
            for (int y = 0; y < target.Height - 1; y++)
            {
                for (int x = 0; x < target.Width - 1; x++)
                {
                    target.SetPixel(x, y, compr.GetPixel(x, y));
                }
            }
        }

        private CompressedImage CompressImage(Bitmap input)
        {
            var compr = new CompressedImage(input.Width, input.Height);
            for (int y = 0; y < input.Height - 1; y += 4)
            {
                for (int x = 0; x < input.Width - 1; x += 4)
                {
                    var cols = Img.CaptureColors(input, x, y);
                    var bl = CalcBrightness(cols);
                    Array.Sort(bl, cols);

                    var ave = bl.Average(b => b);
                    var loCol = Img.LowerColor(cols);
                    var hiCol = Img.UpperColor(cols);

                    compr.SetColors(x, y, hiCol, loCol, (byte)ave);

                    for (int ox = 0; ox < 4; ox++)
                        for (int oy = 0; oy < 4; oy++)
                        {
                            compr.SetPixel(x + ox, y + oy, input.GetPixel(x + ox, y + oy));
                        }
                }
            }
            return compr;
        }

        /// <summary>
        /// Capture 4x4 array of brightness into a linear list of 16 elements
        /// </summary>
        private byte[] CalcBrightness(Color[] input)
        {
            return input.Select(Img.Brightness).ToArray();
        }
    }
}
