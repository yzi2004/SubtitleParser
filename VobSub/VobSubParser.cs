using SubtitleParser.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// http://www.mpucoder.com/DVD/pes-hdr.html
/// </summary>

namespace SubtitleParser.VobSub
{
    public class VobSubParser
    {
        AppSettings _settings = null;
        List<SPU> _spuList = new List<SPU>();
        List<Color> _palettes = new List<Color>();

        public VobSubParser(AppSettings settings)
        {
            _settings = settings;
        }

        public bool Parse()
        {
            LoadPalettesFromIdx();

            var pesList = new List<PES>();

            using (ParseUseBinaryReader br = new ParseUseBinaryReader(new FileStream(_settings.InputFile, FileMode.Open)))
            {
                while (!br.EOF)
                {
                    var pes = ReadPESPack(br);
                    if (pes != null)
                    {
                        pesList.Add(pes);
                    }
                }
            }

            MergePESPack(pesList);

            pesList.ForEach(pes =>
            {
                var spu = ParseToSPU(pes);
                if (spu != null)
                {
                    _spuList.Add(spu);
                }
            });

            return true;
        }

        public void Output()
        {
            var streamNums = _spuList.Where(spu => spu.StreamNum.HasValue).Select(spu => spu.StreamNum.Value).Distinct();

            foreach (var streamNum in streamNums)
            {
                string workingDir = _settings.OutputPath;

                if (streamNums.Count() > 1)
                {
                    workingDir = Utils.EnsureDir($"{workingDir}\\{streamNum}");
                }

                string imgFolder = Utils.EnsureDir($"{workingDir}\\img");

                StreamWriter sw = new StreamWriter($"{workingDir}\\timeline.srt");
                int idx = 1;

                _spuList.ForEach(spu =>
                {

                    if (spu.StreamNum.HasValue && spu.StreamNum == streamNum)
                    {
                        spu.SPDCSQs.ForEach(dcsq =>
                        {
                            Console.Write("@");
                            string file = $"{idx:0000}.{_settings.GetImgExt()}";
                            var bmp = ImgDecode(spu, dcsq);
                            bmp.Save($"{imgFolder}\\{file}", _settings.image.ImageFormat);
                            sw.WriteLine(idx++);
                            var startTime = spu.StartTime + dcsq.Start;
                            var stopTime = spu.StartTime + dcsq.Stop;
                            sw.WriteLine($"{startTime:hh\\:mm\\:ss\\,fff} --> {stopTime:hh\\:mm\\:ss\\,fff}");
                            sw.WriteLine(file);
                            sw.WriteLine();
                        });
                    }
                });
            }
        }

        private PES ReadPESPack(ParseUseBinaryReader br)
        {
            PES pes = new PES();
            var startPos = br.GetPos();
            var header = br.ReadThreeBytes();
            var packID = br.ReadByte();
            if (header == 1 && packID == 0xba) //Is a Mpeg-2 Pack Header
            {
                //skip mpeg-2 pack header
                br.Forward(Constants.Mpeg2PackHeaderLength - 4);
            }
            else
            {
                //back to origin position.
                br.Back(4);
            }

            //skip start code
            br.Forward(3);

            var streamId = br.ReadByte();
            var pesLength = br.ReadTwoBytes();
            //skip mpeg-2 extension first byte; 
            br.Forward(1);

            var ptsdtsFlag = br.ReadByte() >> 6;

            var headerDataLength = br.ReadByte();

            var pos = br.GetPos();

            if (ptsdtsFlag == 0b00000010 || ptsdtsFlag == 0b00000011)
            {
                pes.DST = (ulong)(br.ReadByte() & 0b00001110) << 29;
                pes.DST += (ulong)br.ReadByte() << 22;
                pes.DST += (ulong)(br.ReadByte() & 0b11111110) << 14;
                pes.DST += (ulong)br.ReadByte() << 7;
                pes.DST += (ulong)br.ReadByte() >> 1;
            }

            br.Goto(pos + headerDataLength);
            var payloadSize = pesLength - (Constants.MPEG2ExtLength + headerDataLength);

            if (streamId == 0xBD)
            {
                //In the case of private streams the first byte of the payload is the sub-stream number.
                int id = br.ReadByte();
                if (id >= 0x20 && id <= 0x40) // x3f 0r x40 ?
                {
                    pes.StreamNum = id;
                }
                payloadSize--;
            }

            pos = br.GetPos();

            if (payloadSize + pos - startPos > Constants.PESPackSize) // to fix bad subs...
            {
                payloadSize = Constants.PESPackSize - (int)pos + (int)startPos;
                if (payloadSize < 0)
                {
                    return null;
                }
            }

            pes.Data = br.ReadBytes(payloadSize);

            br.Goto(startPos + Constants.PESPackSize);

            return pes;
        }

