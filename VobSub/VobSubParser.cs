using SubtitleParser.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SubtitleParser.VobSub
{
    public class VobSubParser
    {
        AppSettings _settings = null;
        private Dictionary<int, List<DisplaySet>> _dispSetsList = new Dictionary<int, List<DisplaySet>>();

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

            //StreamWriter sw = new StreamWriter("C:\\Temp\\org.txt", false);
            //pesList.ForEach(pes => sw.WriteLine(pes));
            //sw.Close();
            MergePESPack(pesList);
            //sw = new StreamWriter("C:\\Temp\\merged.txt", false);
            //pesList.ForEach(pes => sw.WriteLine(pes));
            //sw.Close();
            //MergeVobSubPicUnits(pesList);

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
            //var pts = new TimeSpan();
            //var ms = new MemoryStream();

            //float ticksPerMillisecond = 90.000F;
            //if (!_settings.IsPAL)
            //{
            //    ticksPerMillisecond = 90.090F * (23.976F / 24F);
            //}

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

            //foreach (var p in pesList)
            //{
            //    if (p.HasPTSInfo)
            //    {
            //        if (ms.Length > 0)
            //        {
            //            if (p.StreamNum.HasValue &&
            //                !_dispSetsList.ContainsKey(p.StreamNum.Value))
            //            {
            //                lst = new List<VobSubDispSet>();
            //                _dispSetsList.Add(p.StreamNum.Value, lst);
            //            }
            //            else
            //            {
            //                lst = _dispSetsList[p.StreamNum.Value];
            //            }
            //            lst.Add(new VobSubDispSet(ms.ToArray(), pts));
            //        }

            //        ms.Close();
            //        ms = new MemoryStream();
            //        pts = TimeSpan.FromMilliseconds(Convert.ToDouble(p.DST / ticksPerMillisecond)); //90000F * 1000)); (PAL)
            //    }
            //    p.WriteToStream(ms);
            //}
            //if (ms.Length > 0)
            //{
            //    lst.Add(new VobSubDispSet(ms.ToArray(), pts));
            //}

            //ms.Close();

            //// Remove any bad packs
            //foreach (var list in _dispSetsList.Values)
            //{
            //    for (int i = list.Count - 1; i >= 0; i--)
            //    {
            //        VobSubDispSet dispSet = list[i];
            //        if (dispSet.SubPicture == null ||
            //            dispSet.SubPicture.ImageDisplayArea.Width <= 3 ||
            //            dispSet.SubPicture.ImageDisplayArea.Height <= 2)
            //        {
            //            list.RemoveAt(i);
            //        }
            //        else if (dispSet.EndTime.TotalSeconds - dispSet.StartTime.TotalSeconds < 0.1
            //            && dispSet.SubPicture.ImageDisplayArea.Width <= 10
            //            && dispSet.SubPicture.ImageDisplayArea.Height <= 10)
            //        {
            //            list.RemoveAt(i);
            //        }
            //    }
            //}

            //// Fix subs with no duration(completely normal) or negative duration or duration > 10 seconds
            //foreach (var list in _dispSetsList.Values)
            //{
            //    for (int i = 0; i < list.Count; i++)
            //    {
            //        VobSubDispSet pack = list[i];
            //        if (pack.SubPicture.Delay.TotalMilliseconds > 0)
            //        {
            //            pack.EndTime = pack.StartTime.Add(pack.SubPicture.Delay);
            //        }

            //        if (pack.EndTime < pack.StartTime || pack.EndTime.TotalMilliseconds - pack.StartTime.TotalMilliseconds > Utils.SubtitleMaximumDisplayMilliseconds)
            //        {
            //            if (i + 1 < list.Count)
            //            {
            //                pack.EndTime = TimeSpan.FromMilliseconds(list[i + 1].StartTime.TotalMilliseconds - Utils.MinimumMillisecondsBetweenLines);
            //                if (pack.EndTime.TotalMilliseconds - pack.StartTime.TotalMilliseconds > Utils.SubtitleMaximumDisplayMilliseconds)
            //                {
            //                    pack.EndTime = TimeSpan.FromMilliseconds(pack.StartTime.TotalMilliseconds + Utils.SubtitleMaximumDisplayMilliseconds);
            //                }
            //            }
            //            else
            //            {
            //                pack.EndTime = TimeSpan.FromMilliseconds(pack.StartTime.TotalMilliseconds + 3000);
            //            }
            //        }
            //    }
            //}
        }

        private SPU ParseToSPU(PES pes)
        {
            SPU spu = new SPU();
            using (BinaryReader br = new BinaryReader(new MemoryStream(pes.Data)))
            {
                var size = br.ReadTwoBytes();
                var dcstqPos = br.ReadTwoBytes();

                br.Goto(dcstqPos);


                while (!br.EOF())
                {
                    var delay = br.ReadTwoBytes();
                    var nextItem = br.ReadTwoBytes();
                }

            }

            return spu;
        }
    }
}
