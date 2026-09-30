// Port of Aleph One: Source_Files/Files/FileHandler.h, FileHandler.cpp (data and resource files)
//
// Not ported: directories, zip files, text files and dialogs.
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

    /*
        Abstraction for loaded resources;
        this object will release that resource when it finishes.
    */
    public class LoadedResource
    {
        public byte[] p; // Pointer to resource data
        public int size; // Size of data

        public LoadedResource()
        {
            p = null;
            size = 0;
        }

        // Resource loaded?
        public bool IsLoaded()
        {
            return p != null;
        }

        // Unloads the resource
        public void Unload()
        {
            if (p != null)
            {
                p = null;
                size = 0;
            }
        }

        // Get size of loaded resource
        public int GetLength()
        {
            return size;
        }

        // Get pointer (always present)
        public byte[] GetPointer(bool DoDetach = false)
        {
            byte[] ret = p;
            if (DoDetach)
                Detach();
            return ret;
        }

        // Make resource from raw resource data; the caller gives up ownership
        // of the pointed to memory block
        public void SetData(byte[] data, int length)
        {
            Unload();
            p = data;
            size = length;
        }

        // Detaches an allocated resource from this object
        // (keep private to avoid memory leaks)
        private void Detach()
        {
            p = null;
            size = 0;
        }
    }

    /*
        Abstraction for opened resource files:
        it does opening, setting, and closing of such files;
        also getting "LoadedResource" objects that return pointers
    */
    public class OpenedResourceFile
    {
        internal SDL_RWops f; // File handle
        private SDL_RWops saved_f;
        private int err; // Error code

        public OpenedResourceFile()
        {
            f = null;
            saved_f = null;
            err = 0;
        }

        public bool Push()
        {
            saved_f = cur_res_file();
            if (saved_f != f)
                use_res_file(f);
            err = 0;
            return true;
        }

        public bool Pop()
        {
            if (f != saved_f)
                use_res_file(saved_f);
            err = 0;
            return true;
        }

        public bool Check(uint Type, short ID)
        {
            Push();
            bool result = has_1_resource(Type, ID);
            err = result ? 0 : FileSpecifier.unknown_filesystem_error; // ENOENT
            Pop();
            return result;
        }

        public bool Get(uint Type, short ID, LoadedResource Rsrc)
        {
            Push();
            bool success = get_1_resource(Type, ID, Rsrc);
            err = success ? 0 : FileSpecifier.unknown_filesystem_error; // ENOENT
            Pop();
            return success;
        }

        public bool IsOpen()
        {
            return f != null;
        }

        public bool Close()
        {
            if (f != null)
            {
                close_res_file(f);
                f = null;
                err = 0;
            }
            return true;
        }

        public int GetError() { return err; }
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

        // Open resource file
        public bool Open(OpenedResourceFile OFile, bool Writable = false)
        {
            OFile.Close();

            OFile.f = open_res_file(this);
            err = OFile.f != null ? 0 : unknown_filesystem_error;
            if (OFile.f == null)
            {
                return false;
            }
            else
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
