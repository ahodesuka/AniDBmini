
#region Using Statements

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

#endregion Using Statements

namespace AniDBmini
{
    public class MPCAPI
    {

        #region Enums

        private enum MPC_LOADSTATE
        {
            MLS_CLOSED,
            MLS_LOADING,
            MLS_LOADED,
            MLS_CLOSING
        };

        private enum MPC_PLAYSTATE
        {
            PS_PLAY = 0,
            PS_PAUSE = 1,
            PS_STOP = 2,
            PS_UNUSED = 3
        };

        private enum MPCAPI_COMMAND
        {
            // ==== Commands from MPC to host ==== //

            /// <summary>
            /// Sent after connection
            /// Par 1 : MPC window handle (command should be send to this HWnd)
            /// </summary>
            CMD_CONNECT = 0x50000000,

            /// <summary>
            /// Send when opening or closing file
            /// Par 1 : current state (see MPC_LOADSTATE enum)
            /// </summary>
            CMD_STATE = 0x50000001,

            /// <summary>
            /// Send when playing, pausing or closing file
            /// Par 1 : current play mode (see MPC_PLAYSTATE enum)
            /// </summary>
            CMD_PLAYMODE = 0x50000002,

            /// <summary>
            /// Send after opening a new file
            /// Par 1 : title
            /// Par 2 : author
            /// Par 3 : description
            /// Par 4 : complete filename (path included)
            /// Par 5 : duration in seconds
            /// </summary>
            CMD_NOWPLAYING = 0x50000003,

            /// <summary>
            /// List of subtitle tracks
            /// Par 1 : Subtitle track name 0
            /// Par 2 : Subtitle track name 1
            /// ...
            /// Par n : Active subtitle track, -1 if subtitles disabled
            ///
            /// if no subtitle track present, returns -1
            /// if no file loaded, returns -2
            /// </summary>
            CMD_LISTSUBTITLETRACKS = 0x50000004,

            /// <summary>
            /// List of audio tracks
            /// Par 1 : Audio track name 0
            /// Par 2 : Audio track name 1
            /// ...
            /// Par n : Active audio track
            /// 
            /// if no audio track present, returns -1
            /// if no file loaded, returns -2
            /// </summary>
            CMD_LISTAUDIOTRACKS = 0x50000005,

            /// <summary>
            /// Send current playback position in responce
            /// of CMD_GETCURRENTPOSITION.
            /// Par 1 : current position in seconds
            /// </summary>
            CMD_CURRENTPOSITION = 0x50000007,

            /// <summary>
            /// Send the current playback position after a jump.
            /// (Automatically sent after a seek event).
            /// Par 1 : new playback position (in seconds).
            /// </summary>
            CMD_NOTIFYSEEK = 0x50000008,

            /// <summary>
            /// Notify the end of current playback
            /// (Automatically sent).
            /// Par 1 : none.
            /// </summary>
            CMD_NOTIFYENDOFSTREAM = 0x50000009,

            /// <summary>
            /// List of files in the playlist
            /// Par 1 : file path 0
            /// Par 2 : file path 1
            /// ...
            /// Par n : active file, -1 if no active file
            /// </summary>
            CMD_PLAYLIST = 0x50000006

        };

        private enum MPCAPI_SENDCOMMAND : uint
        {
            // ==== Commands from host to MPC ==== //

            /// <summary>
            /// Open new file
            /// Par 1 : file path
            /// </summary>
            CMD_OPENFILE = 0xA0000000,

            /// <summary>
            /// Stop playback, but keep file / playlist
            /// </summary>
            CMD_STOP = 0xA0000001,

            /// <summary>
            /// Stop playback and close file / playlist
            /// </summary>
            CMD_CLOSEFILE = 0xA0000002,

            /// <summary>
            /// Pause or restart playback
            /// </summary>
            CMD_PLAYPAUSE = 0xA0000003,

