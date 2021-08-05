using System;
using System.IO;

namespace SubtitleParser.Common
{
    public class HalfByteBinaryReader : BinaryReader
    {
        private byte? halfByte = null;

        public HalfByteBinaryReader(byte[] data) : base(new MemoryStream(data))
        {
        }

        public byte ReadFourBit()
        {
            if (halfByte.HasValue)
            {
                var ret = halfByte.Value;
                halfByte = null;
                return ret;
            }
            else
            {
                byte b = ReadByte();
                var ret = (byte)((b & 0b11110000) >> 4);
                halfByte = (byte)(b & 0b00001111);
                return ret;
            }
        }

        public byte ReadByte(bool checkHalfByte)
        {
            if (!checkHalfByte)
            {
                return base.ReadByte();
            }
            else
            {
                byte b = ReadByte();

                if (halfByte.HasValue)
                {
                    var ret = (byte)((halfByte.Value << 4) | ((b & 0b11110000) >> 4));
                    halfByte = (byte)(b & 0b00001111);
                    return ret;
                }
                else
                {
                    return b;
                }
            }
        }

        public void ResetHalfByte()
        {
            if ((halfByte ?? 0) != 0)
            {
                Console.WriteLine("skip bit is not zero");
            }
            halfByte = null;
        }

        public bool HasHalfByte => halfByte != null;
    }
}
