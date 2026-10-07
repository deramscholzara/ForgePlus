using System.Collections.Generic;

namespace ForgePlus.DataFileIO
{
    internal static class BigEndian
    {
        public static int Read16(byte[] bytes, int offset)
        {
            return (bytes[offset] << 8) | bytes[offset + 1];
        }

        public static int Read32(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        public static void Write16(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte) (value >> 8);
            bytes[offset + 1] = (byte) value;
        }

        public static void Write32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte) (value >> 24);
            bytes[offset + 1] = (byte) (value >> 16);
            bytes[offset + 2] = (byte) (value >> 8);
            bytes[offset + 3] = (byte) value;
        }

        public static void Add16(List<byte> output, int value)
        {
            output.Add((byte) (value >> 8));
            output.Add((byte) value);
        }

        public static void Add32(List<byte> output, int value)
        {
            output.Add((byte) (value >> 24));
            output.Add((byte) (value >> 16));
            output.Add((byte) (value >> 8));
            output.Add((byte) value);
        }
    }
}
