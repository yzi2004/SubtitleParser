using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace SubtitleParser
{
    public class AppSettings
    {
        public string InputFile { get; set; }
        public string OutputPath { get; set; }
        public ImageFormat ImageFormat { get; set; }
        public Color SupBackground { get; set; } = Color.Transparent;

        /// <summary>
        ///  use by VobSub subtitle.
        /// </summary>
        public bool IsPAL { get; set; }
        public Border ImageBorder { get; set; } = new Border();
        public VobSubFourColors SubCustomColors = new VobSubFourColors();

        public string GetImgExt()
        {
            return ImageFormat.ToString().ToLower().Replace("jpeg", "jpg");
        }

        public bool IsSup => InputFile.ToLower().EndsWith(".sup");
        public bool IsValid => SelfCheck();

        public AppSettings(string[] pars)
        {
            IConfigurationRoot configurationRoot = InitConfig(pars);

            InputFile = configurationRoot["input"];
            OutputPath = configurationRoot["output"];
            SupBackground = Utils.TryParseColor(configurationRoot["sup_background"]);
            ImageFormat = Utils.ConvImgFmt(configurationRoot["format"]);

            var section = configurationRoot.GetSection("img_border");
            string width = section["width"];
            if (!string.IsNullOrWhiteSpace(width) && int.TryParse(width, out var result))
            {
                ImageBorder.Width = result;
            }
            string padding = section["padding"];
            if (!string.IsNullOrWhiteSpace(padding) && int.TryParse(padding, out result))
            {
                ImageBorder.Padding = result;
            }
            string strColor = section["color"];
            if (!string.IsNullOrWhiteSpace(strColor))
            {
                ImageBorder.color = Utils.TryParseColor(strColor);
                if (ImageBorder.color.IsEmpty)
                {
                    ImageBorder.color = Color.Transparent;
                }
            }

             section = configurationRoot.GetSection("vobsub_custom_colors");

            strColor = section["backgroud"];
            if (!string.IsNullOrWhiteSpace(strColor))
            {
                var color = Utils.TryParseColor(strColor);
                if (!color.IsEmpty)
                {
                    SubCustomColors.backgroud = color;
                }
            }
            strColor = section["pattern"];
            if (!string.IsNullOrWhiteSpace(strColor))
            {
                var color = Utils.TryParseColor(strColor);
                if (!color.IsEmpty)
                {
                    SubCustomColors.pattern = color;
                }
            }
            strColor = section["emphasis1"];
            if (!string.IsNullOrWhiteSpace(strColor))
            {
                var color = Utils.TryParseColor(strColor);
                if (!color.IsEmpty)
                {
                    SubCustomColors.emphasis1 = color;
                }
            }
            strColor = section["emphasis2"];
            if (!string.IsNullOrWhiteSpace(strColor))
            {
                var color = Utils.TryParseColor(strColor);
                if (!color.IsEmpty)
                {
                    SubCustomColors.emphasis2 = color;
                }
            }
        }

        private IConfigurationRoot InitConfig(string[] args)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>()
                {{"-i","input"},{"--input","input"},
                 {"-o","output"},{"--output","output"},
                 {"-f","format"},{"--format","format"}};

            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false)
                .AddCommandLine(args, dictionary)
                .Build();
        }

        private bool SelfCheck()
        {
            if (string.IsNullOrEmpty(InputFile))
            {
                Console.WriteLine("Input file is not set.");
                return false;
            }
            if (!File.Exists(InputFile))
            {
                Console.WriteLine("Input file is not exists.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(OutputPath))
                OutputPath = Utils.GetOutputFolder(InputFile);
            else if (!Directory.Exists(OutputPath))
            {
                try
                {
                    Directory.CreateDirectory(OutputPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Can't create output folder.{0}", ex.Message);
                    return false;
                }
            }
            else if (Directory.EnumerateFileSystemEntries(OutputPath).Any())
            {
                Console.WriteLine("output folder is not empty.");
                return false;
            }
            return true;
        }
    }

    public class Border
    {
        public Color color { get; set; } = Color.Transparent;
        public int Width { get; set; } = 0;
        public int Padding { get; set; } = 0;

        public bool HasBorder => Padding > 0 || Width > 0;

        public int EdgeWidth => Width + Padding;
    }

    public class VobSubFourColors
    {
        public Color backgroud { get; set; } = Color.Transparent;
        public Color pattern { get; set; } = Color.Transparent;
        public Color emphasis1 { get; set; } = Color.Transparent;
        public Color emphasis2 { get; set; } = Color.Transparent;
    }
}
