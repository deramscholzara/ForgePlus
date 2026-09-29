#if UNITY_STANDALONE_WIN

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SFB {
    // Uses the Windows shell's own file dialogs (IFileOpenDialog / IFileSaveDialog) directly through COM,
    // rather than Windows Forms, which isn't available on every .NET runtime Unity supports (such as CoreCLR).
    // For fullscreen support, "PlayerSettings/Visible In Background" should be enabled, otherwise the app window
    // minimizes when a file dialog opens.
    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
            var dialog = (IFileOpenDialog)new FileOpenDialog();
            try {
                var options = FOS.FORCEFILESYSTEM | FOS.PATHMUSTEXIST | FOS.FILEMUSTEXIST | FOS.NOCHANGEDIR;
                if (multiselect) {
                    options |= FOS.ALLOWMULTISELECT;
                }
                dialog.SetOptions(options);
                Configure(dialog, title, directory, extensions);

                if (dialog.Show(GetActiveWindow()) != S_OK) {
                    return new string[0];
                }

                dialog.GetResults(out var results);
                results.GetCount(out var count);
                var paths = new string[count];
                for (uint i = 0; i < count; i++) {
                    results.GetItemAt(i, out var item);
                    paths[i] = GetPath(item);
                }
                return paths;
            }
            finally {
                Marshal.ReleaseComObject(dialog);
            }
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb) {
            cb.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect) {
            var dialog = (IFileOpenDialog)new FileOpenDialog();
            try {
                dialog.SetOptions(FOS.PICKFOLDERS | FOS.FORCEFILESYSTEM | FOS.PATHMUSTEXIST | FOS.NOCHANGEDIR);
                Configure(dialog, title, directory, null);

                if (dialog.Show(GetActiveWindow()) != S_OK) {
                    return new string[0];
                }

                dialog.GetResult(out var item);
                return new[] { GetPath(item) };
            }
            finally {
                Marshal.ReleaseComObject(dialog);
            }
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb) {
            cb.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions) {
            var dialog = (IFileDialog)new FileSaveDialog();
            try {
                dialog.SetOptions(FOS.FORCEFILESYSTEM | FOS.PATHMUSTEXIST | FOS.OVERWRITEPROMPT | FOS.NOCHANGEDIR);
                Configure(dialog, title, directory, extensions);

                if (!string.IsNullOrEmpty(defaultName)) {
                    dialog.SetFileName(defaultName);
                }
                if (extensions != null && extensions.Length > 0 && extensions[0].Extensions.Length > 0) {
                    dialog.SetDefaultExtension(extensions[0].Extensions[0]);
                }

                if (dialog.Show(GetActiveWindow()) != S_OK) {
                    return "";
                }

                dialog.GetResult(out var item);
                return GetPath(item);
            }
            finally {
                Marshal.ReleaseComObject(dialog);
            }
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb) {
            cb.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }

        private static void Configure(IFileDialog dialog, string title, string directory, ExtensionFilter[] extensions) {
            if (!string.IsNullOrEmpty(title)) {
                dialog.SetTitle(title);
            }

            if (extensions != null && extensions.Length > 0) {
                var filters = new COMDLG_FILTERSPEC[extensions.Length];
                for (var i = 0; i < extensions.Length; i++) {
                    var patterns = new string[extensions[i].Extensions.Length];
                    for (var j = 0; j < patterns.Length; j++) {
                        patterns[j] = "*." + extensions[i].Extensions[j];
                    }
                    filters[i].pszName = extensions[i].Name + " (" + string.Join(", ", patterns) + ")";
                    filters[i].pszSpec = string.Join(";", patterns);
                }
                dialog.SetFileTypes((uint)filters.Length, filters);
                dialog.SetFileTypeIndex(1);
            }

            if (!string.IsNullOrEmpty(directory)) {
                var folder = GetDirectoryPath(directory);
                if (Directory.Exists(folder) &&
                    SHCreateItemFromParsingName(folder, IntPtr.Zero, typeof(IShellItem).GUID, out var folderItem) == S_OK) {
                    dialog.SetFolder(folderItem);
                }
            }
        }

        private static string GetPath(IShellItem item) {
            item.GetDisplayName(SIGDN_FILESYSPATH, out var pathPointer);
            try {
                return Marshal.PtrToStringUni(pathPointer);
            }
            finally {
                Marshal.FreeCoTaskMem(pathPointer);
            }
        }

        private static string GetDirectoryPath(string directory) {
            var directoryPath = Path.GetFullPath(directory);
            if (!directoryPath.EndsWith("\\", StringComparison.Ordinal)) {
                directoryPath += "\\";
            }
            if (Path.GetPathRoot(directoryPath) == directoryPath) {
                return directory;
            }
            return Path.GetDirectoryName(directoryPath) + Path.DirectorySeparatorChar;
        }

        // Shell file dialog COM interop (shobjidl_core.h)

        private const int S_OK = 0;
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        [Flags]
        private enum FOS : uint {
            OVERWRITEPROMPT = 0x00000002,
            NOCHANGEDIR = 0x00000008,
            PICKFOLDERS = 0x00000020,
            FORCEFILESYSTEM = 0x00000040,
            ALLOWMULTISELECT = 0x00000200,
            PATHMUSTEXIST = 0x00000800,
            FILEMUSTEXIST = 0x00001000,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC {
            [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

        [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport, Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
        private class FileSaveDialog { }

        [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
        }

        // COM interfaces derived from IFileDialog must redeclare its methods first, in the same order
        [ComImport, Guid("D57C7288-D4AD-4768-BE02-9D969532D960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog : IFileDialog {
            [PreserveSig] new int Show(IntPtr parent);
            new void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            new void SetFileTypeIndex(uint iFileType);
            new void GetFileTypeIndex(out uint piFileType);
            new void Advise(IntPtr pfde, out uint pdwCookie);
            new void Unadvise(uint dwCookie);
            new void SetOptions(FOS fos);
            new void GetOptions(out FOS pfos);
            new void SetDefaultFolder(IShellItem psi);
            new void SetFolder(IShellItem psi);
            new void GetFolder(out IShellItem ppsi);
            new void GetCurrentSelection(out IShellItem ppsi);
            new void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            new void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            new void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            new void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            new void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            new void GetResult(out IShellItem ppsi);
            new void AddPlace(IShellItem psi, int fdap);
            new void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            new void Close(int hr);
            new void SetClientGuid(ref Guid guid);
            new void ClearClientData();
            new void SetFilter(IntPtr pFilter);
            void GetResults(out IShellItemArray ppenum);
            void GetSelectedItems(out IShellItemArray ppsai);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);
            void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
            void GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);
            void GetAttributes(int attribFlags, uint sfgaoMask, out uint psfgaoAttribs);
            void GetCount(out uint pdwNumItems);
            void GetItemAt(uint dwIndex, out IShellItem ppsi);
            void EnumItems(out IntPtr ppenumShellItems);
        }
    }
}

#endif