            /// <summary>
            /// Add a new file to playlist (did not start playing)
            /// Par 1 : file path
            /// </summary>
            CMD_ADDTOPLAYLIST = 0xA0001000,

            /// <summary>
            /// Remove all files from playlist
            /// </summary>
            CMD_CLEARPLAYLIST = 0xA0001001,

            /// <summary>
            /// Start playing playlist
            CMD_STARTPLAYLIST = 0xA0001002,

            /// <summary>
            /// Cue current file to specific position
            /// Par 1 : new position in seconds
            /// </summary>
            CMD_SETPOSITION = 0xA0002000,

            /// <summary>
            /// Ask for the current playback position,
            /// see CMD_CURRENTPOSITION.
            /// Par 1 : current position in seconds
            /// </summary>
            CMD_GETCURRENTPOSITION = 0xA0003004,

            /// <summary>
            /// Ask for the properties of the current loaded file
            /// return a CMD_NOWPLAYING
            /// </summary>
            CMD_GETNOWPLAYING = 0xA0003002,

            /// <summary>
            /// Ask for the current playlist
            /// return a CMD_PLAYLIST
            /// </summary>
            CMD_GETPLAYLIST = 0xA0003003,

            /// <summary>
            /// Close App
            /// </summary>
            CMD_CLOSEAPP = 0xA0004006,
        };

        #endregion Enums

        #region Fields

        private MainWindow m_MainWindow;
        private HwndSource m_Source;
        private IntPtr m_hWnd, m_hWndMPC;
        private DispatcherTimer m_SeekTimer;

        private MPC_PLAYSTATE m_currentPlayState;
        private string m_currentFileTitle, m_currentFilePath;
        private int m_currentFileLength, m_currentFilePosition;
        private bool m_fileClosing;

        public bool isHooked { get; private set; }
        public event FileWatchedHandler OnFileWatched = delegate { };

        #endregion Fields

        #region Constructor

        public MPCAPI(MainWindow main)
        {
            m_MainWindow = main;
            m_hWnd = new WindowInteropHelper(m_MainWindow).Handle;

            m_Source = HwndSource.FromHwnd(m_hWnd);
            m_Source.AddHook(WndProc);

            isHooked = true;
            AniDBAPI.AppendDebugLine("MPC-HC hook added.");

            using (Process MPC = new Process())
            {
                MPC.StartInfo = new ProcessStartInfo(ConfigFile.Read("mpchcPath").ToString(), "/slave " + m_hWnd.ToInt32());
                MPC.Start();
            }

            m_SeekTimer = new DispatcherTimer();
            m_SeekTimer.Interval = TimeSpan.FromSeconds(1);
            m_SeekTimer.Tick += (s, e) => { SendData(MPCAPI_SENDCOMMAND.CMD_GETCURRENTPOSITION, string.Empty); };
        }

        #endregion Constructor

        #region Private Methods

