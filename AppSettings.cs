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

        public Vobsub vobsub { get; set; } = new Vobsub();
        public Sup sup { get; set; } = new Sup();
        public Image image { get; set; } = new Image();

        public string GetImgExt()
        {
            return image.ImageFormat.ToString().ToLower().Replace("jpeg", "jpg");
        }

        public bool IsSup => InputFile.ToLower().EndsWith(".sup");
        public bool IsValid => SelfCheck();

        public AppSettings(string[] pars)
        {
            IConfigurationRoot configurationRoot = InitConfig(pars);

            InputFile = configurationRoot["input"];
            OutputPath = configurationRoot["output"];
            string fmt = configurationRoot["format"];

            string val = configurationRoot["fps"];
            if ((val ?? "") == "pal")
            {
                vobsub.fps = FPS.PAL;
            }
            else
            {
                vobsub.fps = FPS.NTSC;
            }

            var section = configurationRoot.GetSection("image");

            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(fmt))
                {
                    fmt = section["format"];
                }
                section = section.GetSection("border");
                if (section != null)
                {
                    val = section["width"];
                    if (!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out var result))
                    {
                        image.Border.Width = result;
                    }

                    val = section["padding"];
                    if (!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out result))
                    {
                        image.Border.Padding = result;
                    }

                    val = section["color"];
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        image.Border.BorderColor = Utils.TryParseColor(val, Color.Transparent);
                    }
                }
            }

            image.ImageFormat = Utils.ConvImgFmt(fmt);


            section = configurationRoot.GetSection("vobsub");
            if (section != null)
            {
                val = section["use_custom_color"];
                if ((val ?? "").ToLower() == "false")
                {
                    vobsub.UseCustomColors = false;
                }

                section = section.GetSection("custom_color");
                if (section != null)
                {
                    val = section["background"];
                    vobsub.CustomColors.Background = Utils.TryParseColor(val, Color.Transparent);

                    val = section["pattern"];
                    vobsub.CustomColors.Pattern = Utils.TryParseColor(val, Color.Transparent);

                    val = section["emphasis1"];
                    vobsub.CustomColors.Emphasis1 = Utils.TryParseColor(val, Color.Transparent);

                    val = section["emphasis2"];
                    vobsub.CustomColors.Emphasis2 = Utils.TryParseColor(val, Color.Transparent);
                }
            }

            section = configurationRoot.GetSection("sup");
            if (section != null)
            {
                val = section["background"];
                sup.Background = Utils.TryParseColor(val, Color.Transparent);

                val = section["convert_color_for_OCR"];
                if (!string.IsNullOrWhiteSpace(val) && val.ToLower() != "false")
                {
                    sup.ConvColorForOCR = true;
                }
            }
        }

        private IConfigurationRoot InitConfig(string[] args)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>()
                {{"-i","input"},{"--input","input"},
                 {"-o","output"},{"--output","output"},
                { "--fps","fps"},
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

        public class Image
        {
            public Border Border { get; set; } = new Border();
            public ImageFormat ImageFormat { get; set; } = ImageFormat.Jpeg;
        }

        public class Border
        {
            public Color BorderColor { get; set; } = Color.Transparent;
            public int Width { get; set; } = 0;
            public int Padding { get; set; } = 0;
        }

        public class Vobsub
        {
            public FourColors CustomColors { get; set; } = new FourColors();
            public FPS fps { get; set; } = FPS.PAL;
            public bool UseCustomColors { get; set; } = true;
        }

        public class FourColors
        {
            public Color Background { get; set; } = Color.Transparent;
            public Color Pattern { get; set; } = Color.Transparent;
            public Color Emphasis1 { get; set; } = Color.Transparent;
            public Color Emphasis2 { get; set; } = Color.Transparent;
        }

        public class Sup
        {
            public Color Background { get; set; } = Color.Transparent;
            public bool ConvColorForOCR { get; set; }
        }
    }
}