        private void MergePESPack(List<PES> pesList)
        {
            for (int idx = pesList.Count - 1; idx > 0; idx--)
            {
                var p = pesList[idx];

                if (!p.HasPTSInfo)
                {
                    for (var mIdx = idx - 1; mIdx >= 0; mIdx--)
                    {
                        if (p.StreamNum == pesList[mIdx].StreamNum)
                        {
                            pesList[mIdx].Data = pesList[mIdx].Data.Concat(p.Data).ToArray();
                            pesList.RemoveAt(idx);
                            break;
                        }
                    }
                }
            }
        }

        private SPU ParseToSPU(PES pes)
        {
            SPU spu = new SPU()
            {
                Data = pes.Data,
                StreamNum = pes.StreamNum,
                StartTime = Utils.Ticks2TimeSpan((uint)pes.DST.Value, _settings.vobsub.fps)
            };

            using (ParseUseBinaryReader br = new ParseUseBinaryReader(pes.Data))
            {
                var size = br.ReadTwoBytes();
                var dcstqPos = br.ReadTwoBytes();

                br.Goto(dcstqPos);

                bool IsLastItem = false;
                SPDCSQ spdcsq = new SPDCSQ();
                while (!br.EOF && !IsLastItem)
                {
                    var currPos = br.GetPos();

                    var delay = br.ReadTwoBytes();
                    var nextItem = br.ReadTwoBytes();
                    if (nextItem == currPos)
                    {
                        IsLastItem = true;
                    }

                    var cmd = br.ReadByte();

                    while (cmd != (byte)Constants.DCC.End)
                    {
                        switch (cmd)
                        {
                            case (byte)Constants.DCC.StartDisplay:
                                spdcsq.Start = TimeSpan.FromMilliseconds((delay << 10) / 90.0);
                                break;
                            case (byte)Constants.DCC.StopDisplay:
                                spdcsq.Stop = TimeSpan.FromMilliseconds((delay << 10) / 90.0);
                                spu.SPDCSQs.Add(spdcsq);
                                spdcsq = new SPDCSQ();
                                break;
                            case (byte)Constants.DCC.SetColor:
                                var buf = br.ReadBytes(2);
                                if (spdcsq.fourColor == null)
                                {
                                    spdcsq.fourColor = new FourColor();
                                }
                                spdcsq.fourColor.Emphasis2 = (byte)((buf[0] & 0b11110000) >> 4);
                                spdcsq.fourColor.Emphasis1 = (byte)(buf[0] & 0b00001111);
                                spdcsq.fourColor.Pattern = (byte)((buf[1] & 0b11110000) >> 4);
                                spdcsq.fourColor.Background = (byte)(buf[1] & 0b00001111);

                                break;
                            case (byte)Constants.DCC.SetDisplayArea:
                                buf = br.ReadBytes(6);

                                int startingX = (buf[0] << 8 | buf[1]) >> 4;
                                int endingX = (buf[1] & 0b00001111) << 8 | buf[2];
                                int startingY = (buf[3] << 8 | buf[4]) >> 4;
                                int endingY = (buf[4] & 0b00001111) << 8 | buf[5];
                                spdcsq.ImgSize = new Size(endingX - startingX, endingY - startingY);
                                break;
                            case (byte)Constants.DCC.SetPixelDataAddress:
                                spdcsq.PXDtfOffset = br.ReadTwoBytes();
                                spdcsq.PXDbfOffset = br.ReadTwoBytes();
                                break;
                            case (byte)Constants.DCC.SetContrast:
                                buf = br.ReadBytes(2);
                                if (spdcsq.fourColor == null)
                                {
                                    spdcsq.fourColor = new FourColor();
                                }
                                spdcsq.fourColor.ContrEm2 = (byte)((buf[0] & 0b11110000) >> 4);
                                spdcsq.fourColor.ContrEm1 = (byte)(buf[0] & 0b00001111);
                                spdcsq.fourColor.ContrPtn = (byte)((buf[1] & 0b11110000) >> 4);
                                spdcsq.fourColor.ContrBG = (byte)(buf[1] & 0b00001111);

                                break;
                            case (byte)Constants.DCC.ChangeColorAndContrast:
                                var lng = br.ReadTwoBytes();
                                br.Forward(lng);
                                break;
                            default:
                                break;

                        }
                        cmd = br.ReadByte();
                    }
                }
            }

            return spu;
        }

        private Bitmap ImgDecode(SPU spu, SPDCSQ dcsq)
        {
            FastBitmap fastBitmap = new FastBitmap(dcsq.ImgSize.Width + 1, dcsq.ImgSize.Height + 1);
            var br = new ParseUseBinaryReader(spu.Data);

            br.Goto(dcsq.PXDtfOffset);
            drawLines(br, fastBitmap, 0, dcsq);
            br.Goto(dcsq.PXDbfOffset);
            drawLines(br, fastBitmap, 1, dcsq);

            var color = GetColor(dcsq, 0);
            fastBitmap.Makeup(color,_settings.image.Border);

            return fastBitmap.GetBitmap();
        }

