// Port of Aleph One: Source_Files/Files/Packing.h, Packing.cpp
//
// Aleph One packs and unpacks data by advancing a byte pointer ("uint8* S") through a buffer.
// StreamPointer stands in for that pointer: a buffer plus a position that each call advances.
// Packed data is big-endian (PACKED_DATA_IS_BIG_ENDIAN), which is Aleph One's default.
using System;

namespace AlephOne
{
    public sealed class StreamPointer
    {
        public readonly byte[] Buffer;
        public int Position;

        public StreamPointer(byte[] buffer, int position = 0)
        {
            Buffer = buffer;
            Position = position;
        }

        // S += count
        public void Skip(int count)
        {
            Position += count;
        }
    }

    public static class Packing
    {
        // Single bytes, which Aleph One reads and writes as *(S++)
        public static void StreamToValue(StreamPointer Stream, out byte Value)
        {
            Value = Stream.Buffer[Stream.Position++];
        }

        public static void StreamToValue(StreamPointer Stream, out ushort Value)
        {
            var S = Stream.Buffer;
            var p = Stream.Position;
            Value = (ushort) ((S[p] << 8) | S[p + 1]);
            Stream.Position += 2;
        }

        public static void StreamToValue(StreamPointer Stream, out short Value)
        {
            StreamToValue(Stream, out ushort UValue);
            Value = unchecked((short) UValue);
        }

        public static void StreamToValue(StreamPointer Stream, out uint Value)
        {
            var S = Stream.Buffer;
            var p = Stream.Position;
            Value = ((uint) S[p] << 24) | ((uint) S[p + 1] << 16) | ((uint) S[p + 2] << 8) | S[p + 3];
            Stream.Position += 4;
        }

        public static void StreamToValue(StreamPointer Stream, out int Value)
        {
            StreamToValue(Stream, out uint UValue);
            Value = unchecked((int) UValue);
        }

        public static void ValueToStream(StreamPointer Stream, byte Value)
        {
            Stream.Buffer[Stream.Position++] = Value;
        }

        public static void ValueToStream(StreamPointer Stream, ushort Value)
        {
            var S = Stream.Buffer;
            var p = Stream.Position;
            S[p] = (byte) (Value >> 8);
            S[p + 1] = (byte) Value;
            Stream.Position += 2;
        }

        public static void ValueToStream(StreamPointer Stream, short Value)
        {
            ValueToStream(Stream, unchecked((ushort) Value));
        }

        public static void ValueToStream(StreamPointer Stream, uint Value)
        {
            var S = Stream.Buffer;
            var p = Stream.Position;
            S[p] = (byte) (Value >> 24);
            S[p + 1] = (byte) (Value >> 16);
            S[p + 2] = (byte) (Value >> 8);
            S[p + 3] = (byte) Value;
            Stream.Position += 4;
        }

        public static void ValueToStream(StreamPointer Stream, int Value)
        {
            ValueToStream(Stream, unchecked((uint) Value));
        }

        public static void StreamToList(StreamPointer Stream, short[] List, int Count)
        {
            for (int k = 0; k < Count; k++) StreamToValue(Stream, out List[k]);
        }

        public static void StreamToList(StreamPointer Stream, ushort[] List, int Count)
        {
            for (int k = 0; k < Count; k++) StreamToValue(Stream, out List[k]);
        }

        public static void StreamToList(StreamPointer Stream, int[] List, int Count)
        {
            for (int k = 0; k < Count; k++) StreamToValue(Stream, out List[k]);
        }

        public static void StreamToList(StreamPointer Stream, uint[] List, int Count)
        {
            for (int k = 0; k < Count; k++) StreamToValue(Stream, out List[k]);
        }

        public static void ListToStream(StreamPointer Stream, short[] List, int Count)
        {
            for (int k = 0; k < Count; k++) ValueToStream(Stream, List[k]);
        }

        public static void ListToStream(StreamPointer Stream, ushort[] List, int Count)
        {
            for (int k = 0; k < Count; k++) ValueToStream(Stream, List[k]);
        }

        public static void ListToStream(StreamPointer Stream, int[] List, int Count)
        {
            for (int k = 0; k < Count; k++) ValueToStream(Stream, List[k]);
        }

        public static void ListToStream(StreamPointer Stream, uint[] List, int Count)
        {
            for (int k = 0; k < Count; k++) ValueToStream(Stream, List[k]);
        }

        public static void StreamToBytes(StreamPointer Stream, byte[] Bytes, int Count)
        {
            Buffer.BlockCopy(Stream.Buffer, Stream.Position, Bytes, 0, Count);
            Stream.Position += Count;
        }

        public static void BytesToStream(StreamPointer Stream, byte[] Bytes, int Count)
        {
            Buffer.BlockCopy(Bytes, 0, Stream.Buffer, Stream.Position, Count);
            Stream.Position += Count;
        }
    }
}
