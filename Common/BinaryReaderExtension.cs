using System.IO;
using System;

namespace SubtitleParser.Common
{
    public static class BinaryReaderExtension
    {
        public static ushort ReadTwoBytes(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(2);

            if (buffer.Length < 2)
            {
                return 0;
            }

            return (ushort)((buffer[0] << 8) + buffer[1]);
        }

        public static uint ReadThreeBytes(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(3);

            if (buffer.Length < 3)
            {
                return 0;
            }

            return (uint)((buffer[0] << 16) + (buffer[1] << 8) + buffer[2]);
        }

        public static uint ReadFourBytes(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(4);

            if (buffer.Length < 4)
            {
                return 0;
            }

            return (uint)((buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + (buffer[3]));
        }

        public static bool EOF(this BinaryReader binaryReader)
        {
            var bs = binaryReader.BaseStream;
            return (bs.Position == bs.Length);
        }

        public static bool Back(this BinaryReader binaryReader, int Count)
        {
            if (!binaryReader.BaseStream.CanSeek)
            {
                return false;
            }

            if (binaryReader.BaseStream.Position <= Count)
            {
                binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                binaryReader.BaseStream.Seek(Count * -1, SeekOrigin.Current);
            }

            return true;
        }

        public static bool Forward(this BinaryReader binaryReader, int Count)
        {
            if (!binaryReader.BaseStream.CanSeek)
            {
                return false;
            }

            if (binaryReader.BaseStream.Position + Count >= binaryReader.BaseStream.Length)
            {
                binaryReader.BaseStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                binaryReader.BaseStream.Seek(Count, SeekOrigin.Current);
            }
            return true;
        }

        public static bool Goto(this BinaryReader binaryReader, long pos)
        {
            if (!binaryReader.BaseStream.CanSeek)
            {
                return false;
            }
            if (pos > binaryReader.BaseStream.Length)
            {
                pos = binaryReader.BaseStream.Length;
            }
            else if (pos < 0)
            {
                pos = 0;
            }

            binaryReader.BaseStream.Seek(pos, SeekOrigin.Begin);
            return true;
        }
        
        public static long GetPos(this BinaryReader binaryReader)
        {
            if (!binaryReader.BaseStream.CanSeek)
            {
                throw new NotSupportedException("the stream is not support seek");
            }

            return binaryReader.BaseStream.Position;
        }
    }
}
