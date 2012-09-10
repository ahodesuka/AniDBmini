
#region Using Statements

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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
            PS_PLAY,
            PS_PAUSE,
            PS_STOP,
            PS_UNUSED
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
            /// <para>Par 1 : file path</para>
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
            /// <para>Par 1 : file path</para>
            /// </summary>
            CMD_ADDTOPLAYLIST = 0xA0001000,

            /// <summary>
            /// Remove all files from playlist
            /// </summary>
            CMD_CLEARPLAYLIST = 0xA0001001,

            /// <summary>
            /// Start playing playlist
            /// </summary>
            CMD_STARTPLAYLIST = 0xA0001002,

            /// <summary>
            /// Cue current file to specific position
            /// <para>Par 1 : new position in seconds</para>
            /// </summary>
            CMD_SETPOSITION = 0xA0002000,

            /// <summary>
            /// Ask for the current playback position,
            /// see CMD_CURRENTPOSITION.
            /// <para>Par 1 : current position in seconds</para>
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

            /// <summary>
            /// Show host defined OSD message string
            /// </summary>
            CMD_OSDSHOWMESSAGE = 0xA0005000,
        };

        private enum OSD_MESSAGEPOS
        {
            OSD_NOMESSAGE,
            OSD_TOPLEFT,
            OSD_TOPRIGHT
        };

        public enum MPC_WATCHED
        {
            DISABLED,
            DURING_TICKS,
            AFTER_FINISHED
        };

        #endregion Enums

        #region Structs

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        private struct MPC_OSDDATA
        {
            public int nMsgPos;
            public int nDurationMS;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
            public string strMsg;
        };

        #endregion Structs

        #region Fields

        private MainWindow m_MainWindow;
        private HwndSource m_Source;
        private IntPtr m_hWnd, m_hWndMPC;
        private DispatcherTimer m_PlayTimer;

        private MPC_PLAYSTATE m_currentPlayState;
        private MPC_WATCHED m_watchedWhen;
        private OSD_MESSAGEPOS m_OSDMSGPos;

        private string m_currentFileTitle, m_currentFilePath;
        private int m_OSDMSGDur, m_mpcProcID;
        private double m_watchedWhenPerc, m_currentFileLength, m_currentFilePosition, m_currentFileTick;
        private bool m_currentFileWatched, m_ShowInFileTitle;

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
            
            AniDBAPI.AppendDebugLine("MPC-HC hook added");

            using (Process MPC = new Process())
            {
                MPC.StartInfo = new ProcessStartInfo(ConfigFile.Read("mpcPath").ToString(), String.Format("/slave {0}", m_hWnd.ToInt32()));
                MPC.Start();

                m_mpcProcID = MPC.Id;
            }

            m_PlayTimer = new DispatcherTimer();
            m_PlayTimer.Interval = TimeSpan.FromMilliseconds(1000);
            m_PlayTimer.Tick += delegate
            {
                ++m_currentFileTick;
                SendData(MPCAPI_SENDCOMMAND.CMD_GETCURRENTPOSITION, String.Empty);
            };
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
                    string mpcMSG = new String((char*)cds.lpData, 0, cds.cbData / 2 - 1);

                    switch (nCmd)
                    {
                        case MPCAPI_COMMAND.CMD_CONNECT:
                            isHooked = true;
                            m_hWndMPC = new IntPtr(int.Parse(mpcMSG));
                            break;
                        case MPCAPI_COMMAND.CMD_CURRENTPOSITION:
                        case MPCAPI_COMMAND.CMD_NOTIFYSEEK:
                            m_currentFilePosition = double.Parse(mpcMSG);
                            if (!m_currentFileWatched && m_watchedWhen == MPC_WATCHED.DURING_TICKS &&
                                m_currentFileTick > m_currentFileLength * m_watchedWhenPerc)
                            {
                                OnFileWatched(this, new FileWatchedArgs(m_currentFilePath));
                                m_currentFileWatched = true;
                                m_PlayTimer.Stop();
                            }
                            break;
                        case MPCAPI_COMMAND.CMD_NOWPLAYING:
                            string[] playing = mpcMSG.Split('|');
                            m_currentFileTitle = !string.IsNullOrWhiteSpace(playing[0]) ? playing[0] : System.IO.Path.GetFileName(playing[3]);
                            m_currentFilePath = playing[3];
                            m_currentFileLength = double.Parse(playing[4]);
                            m_currentFileWatched = false;
                            m_currentFilePosition = m_currentFileTick = 0;
                            LoadConfig();
                            SetTitle();
                            break;
                        case MPCAPI_COMMAND.CMD_PLAYMODE:
                            m_currentPlayState = (MPC_PLAYSTATE)int.Parse(mpcMSG);
                            SetTitle();
                            switch (m_currentPlayState)
                            {
                                case MPC_PLAYSTATE.PS_PLAY:
                                    if (!m_currentFileWatched)
                                        m_PlayTimer.IsEnabled = true;
                                    break;
                                case MPC_PLAYSTATE.PS_PAUSE:
                                case MPC_PLAYSTATE.PS_STOP:
                                    m_PlayTimer.IsEnabled = false;
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
                                    if (m_watchedWhen == MPC_WATCHED.AFTER_FINISHED && !m_currentFileWatched &&
                                        m_currentFileTick > m_currentFileLength * m_watchedWhenPerc)
                                    {
                                        OnFileWatched(this, new FileWatchedArgs(m_currentFilePath));
                                        m_currentFileWatched = true;
                                        m_PlayTimer.Stop();
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
            WinAPI.COPYDATASTRUCT nCDS;
            if (nCmd == MPCAPI_SENDCOMMAND.CMD_OSDSHOWMESSAGE)
            {
                MPC_OSDDATA osdData = new MPC_OSDDATA
                {
                    nMsgPos = (int)m_OSDMSGPos,
                    nDurationMS = m_OSDMSGDur,
                    strMsg = strCmd
                };
                nCDS = new WinAPI.COPYDATASTRUCT
                {
                    dwData = (IntPtr)(int)nCmd,
                    cbData = Marshal.SizeOf(osdData)
                };
                nCDS.lpData = Marshal.AllocCoTaskMem(nCDS.cbData);
                Marshal.StructureToPtr(osdData, nCDS.lpData, false);
            }
            else
            {
                nCDS = new WinAPI.COPYDATASTRUCT
                {
                    dwData = (IntPtr)(int)nCmd,
                    cbData = (strCmd.Length + 1) * 2,
                    lpData = Marshal.StringToCoTaskMemUni(strCmd)
                };
            }

            IntPtr cdsPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(nCDS));
            Marshal.StructureToPtr(nCDS, cdsPtr, false);

            WinAPI.SendMessage(m_hWndMPC, WinAPI.WM_COPYDATA, m_hWnd, cdsPtr);

            Marshal.FreeCoTaskMem(cdsPtr);
            Marshal.FreeCoTaskMem(nCDS.lpData);
        }

        /// <summary>
        /// Send a command to MPC-HC
        /// </summary>
        /// <param name="nCmd">Command to send.</param>
        private void SendData(MPCAPI_SENDCOMMAND nCmd)
        {
            SendData(nCmd, String.Empty);
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

        private void SetTitle()
        {
            if (m_currentPlayState == MPC_PLAYSTATE.PS_STOP)
            {
                SetTitle(MainWindow.m_AppName);
                return;
            }

            string playState = m_currentPlayState == MPC_PLAYSTATE.PS_PAUSE ? "[Paused] " : String.Empty;

            SetTitle(String.Format("{0} - {1}{2}", MainWindow.m_AppName, playState, m_currentFileTitle));
        }

        private void SetTitle(string o_Title)
        {
            if (m_ShowInFileTitle)
                m_MainWindow.wTitle = o_Title;
        }

        private void RemoveHook()
        {
            SetTitle(MainWindow.m_AppName);
            m_Source.RemoveHook(WndProc);
            isHooked = false;

            AniDBAPI.AppendDebugLine("MPC-HC hook removed");
        }

        #endregion Private Methods

        #region Public Methods

        public void CloseMPC()
        {
            SendData(MPCAPI_SENDCOMMAND.CMD_CLOSEAPP);
        }

        public bool FocusMPC()
        {
            WinAPI.FocusWindow(m_hWndMPC);
            return IsMPCAlive();
        }

        public void LoadConfig()
        {
            m_watchedWhen = (MPC_WATCHED)ConfigFile.Read("mpcMarkWatched").ToInt32();
            m_watchedWhenPerc = ConfigFile.Read("mpcMarkWatchedPerc").ToInt32() * .01;
            m_ShowInFileTitle = ConfigFile.Read("mpcShowTitle").ToBoolean();
            m_OSDMSGPos = (OSD_MESSAGEPOS)ConfigFile.Read("mpcOSDPos").ToInt32();
            m_OSDMSGDur = ConfigFile.Read("mpcOSDDurMS").ToInt32();
        }
        
        /// <summary>
        /// Opens a file and starts playback.
        /// </summary>
        /// <param name="path">Absolute path of file</param>
        public void OpenFile(string path)
        {
            SendData(MPCAPI_SENDCOMMAND.CMD_OPENFILE, path);
        }

        /// <summary>
        /// Clear all files from the playlist.
        /// </summary>
        public void ClearPlaylist()
        {
            SendData(MPCAPI_SENDCOMMAND.CMD_CLEARPLAYLIST);
        }

        /// <summary>
        /// Adds a file to the playlist.
        /// </summary>
        /// <param name="path">Absolute path of file</param>
        public void AddFileToPlaylist(string path)
        {
            SendData(MPCAPI_SENDCOMMAND.CMD_ADDTOPLAYLIST, path);
        }

        /// <summary>
        /// Start playing the playlist.
        /// </summary>
        public void StartPlaylist()
        {
            SendData(MPCAPI_SENDCOMMAND.CMD_STARTPLAYLIST);
        }

        /// <summary>
        /// Displays a message within MPC-HC.
        /// </summary>
        /// <param name="msg">Message to display</param>
        public void OSDShowMessage(string msg)
        {
            SendData(MPCAPI_SENDCOMMAND.CMD_OSDSHOWMESSAGE, msg);
        }

        #endregion Public Methods

        #region Properties

        public int ProcessID { get { return m_mpcProcID; } }

        public string CurrentFileName
        {
            get
            {
                return m_currentPlayState != MPC_PLAYSTATE.PS_STOP ?
                    System.IO.Path.GetFileName(m_currentFilePath) : String.Empty;
            }
        }

        #endregion Properties

    }
}