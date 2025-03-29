using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace AltCoD.UI.Win32
{
    /// <summary>
    /// A native console helper <br/>
    /// As of now, only SET font (name, size) is implemented
    /// </summary>
    public class NativeConsole
    {
        public NativeConsole(IntPtr handle)
        {
            _handle = handle;
        }

        public static NativeConsole StdOut
        {
            get
            {
                if (_stdOut == null) _stdOut = new NativeConsole(GetStdHandle(STD_OUTPUT_HANDLE));

                if (_stdOut == null)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open STD_OUTPUT");

                return _stdOut;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="fontname">note: if font does not exist, a fallback is enabled (it's the safe underlying 
        /// behavior)<br/>
        /// A true type fixed width font is expected
        /// </param>
        /// <param name="size"></param>
        public void SetFont(string fontname, short size)
        {
            FontInfo font = new FontInfo
            {
                cbSize = Marshal.SizeOf<FontInfo>(),
                FontIndex = 0,
                FontFamily = FixedWidthTrueType,
                FontName = fontname,
                FontWeight = 400, //normal
                FontSize = size
            };

            if (!SetCurrentConsoleFontEx(_handle, false, ref font))
            {
                var ex = Marshal.GetLastWin32Error();
                throw new Win32Exception(ex);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetCurrentConsoleFontEx(IntPtr handle, bool maxWndSize, ref FontInfo font);

        /// <summary>
        /// 
        /// </summary>
        /// @internal https://learn.microsoft.com/en-us/windows/console/console-font-infoex
        ///
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct FontInfo
        {
            internal int cbSize;
            internal int FontIndex;
            internal short FontWidth;
            public short FontSize;
            /// <summary>
            /// font pitch and family
            /// </summary>
            public int FontFamily;
            public int FontWeight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FontName;
        }

        /// <summary>
        /// </summary>
        /// @internal https://learn.microsoft.com/en-us/windows/console/getstdhandle
        /// it seems that closing the handle is not needed
        private const int STD_OUTPUT_HANDLE = -11;

        /// <summary>
        /// tmPitchAndFamily flags (wingdi.h)
        /// </summary>
        /// @internal https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-textmetrica
        private const byte TMPF_TRUETYPE = 0x4;
        private const byte TMPF_FIXED_PITCH = 0x1;
        private const byte TMPF_VECTOR = 0x2;
        private const byte FF_MODERN = (3 << 4);  /* Constant stroke width, serifed or sans-serifed. */
        private const byte FixedWidthTrueType = FF_MODERN | TMPF_TRUETYPE | TMPF_FIXED_PITCH | TMPF_VECTOR;

        private readonly IntPtr _handle;

        private static NativeConsole _stdOut;
    }
}
