using System;
using System.Runtime.InteropServices;

namespace AniDBmini
{
    public static class WinAPI
    {
        #region Consts

        private const int LOCALE_SSHORTDATE = 0x1F;
        private const int LOCALE_STIME = 0x1003;

        public const int WM_COPYDATA = 0x4A;

        #endregion Consts

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public uint dwData;
            public int cbData;
            public IntPtr lpData;
        }

        #endregion Structs

        #region Externs

        [DllImport("kernel32.dll")]
        private static extern int GetSystemDefaultLCID();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetLocaleInfo(int Locale, int LCType, [In, MarshalAs(UnmanagedType.LPWStr)] string lpLCData, int cchData);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        #endregion Externs

        #region Public Methods

        public static void FocusWindow(IntPtr hWnd)
        {
            ShowWindowAsync(hWnd, 1);
            SetForegroundWindow(hWnd);
        }

        public static string LocalDateFormat()
        {
            int size = GetLocaleInfo(GetSystemDefaultLCID(), LOCALE_SSHORTDATE, null, 0);

            String dateFormat = new String(' ', size);
            GetLocaleInfo(GetSystemDefaultLCID(), LOCALE_SSHORTDATE, dateFormat, size);

            return dateFormat;
        }

        #endregion Public Methods
    }
}
