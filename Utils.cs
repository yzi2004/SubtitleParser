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

        public static string FormatDatetime(uint times)
        {
            var ms = times / 90;
            var MS = ms % 1000;
            var sec = ms / 1000;

            return $"{sec / 3600:00}:{sec % 3600 / 60:00}:{sec % 60:00},{MS:000}";
        }

        public static string GetAppName()
        {
            return AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        }

        public static Version GetAppVer()
        {
            return Assembly.GetEntryAssembly().GetName().Version;
        }

        public static Color TryParseColor(string stringColor)
        {
            if (stringColor.StartsWith("#") && stringColor.Length == 7) //#ffffff
            {
                if (int.TryParse(stringColor.Substring(1, 2), NumberStyles.HexNumber, null, out var r) &&
                        int.TryParse(stringColor.Substring(3, 2), NumberStyles.HexNumber, null, out int g) &&
                       int.TryParse(stringColor.Substring(5, 2), NumberStyles.HexNumber, null, out int b))
                {
                    return Color.FromArgb(r, g, b);
                }
                else
                {
                    return Color.Empty;
                }
            }

            try
            {
                return Color.FromName(stringColor);
            }
            catch (Exception)
            {
                return Color.Empty;
            }
        }

        public static string GetOutputFolder(string inputFile)
        {
            FileInfo fi = new FileInfo(inputFile);
            string path = $"{fi.DirectoryName.TrimEnd('\\')}\\subtitle";
            while (Directory.Exists(path))
            {
                path = $"{fi.DirectoryName.TrimEnd('\\')}\\subtitle_{Path.GetRandomFileName().Substring(0, 4)}";
            }

            Directory.CreateDirectory(path);

            return path;
        }

        public static ImageFormat ConvImgFmt(string fmt)
        {
            if (fmt == "jpg" || fmt == "jpeg")
                return ImageFormat.Jpeg;
            if (fmt == "bmp")
                return ImageFormat.Bmp;
            if (fmt == "gif")
                return ImageFormat.Gif;
            if (fmt == "tiff")
                return ImageFormat.Tiff;
            return !(fmt == "png") ? ImageFormat.Jpeg : ImageFormat.Png;
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
