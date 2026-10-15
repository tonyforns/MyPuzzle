using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MyPuzzle.View
{
    public static class ImageFileDialog
    {
        public static bool TryPick(out string path)
        {
            if (Type.GetType("UnityEditor.EditorUtility, UnityEditor") != null)
                return TryPickInEditor(out path);

            return TryPickOnWindows(out path);
        }

        static bool TryPickInEditor(out string path)
        {
            path = null;
            var editorUtility = Type.GetType("UnityEditor.EditorUtility, UnityEditor");
            if (editorUtility == null)
                return false;

            var open = editorUtility.GetMethod("OpenFilePanel", new[] { typeof(string), typeof(string), typeof(string) });
            if (open == null)
                return false;

            path = open.Invoke(null, new object[] { "Elegir imagen", "", "png,jpg,jpeg" }) as string;
            return !string.IsNullOrEmpty(path);
        }

        static bool TryPickOnWindows(out string path)
        {
            path = null;
            var buffer = Marshal.AllocHGlobal(512 * 2);
            try
            {
                for (var i = 0; i < 512 * 2; i++)
                    Marshal.WriteByte(buffer, i, 0);

                var dialog = new OpenFileName
                {
                    dlgOwner = IntPtr.Zero,
                    instance = IntPtr.Zero,
                    filter = "Imágenes\0*.png;*.jpg;*.jpeg\0",
                    file = buffer,
                    maxFile = 512,
                    title = "Elegir imagen",
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008
                };
                dialog.structSize = Marshal.SizeOf(dialog);
                if (!GetOpenFileName(ref dialog))
                    return false;

                path = Marshal.PtrToStringUni(buffer);
                return !string.IsNullOrEmpty(path);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetOpenFileName(ref OpenFileName dialog);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public string filter;
            public string customFilter;
            public int maxCustomFilter;
            public int filterIndex;
            public IntPtr file;
            public int maxFile;
            public string fileTitle;
            public int maxFileTitle;
            public string initialDirectory;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defaultExtension;
            public IntPtr customData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reservedPointer;
            public int reservedInt;
            public int flagsEx;
        }
    }
}
