// Stand-in for SDL2's SDL_rwops.h and SDL_endian.h: the calls Aleph One makes, with SDL's results.
// Reads past the end give zeros (SDL_ReadBE16/32) or a short count (SDL_RWread), writes and seeks
// that fail give a short count and -1, and SDL_RWFromFile gives NULL for a file it can't open.
using System;
using System.Buffers.Binary;
using System.IO;

namespace AlephOne
{
    public sealed class SDL_RWops
    {
        internal readonly Stream stream;

        internal SDL_RWops(Stream stream)
        {
            this.stream = stream;
        }
    }

    public static class SDL_rwops
    {
        public const int RW_SEEK_SET = 0; /* Seek from the beginning of data */
        public const int RW_SEEK_CUR = 1; /* Seek relative to current read point */
        public const int RW_SEEK_END = 2; /* Seek relative to the end of data */

        // stdio.h
        public const int SEEK_SET = RW_SEEK_SET;
        public const int SEEK_CUR = RW_SEEK_CUR;
        public const int SEEK_END = RW_SEEK_END;

        // mode is "rb" or "wb+"
        public static SDL_RWops SDL_RWFromFile(string file, string mode)
        {
            try
            {
                // Files being read are read into memory once, since Aleph One reads them a few bytes at a time
                return new SDL_RWops(mode == "rb" ?
                    new MemoryStream(File.ReadAllBytes(file), writable: false) :
                    new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static SDL_RWops SDL_RWFromMem(byte[] mem, int size)
        {
            return new SDL_RWops(new MemoryStream(mem, 0, size, true));
        }

        public static SDL_RWops SDL_RWFromConstMem(byte[] mem, int size)
        {
            return new SDL_RWops(new MemoryStream(mem, 0, size, false));
        }

        public static int SDL_RWclose(SDL_RWops context)
        {
            context.stream.Dispose();
            return 0;
        }

        public static long SDL_RWseek(SDL_RWops context, long offset, int whence)
        {
            try
            {
                SeekOrigin origin = whence == RW_SEEK_SET ? SeekOrigin.Begin : whence == RW_SEEK_CUR ? SeekOrigin.Current : SeekOrigin.End;
                return context.stream.Seek(offset, origin);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static long SDL_RWtell(SDL_RWops context)
        {
            return context.stream.Position;
        }

        // size_t SDL_RWread(SDL_RWops *context, void *ptr, size_t size, size_t maxnum), ptr being ptr[ptr_offset]
        public static int SDL_RWread(SDL_RWops context, byte[] ptr, int ptr_offset, int size, int maxnum)
        {
            long total = (long) size * maxnum;
            if (size <= 0 || maxnum <= 0)
            {
                return 0;
            }

            return ReadUpTo(context, ptr.AsSpan(ptr_offset, (int) total)) / size;
        }

        // size_t SDL_RWwrite(SDL_RWops *context, const void *ptr, size_t size, size_t num), ptr being ptr[ptr_offset]
        public static int SDL_RWwrite(SDL_RWops context, byte[] ptr, int ptr_offset, int size, int num)
        {
            if (size <= 0 || num <= 0)
            {
                return 0;
            }

            try
            {
                context.stream.Write(ptr, ptr_offset, size * num);
            }
            catch (IOException)
            {
                return 0;
            }

            return num;
        }

        public static ushort SDL_ReadBE16(SDL_RWops src)
        {
            Span<byte> value = stackalloc byte[2];
            ReadUpTo(src, value);
            return BinaryPrimitives.ReadUInt16BigEndian(value);
        }

        public static uint SDL_ReadBE32(SDL_RWops src)
        {
            Span<byte> value = stackalloc byte[4];
            ReadUpTo(src, value);
            return BinaryPrimitives.ReadUInt32BigEndian(value);
        }

        // Fills as much of the buffer as the stream has, returning how much that was
        // (so SDL_ReadBE16/32, reading into zeroed buffers, read zeros past the end)
        private static int ReadUpTo(SDL_RWops context, Span<byte> buffer)
        {
            int read_total = 0;
            try
            {
                while (read_total < buffer.Length)
                {
                    int read = context.stream.Read(buffer.Slice(read_total));
                    if (read <= 0)
                    {
                        break;
                    }

                    read_total += read;
                }
            }
            catch (IOException)
            {
            }

            return read_total;
        }
    }
}