        private void drawLines(ParseUseBinaryReader br, FastBitmap fastBitmap, int yStartPos, SPDCSQ dcsq)
        {
            int xPos = 0, yPos = yStartPos;

            while (yPos < (dcsq.ImgSize.Height + 1) && !br.EOF)
            {
                var clrLen = GetColorRunLength(br);

                if (clrLen.colorIdx == 0xff)
                {
                    break;
                }

                var color = GetColor(dcsq, clrLen.colorIdx);

                if (clrLen.runLength == 0)
                {
                    for (; ; )
                    {
                        fastBitmap.SetPixel(xPos++, yPos, color);

                        if (xPos >= dcsq.ImgSize.Width + 1)
                        {
                            break;
                        }
                    }
                    xPos = 0;
                    yPos += 2;
                    br.ResetHalfByte();
                }
                else
                {
                    for (int i = 0; i < clrLen.runLength; i++)
                    {
                        fastBitmap.SetPixel(xPos++, yPos, color);

                        if (xPos >= dcsq.ImgSize.Width + 1)
                        {
                            xPos = 0;
                            yPos += 2;
                            br.ResetHalfByte();
                            break;
                        }
                    }
                }
            }
        }

        private (byte colorIdx, byte runLength) GetColorRunLength(ParseUseBinaryReader br)
        {
            byte runLength, colorIdx;

            //load first 4 bit
            uint buf = br.ReadFourBit();
            if ((buf & 0b00001100) != 0)
            {
                //1-3	4	n n c c
                runLength = (byte)(buf >> 2);
                colorIdx = (byte)(buf & 0b0011);

                return (colorIdx, runLength);
            }
            //load next 4 bit
            buf = (byte)((buf << 4) | br.ReadFourBit());

            //4-15	8	0 0 n n n n c c
            if ((buf & 0b00110000) != 0)
            {
                runLength = (byte)(buf >> 2);
                colorIdx = (byte)(buf & 0b00000011);
                return (colorIdx, runLength);
            }

            //16-63	12	0 0 0 0 n n n n n n c c
            if ((buf & 0b00001100) != 0)
            {
                buf = (byte)((buf << 4) | br.ReadFourBit());

                runLength = (byte)(buf >> 2);
                colorIdx = (byte)(buf & 0b00000011);
                return (colorIdx, runLength);
            }

            //64-255	16	0 0 0 0 0 0 n n n n n n n n c c
            buf = buf << 8 | br.ReadByte(true);
            runLength = (byte)(buf >> 2);
            colorIdx = (byte)(buf & 0b0000000011);

            return (colorIdx, runLength);
        }

        private Color GetColor(SPDCSQ dcsq, int colorIdx)
        {
            switch (colorIdx)
            {
                case 0:
                    return GetColor(dcsq.fourColor?.Background, dcsq.fourColor?.ContrBG, _settings.vobsub.CustomColors.Background);
                case 1:
                    return GetColor(dcsq.fourColor?.Pattern, dcsq.fourColor?.ContrPtn, _settings.vobsub.CustomColors.Pattern);
                case 2:
                    return GetColor(dcsq.fourColor?.Emphasis1, dcsq.fourColor?.ContrEm1, _settings.vobsub.CustomColors.Emphasis1);
                case 3:
                    return GetColor(dcsq.fourColor?.Emphasis2, dcsq.fourColor?.ContrEm2, _settings.vobsub.CustomColors.Emphasis2);
                default:
                    return Color.Transparent;
            }

        }

        private Color GetColor(int? palette, int? contr, Color customColor)
        {
            if (_settings.vobsub.UseCustomColors)
            {
                return customColor;
            }

            if ((palette ?? -1) >= 0 && (palette ?? -1) < (_palettes?.Count ?? -1))
            {
                var color = _palettes[palette.Value];
                if (contr.HasValue)
                {
                    if (contr.Value >= 0 && contr.Value <= 15)
                    {
                        return Color.FromArgb(contr.Value * 17, color);
                    }
                }
                return color;
            }

            return customColor;
        }

        private void LoadPalettesFromIdx()
        {
            string idxFile = _settings.InputFile.ToLower().Replace(".sub", ".idx");
            if (!File.Exists(idxFile))
            {
                return;
            }

            using (StreamReader sr = new StreamReader(idxFile, Encoding.Default))
            {
                while (!sr.EndOfStream)
                {
                    string str = sr.ReadLine();
                    if (str.StartsWith("palette:"))
                    {
                        str = str.Substring("palette:".Length);

                        var colors = str.Replace(" ", "").Split(',');

                        foreach (var clr in colors)
                        {
                            _palettes.Add(Utils.TryParseColor(clr, Color.Black));
                        }
                    }
                }
                sr.Close();
            }
        }
    }
}
