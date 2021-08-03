using SubtitleParser.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static SubtitleParser.VobSub.SPU;

namespace SubtitleParser.VobSub
{
    public class VobSubParser
    {
        AppSettings _settings = null;
        List<SPU> spuList = new List<SPU>();

        public VobSubParser(AppSettings settings)
        {
            _settings = settings;
        }

        public bool Parse()
        {
            var pesList = new List<PES>();

            using (BinaryReader br = new BinaryReader(new FileStream(_settings.InputFile, FileMode.Open)))
            {
                while (!br.EOF())
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
                    spuList.Add(spu);
                }
            });

            CheckSPU(spuList);

            return true;
        }

        public void Output()
        {
            //    foreach (var pair in _dispSetsList)
            //    {
            //        string path = $"c:\\temp\\{pair.Key}\\";
            //        if (!Directory.Exists(path))
            //        {
            //            Directory.CreateDirectory(path);
            //        }

            //        for (int i = 0; i < pair.Value.Count; i++)
            //        {
            //            var bmp = pair.Value[i].SubPicture.GetBitmap(null, Color.Transparent, Color.Black, Color.Black, Color.Transparent, true);
            //            bmp.Save($"{path}img_{i}.png", ImageFormat.Png);
            //        }
            //    }
        }

        private PES ReadPESPack(BinaryReader br)
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
                StreamNum = pes.StreamNum
            };
            using (BinaryReader br = new BinaryReader(new MemoryStream(pes.Data)))
            {
                var size = br.ReadTwoBytes();
                var dcstqPos = br.ReadTwoBytes();

                br.Goto(dcstqPos);

                bool IsLastItem = false;
                SPDCSQ spdcsq = new SPDCSQ();
                while (!br.EOF() && !IsLastItem)
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
                                break;
                            case (byte)Constants.DCC.ChangeColorAndContrast:
                                var siz = br.ReadTwoBytes();
                                br.Forward(siz);
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

        private void CheckSPU(List<SPU> spuList)
        {

            for (int i = spuList.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < spuList[i].SPDCSQs.Count; j--)
                {
                    var spdcsq = spuList[i].SPDCSQs[j];
                    if (spdcsq.ImgSize.Width <= 3 || spdcsq.ImgSize.Height <= 2)
                    {
                        spuList[i].SPDCSQs.RemoveAt(j);
                    }
                    if (spdcsq.Stop.TotalSeconds - spdcsq.Start.TotalSeconds < 0.1 &&
                        spdcsq.ImgSize.Width <= 10 &&
                        spdcsq.ImgSize.Height <= 10)
                    {
                        spuList[i].SPDCSQs.RemoveAt(j);
                    }
                }

                if (spuList[i].SPDCSQs.Count <= 0)
                {
                    spuList.RemoveAt(i);
                }
            }
        }

        //public FastBitmap GetBitmap(BinaryReader br,SPDCSQ dcsq)
        //{
        //    FastBitmap bitmap = new FastBitmap(dcsq.ImgSize,_settings);

        //    int xPos = 0, yPos = 0;
        //    bool isHalfByte = false;
        //    while (xPos < dcsq.ImgSize.Width && yPos < dcsq.ImgSize.Height)
        //    {
        //        byte b1 = br.ReadByte();
        //        byte b2 = br.ReadByte();

        //        if (isHalfByte)
        //        {
        //            byte b3 = br.ReadByte();
        //            b1 = (byte)(((b1 & 0b00001111) << 4) | ((b2 & 0b11110000) >> 4));
        //            b2 = (byte)(((b2 & 0b00001111) << 4) | ((b3 & 0b11110000) >> 4));
        //        }

        //        if (b1 >> 2 == 0)
        //        {
        //            runLength = (b1 << 6) | (b2 >> 2);
        //            color = b2 & 0b00000011;
        //            if (runLength == 0)
        //            {
        //                // rest of line + skip 4 bits if Only half
        //                restOfLine = true;
        //                if (onlyHalf)
        //                {
        //                    onlyHalf = false;
        //                    return 3;
        //                }
        //            }
        //            return 2;
        //        }

        //        if (b1 >> 4 == 0)
        //        {
        //            runLength = (b1 << 2) | (b2 >> 6);
        //            color = (b2 & 0b00110000) >> 4;
        //            if (onlyHalf)
        //            {
        //                onlyHalf = false;
        //                return 2;
        //            }
        //            onlyHalf = true;
        //            return 1;
        //        }

        //        if (b1 >> 6 == 0)
        //        {
        //            runLength = b1 >> 2;
        //            color = b1 & 0b00000011;
        //            return 1;
        //        }

        //        runLength = b1 >> 6;
        //        color = (b1 & 0b00110000) >> 4;

        //        if (onlyHalf)
        //        {
        //            onlyHalf = false;
        //            return 1;
        //        }
        //        onlyHalf = true;
        //        return 0;
        //    }

            
        //}

        private 

    }
}
