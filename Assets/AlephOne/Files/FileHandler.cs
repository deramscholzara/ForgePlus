// Port of Aleph One: Source_Files/Files/FileHandler.h, FileHandler.cpp (data files only)
//
// Not ported: resource forks, directories, zip files, text files and dialogs.
using System;
using System.IO;
using static AlephOne.resource_manager;
using static AlephOne.SDL_rwops;

namespace AlephOne
{
    /*
        Abstraction for opened files; it does reading, writing, and closing of such files,
        without doing anything to the files' specifications
    */
    public class OpenedFile
    {
        internal SDL_RWops f; // File handle
        private int err; // Error code
        internal bool is_forked;
        internal int fork_offset, fork_length;

        public OpenedFile()
        {
            f = null;
            err = 0;
            is_forked = false;
            fork_offset = 0;
            fork_length = 0;
        }

        public bool IsOpen()
        {
            return f != null;
        }

        public bool Close()
        {
            if (f != null)
            {
                SDL_RWclose(f);
                f = null;
                err = 0;
            }
            is_forked = false;
            fork_offset = 0;
            fork_length = 0;
            return true;
        }

        public bool GetPosition(out int Position)
        {
            Position = 0;
            if (f == null)
                return false;

            err = 0;
            Position = (int) SDL_RWtell(f) - fork_offset;
            return true;
        }

        public bool SetPosition(int Position)
        {
            if (f == null)
                return false;

            err = 0;
            if (SDL_RWseek(f, Position + fork_offset, SEEK_SET) < 0)
                err = FileSpecifier.unknown_filesystem_error;
            return err == 0;
        }

        public bool GetLength(out int Length)
        {
            Length = 0;
            if (f == null)
                return false;

            if (is_forked)
                Length = fork_length;
            else
            {
                int pos = (int) SDL_RWtell(f);
                SDL_RWseek(f, 0, SEEK_END);
                Length = (int) SDL_RWtell(f);
                SDL_RWseek(f, pos, SEEK_SET);
            }
            err = 0;
            return true;
        }

        public bool Read(int Count, byte[] Buffer, int BufferOffset = 0)
        {
            if (f == null)
                return false;

            err = 0;
            return (SDL_RWread(f, Buffer, BufferOffset, 1, Count) == Count);
        }

        public bool Write(int Count, byte[] Buffer, int BufferOffset = 0)
        {
            if (f == null)
                return false;

            err = 0;
            return (SDL_RWwrite(f, Buffer, BufferOffset, 1, Count) == Count);
        }

        public int GetError() { return err; }
        public SDL_RWops GetRWops() { return f; }
    }

    public class FileSpecifier
    {
        // Returned by .GetError() for unknown errors
        public const int unknown_filesystem_error = -1;

        private string name;
        private int err;

        public FileSpecifier()
        {
            name = "";
            err = 0;
        }

        public FileSpecifier(string name)
        {
            this.name = name;
            err = 0;
        }

        public string GetPath()
        {
            return name;
        }

        // Get last element of path
        public string GetName()
        {
            return Path.GetFileName(name);
        }

        public int GetError() { return err; }

        public bool Exists()
        {
            // Check whether the file is readable
            err = 0;
            if (!File.Exists(name))
                err = unknown_filesystem_error;
            return err == 0;
        }

        // Create file
        public bool Create()
        {
            Delete();
            // files are automatically created when opened for writing
            err = 0;
            return true;
        }

        // Open data file
        public bool Open(OpenedFile OFile, bool Writable = false)
        {
            OFile.Close();

            SDL_RWops f;
            {
                f = OFile.f = SDL_RWFromFile(GetPath(), Writable ? "wb+" : "rb");
                err = f != null ? 0 : unknown_filesystem_error;
            }

            if (f == null)
            {
                return false;
            }
            if (Writable)
                return true;

            // Transparently handle AppleSingle and MacBinary files on reading
            int offset, data_length, rsrc_length;
            if (is_applesingle(f, false, out offset, out data_length))
            {
                OFile.is_forked = true;
                OFile.fork_offset = offset;
                OFile.fork_length = data_length;
                SDL_RWseek(f, offset, SEEK_SET);
                return true;
            }
            else if (is_macbinary(f, out data_length, out rsrc_length))
            {
                OFile.is_forked = true;
                OFile.fork_offset = 128;
                OFile.fork_length = data_length;
                SDL_RWseek(f, 128, SEEK_SET);
                return true;
            }
            SDL_RWseek(f, 0, SEEK_SET);
            return true;
        }

        public bool Delete()
        {
            err = 0;
            try
            {
                if (File.Exists(name))
                    File.Delete(name);
                else
                    err = unknown_filesystem_error; // ENOENT
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                err = unknown_filesystem_error;
            }
            return err == 0;
        }

        // Replaces Destination if it exists
        public bool Rename(FileSpecifier Destination)
        {
            err = 0;
            try
            {
                if (File.Exists(Destination.name))
                    File.Replace(name, Destination.name, null);
                else
                    File.Move(name, Destination.name);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                err = unknown_filesystem_error;
            }
            return err == 0;
        }

        public void SetTempName(FileSpecifier other)
        {
            name = other.name + Guid.NewGuid().ToString("N").Substring(0, 6);
        }
    }
}
