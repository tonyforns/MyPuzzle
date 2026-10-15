using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MyPuzzle.View
{
    public static class PuzzleFileDialog
    {
        public static bool TrySave(out string path)
        {
            if (Type.GetType("UnityEditor.EditorUtility, UnityEditor") != null)
                return TrySaveInEditor(out path);

            return TrySaveOnWindows(out path);
        }

        public static bool TryOpen(out string path)
        {
            if (Type.GetType("UnityEditor.EditorUtility, UnityEditor") != null)
                return TryOpenInEditor(out path);

            return TryOpenOnWindows(out path);
        }

        static bool TrySaveInEditor(out string path)
        {
            path = Invoke("SaveFilePanel", "Guardar partida", "partida", "mypuzzle");
            return !string.IsNullOrEmpty(path);
        }

        static bool TryOpenInEditor(out string path)
        {
            path = null;
            var editorUtility = Type.GetType("UnityEditor.EditorUtility, UnityEditor");
            if (editorUtility == null)
                return false;

            var open = editorUtility.GetMethod("OpenFilePanel", new[] { typeof(string), typeof(string), typeof(string) });
            if (open == null)
                return false;

            path = open.Invoke(null, new object[] { "Cargar partida", string.Empty, "mypuzzle" }) as string;
            return !string.IsNullOrEmpty(path);
        }

        static string Invoke(string methodName, string title, string defaultName, string extension)
        {
            var editorUtility = Type.GetType("UnityEditor.EditorUtility, UnityEditor");
            if (editorUtility == null)
                return null;

            var method = editorUtility.GetMethod(methodName, new[] { typeof(string), typeof(string), typeof(string), typeof(string) });
            if (method == null)
                return null;

            return method.Invoke(null, new object[] { title, string.Empty, defaultName, extension }) as string;
        }

        static bool TrySaveOnWindows(out string path)
        {
            return TryWindows(save: true, "Guardar partida", out path);
        }

        static bool TryOpenOnWindows(out string path)
        {
            return TryWindows(save: false, "Cargar partida", out path);
        }

        static bool TryWindows(bool save, string title, out string path)
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
                    filter = "Partida\0*.mypuzzle\0",
                    file = buffer,
                    maxFile = 512,
                    title = title,
                    defaultExtension = "mypuzzle",
                    flags = 0x00080000 | 0x00000800 | 0x00000008 | 0x00000002
                };
                dialog.structSize = Marshal.SizeOf(dialog);
                var accepted = save ? GetSaveFileName(ref dialog) : GetOpenFileName(ref dialog);
                if (!accepted)
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

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetSaveFileName(ref OpenFileName dialog);

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
