using System;
using System.IO;

namespace ProxyKit
{
    public static class ReaderUtils
    {
        // ReSharper disable InconsistentNaming
        public static ushort ReadUInt16BE(BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(ReadBytesRequired(binRdr, sizeof(UInt16)), 0);
        }

        public static short ReadInt16BE(BinaryReader binRdr)
        {
            return BitConverter.ToInt16(ReadBytesRequired(binRdr, sizeof(Int16)), 0);
        }

        public static uint ReadUInt32BE(BinaryReader binRdr)
        {
            return BitConverter.ToUInt32(ReadBytesRequired(binRdr, sizeof(UInt32)), 0);
        }

        public static int ReadInt32BE(BinaryReader binRdr)
        {
            return BitConverter.ToInt32(ReadBytesRequired(binRdr, sizeof(Int32)), 0);
        }

        public static byte[] ReadBytesRequired(BinaryReader binRdr, int byteCount)
        {
            byte[] result = binRdr.ReadBytes(byteCount);
            if(result.Length != byteCount)
            {
                throw new EndOfStreamException(
                    string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));
            }
            Array.Reverse(result);
            return result;
        }
    }
}