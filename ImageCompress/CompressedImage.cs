using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ImageCompress
{
    // Plan:
    //     Each 4x4 block, sort pixels by brightness. Average the top and bottom (half/quarter)
    //     Each pixel, pick 1 for brighter, 0 for darker.
    // Then,
    //     Try setting colors in middle of encoded blocks, and smoothly transition between them. (BEFORE picking encoding)

    public class CompressedImage {
        // These 4 fields are stored as the image
        public UInt16[] Pixels; // each Int16 is a 4x4 grid of 1bpp values
        public UInt16[] HiColors; // brighter color for each grid point
        public UInt16[] LoColors; // darker color for each grid point
        public UInt16 Width; // Width of target image, in pixels

        // These fields are not stored, but are used to build the others
        private readonly int _gridWidth;
        private readonly byte[] _gridBrightness; // average brightness across grid positions

        public CompressedImage(int width, int height)
        {
            Width = (UInt16)width;
            _gridWidth = (int)Math.Ceiling(width / 4.0);
            var _gridHeight = (int)Math.Ceiling(height / 4.0);

            var gridCount = (_gridWidth * _gridHeight);
            Pixels = new UInt16[gridCount];
            HiColors = new UInt16[gridCount];
            LoColors = new UInt16[gridCount];
            _gridBrightness = new byte[gridCount];
        }

        /// <summary>
        /// Write image to a stream
        /// </summary>
        public void WriteToStream(Stream s) {
            var w = new BinaryWriter(s);
            w.Write(Width);
            for (int i = 0; i < Pixels.Length; i++)
            {
                w.Write(HiColors[i]);
                w.Write(LoColors[i]);
                w.Write(Pixels[i]);
            }
        }

        /// <summary>
        /// Read image from a stream
        /// </summary>
        public static CompressedImage ReadFromStream(Stream s) {
            var r = new BinaryReader(s);

            var pixels = new List<UInt16>();
            var hiColors = new List<UInt16>();
            var loColors = new List<UInt16>();

            var width = r.ReadUInt16();

            try
            {
                while (true)
                {
                    hiColors.Add(r.ReadUInt16());
                    loColors.Add(r.ReadUInt16());
                    pixels.Add(r.ReadUInt16());
                }
            }
            catch (EndOfStreamException)
            {
                // done
            }

            var height = (pixels.Count / width) * 4;
            var output = new CompressedImage(width, height);
            output.HiColors = hiColors.ToArray();
            output.LoColors = loColors.ToArray();
            output.Pixels = pixels.ToArray();
            return output;
        }

        /// <summary>
        /// Return the size of the stored image in bytes
        /// </summary>
        public int ByteCount(){
            var pixSz = Utils.SizeOf(Pixels[0]);
            var hiSz = Utils.SizeOf(HiColors[0]);
            var loSz = Utils.SizeOf(LoColors[0]);
            var widthSz = Utils.SizeOf(Width);
            return (Pixels.Length * pixSz) + (HiColors.Length * hiSz) + (LoColors.Length * loSz) + widthSz; // color and pixel tables, plus width
        }

        /// <summary>
        /// Store hi and lo colors for a grid position.
        /// This must be done for all positions befor setting pixels
        /// </summary>
        public void SetColors(int x, int y, Color hi, Color lo, byte averageBrightness) {
            var gidx = (x / 4) + ((y / 4) * _gridWidth);
            HiColors[gidx] = Ycbcr_16(hi);
            LoColors[gidx] = Ycbcr_16(lo);
            _gridBrightness[gidx] = averageBrightness;
        }

        /// <summary>
        /// Set a pixel value based on an original color.
        /// `SetColors` must be called for all grid positions first
        /// </summary>
        public void SetPixel(int x, int y, Color orig) {
            var gidx = (x / 4) + ((y / 4) * _gridWidth);
            var pidx = (x % 4) + ((y % 4) * 4); // pixel offset in grid

            if (Img.Brightness(orig) > _gridBrightness[gidx]) {
                // set pixel bright:
                Pixels[gidx] |= (ushort)(1 << pidx);
            } else {
                // set pixel dark:
                Pixels[gidx] &= (ushort)(~(1 << pidx));
            }
        }

        /// <summary>
        /// Get the rendered color of a pixel
        /// </summary>
        public Color GetPixel(int x, int y) {
            var gidx = (x / 4) + ((y / 4) * _gridWidth);
            var pidx = (x % 4) + ((y % 4) * 4); // pixel offset in grid

            
            var lo = (Pixels[gidx] & (short)(1 << pidx)) == 0;

            return (lo) ? Color_16(LoColors[gidx]) : Color_16(HiColors[gidx]);
        }

        /// <summary>
        /// Lossy conversion to Ycbcr (24 bit, stored as 32)
        /// </summary>
        public static UInt32 Ycbcr_32(Color c)
        {
            var Y = clip(16 + (0.257 * c.R + 0.504 * c.G + 0.098 * c.B));
            var cb = clip(128 + (-0.148 * c.R + -0.291 * c.G + 0.439 * c.B));
            var cr = clip(128 + (0.439 * c.R + -0.368 * c.G + -0.071 * c.B));

            return (UInt32)((Y << 16) + (cb << 8) + (cr));
        }

        /// <summary>
        /// Lossy conversion from Ycbcr (24 bit, stored as 32)
        /// </summary>
        public static Color Color_32(UInt32 c)
        {
            long Y =  (c >> 16) & 0xFF;
            long cb = (c >>  8) & 0xFF;
            long cr = (c      ) & 0xFF;
            
            var R = clip(1.164 * (Y - 16) + 0.0 * (cb - 128) + 1.596 * (cr - 128));
            var G = clip(1.164 * (Y - 16) + -0.392 * (cb - 128) + -0.813 * (cr - 128));
            var B = clip(1.164 * (Y - 16) + 2.017 * (cb - 128) + 0.0 * (cr - 128));

            return Color.FromArgb(R, G, B);
        }
        
        /// <summary>
        /// Very lossy conversion to 16 bit Ycbcr
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static UInt16 Ycbcr_16(Color c)
        {
            var Y = clip(16 + (0.257 * c.R + 0.504 * c.G + 0.098 * c.B)) >> 2;     // 6 bps
            var cb = clip(128 + (-0.148 * c.R + -0.291 * c.G + 0.439 * c.B)) >> 3; // 5 bps
            var cr = clip(128 + (0.439 * c.R + -0.368 * c.G + -0.071 * c.B)) >> 3; // 5 bps

            return (UInt16)((Y << 10) + (cb << 5) + (cr));
        }
        
        /// <summary>
        /// Very lossy conversion from 16 bit Ycbcr
        /// </summary>
        public static Color Color_16(UInt32 c)
        {
            long Y =  ((c >> 10) & 0xFF) << 2;
            long cb = ((c >>  5) & 0x1F) << 3;
            long cr = ((c      ) & 0x1F) << 3;
            
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

        public static int clip(long v) {
            if (v > 255) return 255;
            if (v < 0) return 0;
            return (int)v;
        }

    }
}