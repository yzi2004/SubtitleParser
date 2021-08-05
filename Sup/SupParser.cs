using SubtitleParser.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
namespace SubtitleParser.Sup
{
    public class SupParser
    {
        AppSettings _settings = null;
        List<DisplaySet> _displaySets = new List<DisplaySet>();

        public SupParser(AppSettings settings)
        {
            _settings = settings;
        }

        public bool Parse()
        {
            try
            {
                using (BinaryReader br = new BinaryReader(new FileStream(_settings.InputFile, FileMode.Open)))
                {
                    DisplaySet set = new DisplaySet();
                    while (!br.EOF())
                    {
                        var segment = Read(br);
                        if (segment is PCSData)
                        {
                            if (set.PCSData != null)
                            {
                                throw new Exception();
                            }
                            set.PCSData = (PCSData)segment;
                        }
                        else if (segment is WDSData)
                        {
                            set.WDSDatas.Add((WDSData)segment);
                        }
                        else if (segment is PDSData)
                        {
                            set.PDSDatas.Add((PDSData)segment);
                        }
                        else if (segment is ODSData)
                        {
                            set.ODSDatas.Add((ODSData)segment);
                        }
                        else if (segment is ENDData)
                        {
                            _displaySets.Add(set);
                            set = new DisplaySet();
                        }
                        else
                        {
                            set = new DisplaySet();
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Output()
        {
            StreamWriter sw = new StreamWriter(_settings.OutputPath + "\\timeline.srt");
            StringBuilder sbImgs = new StringBuilder();

            int dsIdx = 0;
            int objIdx = 0;

            DisplaySet startDS = null;
            _displaySets.ForEach(ds =>
            {
                if (ds.PCSData.CompState == CompState.Normal)
                {
                    if (ds == null)
                    {
                        return;
                    }
                    sw.WriteLine(dsIdx);
                    sw.WriteLine($"{startDS.PCSData.PTS:HH:mm:ss,fff} --> {ds.PCSData.PTS:HH:mm:ss,fff}");
                    sw.WriteLine(sbImgs.ToString());
                    sbImgs = new StringBuilder();
                }
                else
                {
                    startDS = ds;
                    Console.Write("*");
                    PDSData ptn = ds.PDSDatas.Where(t => t.PaletteID == ds.PCSData.PaletteID).FirstOrDefault();
                    if (ptn == null && ds.PDSDatas.Count > 0)
                    {
                        ptn = ds.PDSDatas[0];
                    }
                    ++dsIdx;
                    objIdx = 0;

                    var imgFolder = Utils.EnsureDir($"{_settings.OutputPath}\\img");

                    ds.PCSData.PCSObjects.ForEach(pcsObj =>
                    {
                        ODSData odsData = ds.ODSDatas.Where(t => t.ObjectID == pcsObj.ObjectID).FirstOrDefault();
                        if (odsData == null)
                        {
                            return;
                        }
                        string file = string.Format("{0}_{1}.{2}", dsIdx, ++objIdx, _settings.GetImgExt());
                        sbImgs.AppendLine(file);
                        var bmp = ImgDecode(odsData.ObjectData, odsData.ImageSize, ptn.EntryObjects);
                        bmp.Save($"{imgFolder}\\{file}", _settings.image.ImageFormat);
                    });
                }
            });
            sw.Close();
        }

        private Segment Read(BinaryReader binaryReader)
        {
            if (!CheckMagicNumber(binaryReader, false))
            {
                return null;
            }

            var heaser = ReadSegHeader(binaryReader);

            switch (heaser.Type)
            {

                case SegType.PCS:
                    return ReadPCSData(binaryReader, heaser);
                case SegType.WDS:
                    return ReadWDSData(binaryReader, heaser);
                case SegType.PDS:
                    return ReadPDSData(binaryReader, heaser);
                case SegType.ODS:
                    return ReadODSData(binaryReader, heaser);
                case SegType.END:
                    return new ENDData(heaser);
                default:
                    return heaser;
            }
        }

        private Segment ReadSegHeader(BinaryReader binaryReader)
        {
            Segment seg = new Segment();
            seg.PTS = Utils.Ticks2TimeSpan(binaryReader.ReadFourBytes());
            seg.DTS = Utils.Ticks2TimeSpan(binaryReader.ReadFourBytes());
            seg.Type = (SegType)(binaryReader.ReadByte());
            seg.DataSize = binaryReader.ReadTwoBytes();

            return seg;
        }

        private PCSData ReadPCSData(BinaryReader binaryReader, Segment header)
        {
            PCSData data = new PCSData(header);
            data.Size = new Size(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());
            data.FrameRate = binaryReader.ReadByte();
            data.CompNum = binaryReader.ReadTwoBytes();
            data.CompState = (CompState)(binaryReader.ReadByte());
            data.PaletteUpdFlag = binaryReader.ReadByte();
            data.PaletteID = binaryReader.ReadByte();
            data.CompObjCount = binaryReader.ReadByte();

            if (data.CompObjCount > 0)
            {
                data.PCSObjects = new List<PCSObject>();
                for (int i = 0; i < data.CompObjCount; i++)
                {
                    PCSObject compObject = new PCSObject();
                    compObject.ObjectID = binaryReader.ReadTwoBytes();
                    compObject.WindowID = binaryReader.ReadByte();
                    compObject.ObjCroppedFlag = (binaryReader.ReadByte() == 0x40);
                    compObject.Origin = new Point(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());
                    if (compObject.ObjCroppedFlag)
                    {
                        compObject.CropOrigin = new Point(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());
                        compObject.CropSize = new Size(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());
                    }

                    data.PCSObjects.Add(compObject);
                }
            }

            return data;
        }

        private WDSData ReadWDSData(BinaryReader binaryReader, Segment header)
        {
            WDSData data = new WDSData(header);

            data.ObjectCount = binaryReader.ReadByte();

            if (data.ObjectCount > 0)
            {
                data.WDSObjects = new List<WDSObject>();

                for (int i = 0; i < data.ObjectCount; i++)
                {
                    WDSObject obj = new WDSObject();

                    obj.WindowsID = binaryReader.ReadByte();
                    obj.Origin = new Point(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());
                    obj.Size = new Size(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());

                    data.WDSObjects.Add(obj);
                }
            }
            return data;
        }

        private PDSData ReadPDSData(BinaryReader binaryReader, Segment header)
        {
            PDSData data = new PDSData(header);
            data.PaletteID = binaryReader.ReadByte();
            data.PaletteVer = binaryReader.ReadByte();
            int entryCount = (header.DataSize - 2) / 5;
            if (entryCount > 0)
            {
                data.EntryObjects = new List<PDSEntryObject>();
                for (int i = 0; i < entryCount; i++)
                {
                    PDSEntryObject obj = new PDSEntryObject();
                    obj.PaletteEntryID = binaryReader.ReadByte();
                    obj.Luminance = binaryReader.ReadByte();
                    obj.ColorDifferenceRed = binaryReader.ReadByte();
                    obj.ColorDifferenceBlue = binaryReader.ReadByte();
                    obj.Transparency = binaryReader.ReadByte();

                    data.EntryObjects.Add(obj);
                }
            }
            return data;
        }

        private ODSData ReadODSData(BinaryReader binaryReader, Segment header)
        {
            ODSData data = new ODSData();
            data.ObjectID = binaryReader.ReadTwoBytes();
            data.ObjectVer = binaryReader.ReadByte();
            data.SeqFlag = binaryReader.ReadByte();
            data.ObjDataLength = binaryReader.ReadThreeBytes();
            data.ImageSize = new Size(binaryReader.ReadTwoBytes(), binaryReader.ReadTwoBytes());
            data.ObjectData = binaryReader.ReadBytes((int)data.ObjDataLength - 4); //4 is Image Size bytes

            return data;
        }

        private bool CheckMagicNumber(BinaryReader binaryReader, bool BackIfFalse = false)
        {
            var bytes = binaryReader.ReadBytes(2);
            if (bytes.Length < 2)
            {
                if (BackIfFalse)
                {
                    binaryReader.Back(bytes.Length);
                }

                return false;
            }

            if (bytes[0] == 0x50 && bytes[1] == 0x47)
            {
                return true;
            }
            else
            {
                if (BackIfFalse)
                {
                    binaryReader.Back(2);
                }
                return false;
            }
        }

        private Bitmap ImgDecode(byte[] data, Size size, List<PDSEntryObject> palettes)
        {
            int pos = 0;
            FastBitmap fastBitmap = new FastBitmap(size);
            int xpos = 0; int ypos = 0;

            for (; ; )
            {
                if (pos >= data.Length)
                {
                    break;
                }
                var ColorTimes = GetColorTimes(data, pos);

                if (ColorTimes.colorIdx == 0 && ColorTimes.times == 0)
                {
                    continue;
                }
                else
                {
                    Color color;
                    if (ColorTimes.colorIdx == 0xff)
                    {
                        color = _settings.sup.Background;
                    }
                    else
                    {
                        var rgb = Utils.YCbCr2Rgb(palettes[ColorTimes.colorIdx].Luminance,
                                palettes[ColorTimes.colorIdx].ColorDifferenceRed,
                                palettes[ColorTimes.colorIdx].ColorDifferenceBlue);

                        if (palettes[ColorTimes.colorIdx].Transparency != 255)
                        {
                            color = _settings.sup.Background;
                        }
                        else
                        {
                            color = Color.FromArgb(0xff, (int)rgb.r, (int)rgb.g, (int)rgb.b);
                        }
                    }

                    for (int j = 0; j < ColorTimes.times; j++)
                    {
                        fastBitmap.SetPixel(xpos++, ypos, color);
                        if (xpos >= size.Width)
                        {
                            ypos++;
                            xpos = 0;
                            break;
                        }
                    }
                }
            }

            if (_settings.image.Border.Padding > 0)
            {
                fastBitmap.AddMargin(_settings.image.Border.Padding, _settings.sup.Background);
            }
            if (_settings.image.Border.Width > 0)
            {
                fastBitmap.AddMargin(_settings.image.Border.Width, _settings.image.Border.BorderColor);
            }

            return fastBitmap.GetBitmap();
        }

        private (int colorIdx, ushort times) GetColorTimes(byte[] data, int pos)
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
    }
}