        private unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WinAPI.WM_COPYDATA)
            {
                WinAPI.COPYDATASTRUCT cds = (WinAPI.COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(WinAPI.COPYDATASTRUCT));
                if (cds.cbData > 0)
                {
                    MPCAPI_COMMAND nCmd = (MPCAPI_COMMAND)cds.dwData;
                    string mpcMSG = new String((char*)cds.lpData, 0, cds.cbData / 2);

                    //AniDBAPI.AppendDebugLine(String.Format("{0} : {1}", nCmd, mpcMSG));

                    switch (nCmd)
                    {
                        case MPCAPI_COMMAND.CMD_CONNECT:
                            m_hWndMPC = new IntPtr(int.Parse(mpcMSG));
                            break;
                        // This will only fire for the last item on a playlist..
                        //case MPCAPI_COMMAND.CMD_NOTIFYENDOFSTREAM:
                        //    OnFileWatched(this, new FileWatchedArgs(m_currentFilePath));
                        //    break;
                        case MPCAPI_COMMAND.CMD_CURRENTPOSITION:
                        case MPCAPI_COMMAND.CMD_NOTIFYSEEK:
                            m_currentFilePosition = int.Parse(mpcMSG);
                            break;
                        case MPCAPI_COMMAND.CMD_NOWPLAYING:
                            string[] playing = mpcMSG.Split('|');
                            m_currentFileTitle = !string.IsNullOrWhiteSpace(playing[0]) ? playing[0] : System.IO.Path.GetFileName(playing[3]);
                            m_currentFilePath = playing[3];
                            m_currentFileLength = int.Parse(playing[4]);
                            m_currentFilePosition = 0;
                            SetTitle();
                            m_fileClosing = false;
                            break;
                        case MPCAPI_COMMAND.CMD_PLAYMODE:
                            m_currentPlayState = (MPC_PLAYSTATE)int.Parse(mpcMSG);
                            SetTitle();
                            switch (m_currentPlayState)
                            {
                                case MPC_PLAYSTATE.PS_PLAY:
                                    m_SeekTimer.Start();
                                    break;
                                case MPC_PLAYSTATE.PS_PAUSE:
                                case MPC_PLAYSTATE.PS_STOP:
                                    m_SeekTimer.Stop();
                                    break;
                            }
                            break;
                        case MPCAPI_COMMAND.CMD_STATE:
                            MPC_LOADSTATE mpcLS = (MPC_LOADSTATE)int.Parse(mpcMSG);
                            switch (mpcLS)
                            {
                                case MPC_LOADSTATE.MLS_CLOSED:
                                    IsMPCAlive();
                                    break;
                                case MPC_LOADSTATE.MLS_CLOSING:
                                    if (m_currentFileLength - m_currentFilePosition < m_currentFilePosition / 15 && !m_fileClosing)
                                    {
                                        OnFileWatched(this, new FileWatchedArgs(m_currentFilePath));
                                        m_fileClosing = true;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                handled = true;
            }
            else
                handled = false;

            return IntPtr.Zero;
        }

        /// <summary>
        /// Send a command to MPC-HC
        /// </summary>
        /// <param name="nCmd">Command to send.</param>
        /// <param name="strCmd">Some commands require a parameter.</param>
        private void SendData(MPCAPI_SENDCOMMAND nCmd, string strCmd)
        {
            WinAPI.COPYDATASTRUCT nCDS = new WinAPI.COPYDATASTRUCT
            {
                dwData = (uint)nCmd,
                cbData = (strCmd.Length + 1) * 2,
                lpData = Marshal.StringToCoTaskMemUni(strCmd)
            };

            WinAPI.SendMessage(m_hWndMPC, (uint)WinAPI.WM_COPYDATA, m_hWnd, ref nCDS);
        }

        private bool IsMPCAlive()
        {
            if (!WinAPI.IsWindowVisible(m_hWndMPC))
            {
                this.RemoveHook();
                return false;
            }

            return true;
        }

        private void SetTitle(string o_Title = null)
        {
            if (!string.IsNullOrWhiteSpace(o_Title))
                m_MainWindow.Title = o_Title;
            else
            {
                string playState = m_currentPlayState == MPC_PLAYSTATE.PS_PLAY ||
                                   m_currentPlayState == MPC_PLAYSTATE.PS_STOP ? string.Empty : "[Paused]";

                m_MainWindow.Title = String.Format("{0} - {1} {2}", MainWindow.m_AppName, playState, m_currentFileTitle);
            }
        }

        private void RemoveHook()
        {
            SetTitle(MainWindow.m_AppName);
            m_Source.RemoveHook(WndProc);
            isHooked = false;

            AniDBAPI.AppendDebugLine("MPC-HC hook removed.");
        }

        #endregion Private Methods

        #region Public Methods

        public bool FocusMPC()
        {
            WinAPI.FocusWindow(m_hWndMPC);

            if (!IsMPCAlive())
                return false;

            return true;
        }

        #endregion Public Methods

    }
}