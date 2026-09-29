// Port of Aleph One: Source_Files/Files/Packing.h, Packing.cpp
//
// Aleph One packs and unpacks data by advancing a byte pointer ("uint8* S") through a buffer.
// StreamPointer stands in for that pointer: a buffer plus a position that each call advances.
// Packed data is big-endian (PACKED_DATA_IS_BIG_ENDIAN), which is Aleph One's default.
using System;
using System.Buffers.Binary;

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

        // The next count bytes, and S += count
        public Span<byte> Take(int count)
        {
            var span = Buffer.AsSpan(Position, count);
            Position += count;
            return span;
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
            Value = BinaryPrimitives.ReadUInt16BigEndian(Stream.Take(2));
        }

        public static void StreamToValue(StreamPointer Stream, out short Value)
        {
            Value = BinaryPrimitives.ReadInt16BigEndian(Stream.Take(2));
        }

        public static void StreamToValue(StreamPointer Stream, out uint Value)
        {
            Value = BinaryPrimitives.ReadUInt32BigEndian(Stream.Take(4));
        }

        public static void StreamToValue(StreamPointer Stream, out int Value)
        {
            Value = BinaryPrimitives.ReadInt32BigEndian(Stream.Take(4));
        }

        public static void ValueToStream(StreamPointer Stream, byte Value)
        {
            Stream.Buffer[Stream.Position++] = Value;
        }

        public static void ValueToStream(StreamPointer Stream, ushort Value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(Stream.Take(2), Value);
        }

        public static void ValueToStream(StreamPointer Stream, short Value)
        {
            BinaryPrimitives.WriteInt16BigEndian(Stream.Take(2), Value);
        }

        public static void ValueToStream(StreamPointer Stream, uint Value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(Stream.Take(4), Value);
        }

        public static void ValueToStream(StreamPointer Stream, int Value)
        {
            BinaryPrimitives.WriteInt32BigEndian(Stream.Take(4), Value);
        }

        // ForgePlus: the lists are read and written in one pass over their bytes, rather than a StreamToValue or
        // ValueToStream per element (the result is the same)
        public static void StreamToList(StreamPointer Stream, short[] List, int Count)
        {
            var S = Stream.Take(Count * 2);
            for (int k = 0; k < Count; k++) List[k] = BinaryPrimitives.ReadInt16BigEndian(S.Slice(k * 2));
        }

        public static void StreamToList(StreamPointer Stream, ushort[] List, int Count)
        {
            var S = Stream.Take(Count * 2);
            for (int k = 0; k < Count; k++) List[k] = BinaryPrimitives.ReadUInt16BigEndian(S.Slice(k * 2));
        }

        public static void StreamToList(StreamPointer Stream, int[] List, int Count)
        {
            var S = Stream.Take(Count * 4);
            for (int k = 0; k < Count; k++) List[k] = BinaryPrimitives.ReadInt32BigEndian(S.Slice(k * 4));
        }

        public static void StreamToList(StreamPointer Stream, uint[] List, int Count)
        {
            var S = Stream.Take(Count * 4);
            for (int k = 0; k < Count; k++) List[k] = BinaryPrimitives.ReadUInt32BigEndian(S.Slice(k * 4));
        }

        public static void ListToStream(StreamPointer Stream, short[] List, int Count)
        {
            var S = Stream.Take(Count * 2);
            for (int k = 0; k < Count; k++) BinaryPrimitives.WriteInt16BigEndian(S.Slice(k * 2), List[k]);
        }

        public static void ListToStream(StreamPointer Stream, ushort[] List, int Count)
        {
            var S = Stream.Take(Count * 2);
            for (int k = 0; k < Count; k++) BinaryPrimitives.WriteUInt16BigEndian(S.Slice(k * 2), List[k]);
        }

        public static void ListToStream(StreamPointer Stream, int[] List, int Count)
        {
            var S = Stream.Take(Count * 4);
            for (int k = 0; k < Count; k++) BinaryPrimitives.WriteInt32BigEndian(S.Slice(k * 4), List[k]);
        }

        public static void ListToStream(StreamPointer Stream, uint[] List, int Count)
        {
            var S = Stream.Take(Count * 4);
            for (int k = 0; k < Count; k++) BinaryPrimitives.WriteUInt32BigEndian(S.Slice(k * 4), List[k]);
        }

        public static void StreamToBytes(StreamPointer Stream, byte[] Bytes, int Count)
        {
            Stream.Take(Count).CopyTo(Bytes);
        }

        public static void BytesToStream(StreamPointer Stream, byte[] Bytes, int Count)
        {
            Bytes.AsSpan(0, Count).CopyTo(Stream.Take(Count));
        }
    }
}
