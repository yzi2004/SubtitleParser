using SubtitleParser.Sup;
using SubtitleParser.VobSub;
using System;

namespace SubtitleParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                if (args[0] == "-v" || args[0] == "--version")
                {
                    PrintVer();
                }
                else if (args[0] == "-h" || args[0] == "--help")
                {
                    PrintUsage();
                }
                else
                {
                    AppSettings settings = new AppSettings(args);
                    if (!settings.IsValid)
                    {
                        return;
                    }
                    if (settings.IsSup)
                    {
                        var parser = new SupParser(settings);
                        if (parser.Parse())
                        {
                            parser.Output();
                        }
                    }
                    else
                    {
                        var parser = new VobSubParser(settings);
                        if (parser.Parse())
                        {
                            parser.Output();
                        }
                    }
                }
            }
            else
            {
                PrintUsage();
            }
        }

        private static void PrintUsage()
        {
            PrintVer();
            Console.WriteLine("Usage: " + Utils.GetAppName() + " -i|--input input_file --fps pal|ntsc  -o|-out <path> -f|-format {bmp|jpeg|gif|tiff|png}");
            Console.WriteLine("  -i,--input\t: subtitle file for parse (*.sup|*.sub) ");
            Console.WriteLine("  -o,--out\t: folder path for time line and images file to save.");
            Console.WriteLine("  -f,--format\t: image format. (default:jpeg)");
            Console.WriteLine("  --fps\t: vobsub(*.sub) frames per second(pal/ntsc). (default: pal) ");
            Console.WriteLine();
        }

        private static void PrintVer()
        {
            Version appVer = Utils.GetAppVer();
            DateTime dateTime = new DateTime(2000, 1, 1).AddDays(appVer.Build);
            Console.WriteLine(string.Format("{0} {1}.{2}-{3}\r\n", Utils.GetAppName(), appVer.Major, appVer.Minor, dateTime.ToString("yyyyMMdd")));
        }
    }
}
