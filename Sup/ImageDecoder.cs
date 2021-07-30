using SubtitleParser.Common;
using System.Collections.Generic;
using System.Drawing;

namespace SubtitleParser.Sup
{
    public class ImageDecoder
    {
        int pos = 0;

        public Bitmap Decode(byte[] data, Size size, List<PDSEntryObject> palettes, AppSettings settings)
        {
            pos = 0;
            FastBitmap fastBitmap = new FastBitmap(size, settings);

            for (; ; )
            {
                if (pos >= data.Length)
                {
                    break;
                }
                var ColorTimes = GetColorTimes(data);

                if (ColorTimes.colorIdx == 0 && ColorTimes.times == 0)
                {
                    continue;
                }
                else
                {
                    Color color;
                    if (ColorTimes.colorIdx == 0xff)
                    {
                        color = settings.SupBackground;
                    }
                    else
                    {
                        var rgb = YCbCr2Rgb(palettes[ColorTimes.colorIdx].Luminance,
                                palettes[ColorTimes.colorIdx].ColorDifferenceRed,
                                palettes[ColorTimes.colorIdx].ColorDifferenceBlue);

                        if (palettes[ColorTimes.colorIdx].Transparency != 255)
                        {
                            color = settings.SupBackground;
                        }
                        else
                        {
                            color = Color.FromArgb((int)palettes[ColorTimes.colorIdx].Transparency, (int)rgb.r, (int)rgb.g, (int)rgb.b);
                        }
                    }

                    for (int j = 0; j < ColorTimes.times; j++)
                    {
                        fastBitmap.AddPixel(color);
                    }
                }
            }

            return fastBitmap.GetBitmap();
        }

        private (int colorIdx, ushort times) GetColorTimes(byte[] data)
        {
            byte b0 = data[pos++];
            if (b0 != 0) //CCCCCCCC
            {
                return (b0, 1);
            }

            var b1 = data[pos++];

            if (b1 == 0) //00000000 00000000
            {
                //End of Line 
                return (0, 0);
            }

            var flg = b1 & 0xC0;
            if (flg == 0) //00000000 00LLLLLL
            {
                var times = (ushort)(b1 & 0x3f);
                return (0, times);
            }

            var b2 = data[pos++];
            if (flg == 0x40) //00000000 01LLLLLL LLLLLLLL
            {
                var times = (ushort)(((b1 & 0x3f) << 8) + b2);
                return (0, times);
            }

            if (flg == 0x80) //00000000 10LLLLLL CCCCCCCC
            {
                var times = (ushort)(b1 & 0x3f);
                return (b2, times);
            }

            if (flg == 0xC0) //00000000 11LLLLLL LLLLLLLL CCCCCCCC
            {
                var b3 = data[pos++];
                var times = (ushort)(((b1 & 0x3f) << 8) + b2);
                return (b3, times);
            }

            return (0, 0);
        }

        private (uint r, uint g, uint b) YCbCr2Rgb(uint y, uint cb, uint cr)
        {
            double r, g, b;

            y -= 16;
            cb -= 128;
            cr -= 128;

            var y1 = y * 1.164383562;

            r = y1 + cr * 1.792741071;
            g = y1 - cr * 0.5329093286 - cb * 0.2132486143;
            b = y1 + cb * 2.112401786;

            r = r < 0 ? 0 : (r > 255 ? 255 : r);
            g = g < 0 ? 0 : (g > 255 ? 255 : g);
            b = b < 0 ? 0 : (b > 255 ? 255 : b);

            return ((uint)(r + 0.5), (uint)(g + 0.5), (uint)(b + 0.5));
        }
    }
}
