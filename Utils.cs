using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace SubtitleParser
{
    public class Utils
    {
        public const int SubtitleMaximumDisplayMilliseconds = 8000; //8秒
        public const int MinimumMillisecondsBetweenLines = 24; //

        public static TimeSpan Ticks2TimeSpan(uint ticks, bool IsPal = false)
        {
            float ticksPerMillisecond = 90.000F;
            if (!IsPal)
            {
                ticksPerMillisecond = 90.090F * (23.976F / 24F);
            }

            return TimeSpan.FromMilliseconds(Convert.ToDouble(ticks / ticksPerMillisecond));
        }

        public static string GetAppName()
        {
            return AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        }

        public static Version GetAppVer()
        {
            return Assembly.GetEntryAssembly().GetName().Version;
        }

        public static Color TryParseColor(string stringColor, Color defaultColor)
        {
            if ((stringColor.StartsWith("#") && stringColor.Length == 7) //#ffffff
                || (stringColor.Length == 6)) //RRGGBB
            {
                stringColor = stringColor.TrimStart('#');
                if (int.TryParse(stringColor.Substring(0, 2), NumberStyles.HexNumber, null, out var r) &&
                        int.TryParse(stringColor.Substring(2, 2), NumberStyles.HexNumber, null, out int g) &&
                       int.TryParse(stringColor.Substring(4, 2), NumberStyles.HexNumber, null, out int b))
                {
                    return Color.FromArgb(r, g, b);
                }
                else
                {
                    return defaultColor;
                }
            }

            try
            {
                return Color.FromName(stringColor);
            }
            catch (Exception)
            {
                return defaultColor;
            }
        }

        public static string GetOutputFolder(string inputFile)
        {
            FileInfo fi = new FileInfo(inputFile);
            string path = $"{fi.DirectoryName.TrimEnd('\\')}\\subtitle";
            while (Directory.Exists(path))
            {
                path = $"{fi.DirectoryName.TrimEnd('\\')}\\subtitle_{DateTime.Now:MMddHHmmss}";
            }

            Directory.CreateDirectory(path);

            return path;
        }

        public static string EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir.TrimEnd('\\');
        }

        public static ImageFormat ConvImgFmt(string fmt)
        {
            switch (fmt)
            {
                case "jpg":
                case "jpeg":
                    return ImageFormat.Jpeg;
                case "bmp":
                    return ImageFormat.Bmp;
                case "gif":
                    return ImageFormat.Gif;
                case "tiff":
                    return ImageFormat.Tiff;
                default:
                    return ImageFormat.Jpeg;
            }
        }

        public static string IntToHex(UInt64 value, int digits)
        {
            return value.ToString("X").PadLeft(digits, '0');
        }

        public static string IntToHex(int value, int digits)
        {
            return value.ToString("X").PadLeft(digits, '0');
        }

        public static string IntToBin(long value, int digits)
        {
            return Convert.ToString(value, 2).PadLeft(digits, '0');
        }


        public static (uint r, uint g, uint b) YCbCr2Rgb(uint y, uint cb, uint cr)
        {
            double r, g, b;

            y -= 16;
            cb -= 128;
            cr -= 128;

            var y1 = y * 1.164383562;

            r = y1 + cr * 1.792741071;
            g = y1 - cr * 0.5329093286 - cb * 0.2132486143;
            b = y1 + cb * 2.112401786;


            r = (uint)(r + 0.5) < 0 ? 0 : ((uint)(r + 0.5) > 255 ? 255 : r + 0.5);
            g = (uint)(g + 0.5) < 0 ? 0 : ((uint)(g + 0.5) > 255 ? 255 : g + 0.5);
            b = (uint)(b + 0.5) < 0 ? 0 : ((uint)(b + 0.5) > 255 ? 255 : b + 0.5);

            return ((uint)r, (uint)g, (uint)b);
        }

    }
}
