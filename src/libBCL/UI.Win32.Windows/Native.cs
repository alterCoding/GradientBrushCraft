using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace AltCoD.UI.Win32.Windows
{
    public static partial class Native
    {
		public static void CenterWindow(IntPtr hParentWnd, IntPtr hChildWnd)
		{
			Rectangle rect = Rectangle.Empty;
			GetWindowRect(hChildWnd, ref rect);
			int width = rect.Width - rect.X;
			int height = rect.Height - rect.Y;

			GetWindowRect(hParentWnd, ref rect);
			int xCenter = rect.X + ((rect.Width - rect.X) / 2);
			int yCenter = rect.Y + ((rect.Height - rect.Y) / 2);

			int xWnd = xCenter - (width / 2);
			if (xWnd < 0) xWnd = 0;
			int yWnd = yCenter - (height / 2);
			if (yWnd < 0) yWnd = 0;

			MoveWindow(hChildWnd, xWnd, yWnd, width, height, false);
		}

		public const int WM_ACTIVATE = 6;

		//loword WPARAM, WM_ACTIVATE (window is activated)
		public const int WM_CLICKACTIVE = 2;
		//loword WPARAM, WM_ACTIVATE (window is activated)
		public const int WA_ACTIVE = 1;
		//loword WPARAM, WM_ACTIVATE (window is deactivated)
		public const int WA_INACTIVE = 0;

		public const int WM_CLEAR = 0x0303;

		[DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);
        [DllImport("user32.dll")]
        private static extern int MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }

}
