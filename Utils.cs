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

       
        /// <summary>
        /// Get two bytes word stored in endian order
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="index">Index in byte array</param>
        /// <returns>Word as int</returns>
        public static int GetEndianWord(byte[] buffer, int index)
        {
            if (index + 1 < buffer.Length)
            {
                return (buffer[index] << 8) | buffer[index + 1];
            }

            return 0;
        }
        
    }
}
