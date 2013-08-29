using System;
using System.Runtime.InteropServices;

namespace PortiLog.Monitor
{
    class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const uint WM_VSCROLL = 0x0115;
        public const int SB_BOTTOM = 7;
    }
}
