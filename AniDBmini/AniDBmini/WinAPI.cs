
#region Using Statements

using System;
using System.Runtime.InteropServices;

#endregion Using Statements

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        #endregion Structs

        #region Externs

        [DllImport("kernel32.dll")]
        public static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, System.Text.StringBuilder lpReturnedString, int nSize, string lpFilePath);

        [DllImport("kernel32.dll")]
        public static extern long WritePrivateProfileString(string lPAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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

        #endregion Public Methods
    }
}
