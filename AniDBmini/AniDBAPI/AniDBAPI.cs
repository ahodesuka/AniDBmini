
#region Using Statements

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using AniDBmini.Collections;
using AniDBmini.HashAlgorithms;

#endregion

namespace AniDBmini
{

    #region Args & Delegates

    public class FileInfoFetchedArgs : EventArgs
    {
        public MylistEntry Anime { get; private set; }
        public EpisodeEntry Episode { get; private set; }
        public FileEntry File { get; private set; }

        public FileInfoFetchedArgs(MylistEntry anime, EpisodeEntry episode, FileEntry file)
        {
            Anime = anime;
            Episode = episode;
            File = file;
        }
    }

    public delegate void FileInfoFetchedHandler(FileInfoFetchedArgs fInfo);
    public delegate void AnimeTabFetchedHandler(AnimeTab aTab);

    #endregion Args & Delegates

    public class AniDBAPI
    {

        #region Structs

        public struct APIResponse
        {
            public string Message;
            public int Code;
        }

        #endregion Structs

        #region Fields

        private MainWindow mainWindow;
		private Ed2k hasher = new Ed2k();

        private UdpClient conn = new UdpClient();
        private IPEndPoint apiserver;
        private DateTime m_lastCommand;

        private byte[] data = new byte[1400];
        private bool isLoggedIn;
        private string sessionKey, user, pass;

        private static TSObservableCollection<DebugLine> debugLog = new TSObservableCollection<DebugLine>();

        public static string[] statsText = { "Total anime in mylist",
                                             "Total eps in mylist",
                                             "Total files in mylist",
                                             "Size of mylist",
                                             "x", "x", "x", "x", "x", "x",
                                             "AniDB watched",
                                             "AniDB in mylist",
                                             "Mylist watched",
                                             "Eps watched",
                                             "x", "x",
                                             "Time wasted" };

        public event FileInfoFetchedHandler OnFileInfoFetched;
        public event AnimeTabFetchedHandler OnAnimeTabFetched;

		#endregion Fields

		#region Constructor

		public AniDBAPI(string server, int port, int localPort)
        {
#if !DEBUG
            apiserver = new IPEndPoint(IPAddress.Any, localPort);
            conn.Connect(server, port); //TODO: Check if connect was succesful or not
#endif
        }

        #endregion Constructor

        #region AUTH

        public bool Login(string user, string pass)
        {
            APIResponse response = Execute("AUTH user=" + user + "&pass=" + pass + "&protover=3&client=anidbmini&clientver=1&enc=UTF8");

            if (response.Code == 200 || response.Code == 201) // successful login
            {
#if !DEBUG
                sessionKey = response.Message.Split(' ')[1];
#endif
                isLoggedIn = true;
                this.user = user;
                this.pass = pass;                
            }
            else
            {
                MessageBox.Show(response.Message, "Login Failed!", MessageBoxButton.OK, MessageBoxImage.Error);
                isLoggedIn = false;
            }

            return isLoggedIn;
        }

        public void Logout()
        {
            Execute("LOGOUT");
        }

        #endregion AUTH

        #region DATA

        /// <summary>
        /// Anime command to create a anime tab from an anime ID.
        /// </summary>
        private void Anime(int animeID)
        {
            APIResponse response = Execute(String.Format("ANIME aid={0}&amask=b2e05efe400080", animeID));

            if (response.Code == 230)
                OnAnimeTabFetched(new AnimeTab(Regex.Split(response.Message, "\n")[1]));
        }

        private void File(HashItem item)
        {
            Action fileInfo = new Action(delegate
            {
                APIResponse response = Execute(String.Format("FILE size={0}&ed2k={1}&fmask=7800682810&amask=70e0f0c0", item.Size, item.Hash));

                //if (response.Code == 220)
                {
                    string[] info = Regex.Split(response.Message, "\n")[1].Split('|');

                    MylistEntry anime = new MylistEntry();
                    EpisodeEntry episode = new EpisodeEntry();
                    FileEntry file = new FileEntry(item);

                    anime.aid = int.Parse(info[1]);
                    episode.eid = int.Parse(info[2]);

                    file.gid = int.Parse(info[3]);
                    file.lid = int.Parse(info[4]);
                    file.source = info[5].formatNullable();
                    file.acodec = info[6].Contains("'") ? info[6].Split('\'')[0] : info[6].formatNullable();
                    file.vcodec = info[7];
                    file.vres = info[8].formatNullable();
                    file.length = int.Parse(info[9]);
                    episode.airdate = info[10];
                    file.watcheddate = info[11];

                    anime.eps_total = int.Parse(info[12]);
                    anime.year = info[13];
                    anime.type = info[14];
                    anime.title = info[15];
                    anime.nihongo = info[16].formatNullable();
                    anime.english = info[17].formatNullable();

                    episode.epno = int.Parse(info[18]);
                    episode.english = info[19];
                    episode.romaji = info[20].formatNullable();
                    episode.nihongo = info[21].formatNullable();

                    file.group_name = info[22];
                    file.group_abbr = info[23];

                    OnFileInfoFetched(new FileInfoFetchedArgs(anime, episode, file));
                    System.Diagnostics.Debug.WriteLine(response.Message);
                }
            });

            LowPriorityCommand(fileInfo);               
        }

        #endregion DATA

        #region MYLIST

        public void MyListAdd(HashItem item)
        {
            Action addToList = new Action(delegate
            {
                string r_msg = String.Empty;
                APIResponse response = Execute("MYLISTADD size=" + item.Size +
                                                        "&ed2k=" + item.Hash +
                                                        "&viewed=" + item.Viewed +
                                                        "&state=" + item.State + (item.Edit ? "&edit=1" : null));
                switch (response.Code)
                {
                    case 210:
                        File(item);
                        r_msg = "Added " + item.Name + " to mylist.";
                        break;
                    case 310:
                        item.Edit = true;
                        MyListAdd(item);
                        return;
                    case 311:
                        File(item);
                        r_msg = "Edited mylist entry for " + item.Name + ".";
                        break;
                    case 320:
                        r_msg = "Error! File not in database.";
                        break;
                    case 330:
                        r_msg = "Error! Anime not in database.";
                        break;
                    case 350:
                        r_msg = "Error! Group not in database.";
                        break;
                    case 411:
                        r_msg = "Error! Mylist entry not found.";
                        break;
                }

                AppendDebugLine(r_msg);
            });

            LowPriorityCommand(addToList);
        }

        public int[] MyListStats()
        {
            string r_msg = Execute("MYLISTSTATS").Message;
            return Array.ConvertAll<string, int>(Regex.Split(r_msg, "\n")[1].Split('|'), delegate(string s) { return int.Parse(s); });
        }

        public void RandomAnime(int type)
        {
            Action random = new Action(delegate
            {
                APIResponse response = Execute(String.Format("RANDOMANIME type={0}", type));
                //if (response.Code == 230)
                {
                    Action anime = new Action(delegate { Anime(int.Parse(Regex.Split(response.Message, "\n")[1].Split('|')[0])); });
                    LowPriorityCommand(anime);
                }
            });

            LowPriorityCommand(random);
        }

        #endregion MYLIST

        #region File Hashing

        public HashItem ed2kHash(HashItem item)
        {
            hasher.Clear();
            FileInfo file = new FileInfo(item.Path);

            using (FileStream fs = file.OpenRead())
            {
                AppendDebugLine("Hashing " + item.Name);
                byte[] temp;

                if ((temp = hasher.ComputeHash(fs)) != null)
                {
                    item.Hash = string.Concat(temp.Select(b => b.ToString("x2")).ToArray());
                    AppendDebugLine("Ed2k hash: " + item.Hash);

                    return item;
                }
                else
                    AppendDebugLine("Hashing aborted");

                return null;
            }
        }

        public void cancelHashing()
        {
            hasher.Cancel();
            hasher.Clear();
        }

		#endregion File Hashing

		#region Private Methods

        /// <summary>
        /// Executes an action after a certain amount of time has passed
        /// since the previous command was sent to the server.
        /// </summary>
        /// <param name="todo"></param>
        private void LowPriorityCommand(Action todoCommand)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                double secondsSince = DateTime.Now.Subtract(m_lastCommand).TotalSeconds;

                if (secondsSince < 4)
                    Thread.Sleep(TimeSpan.FromSeconds(4 - secondsSince));

                todoCommand();
            }));
        }

        /// <summary>
        /// Executes a given command.
        /// And returns a response.
        /// </summary>
        /// <returns>Response from server.</returns>
        private APIResponse Execute(string cmd)
        {
#if !DEBUG
            string e_cmd = cmd;
            string e_response = String.Empty;

            if (isLoggedIn)
                e_cmd += (e_cmd.Contains("&") ? "&" : " ") + "s=" + sessionKey;

            data = Encoding.UTF8.GetBytes(e_cmd);
            conn.Send(data, data.Length);
            data = conn.Receive(ref apiserver);

            m_lastCommand = DateTime.Now;

            e_response = Encoding.UTF8.GetString(data, 0, data.Length);
            int e_code = int.Parse(e_response.Substring(0, 3));

            switch (e_code)
            {
                case 501:
                case 502:
                case 506:
                    isLoggedIn = false;
                    if (Login(user, pass))
                        return Execute(cmd);
                    else
                    {
                        var login = new LoginWindow();
                        login.Show();
                        mainWindow.Close();
                        return new APIResponse();
                    }
                default:                    
                    return new APIResponse { Message = e_response, Code = e_code };
            }
#else
            m_lastCommand = DateTime.Now;
            APIResponse response = new APIResponse { Message = "\n1|2", Code = 200 };
            System.Diagnostics.Debug.WriteLine(String.Format("Executed: {0} @ {1}", cmd, m_lastCommand.ToLongTimeString()));
            System.Diagnostics.Debug.WriteLine(String.Format("Response: {0}", response.Message));
            return response;
#endif
        }

        #endregion Private Methods

        #region Properties & Static Methods

        public static void AppendDebugLine(string line)
        {
            debugLog.Add(new DebugLine(DateTime.Now.ToLongTimeString(), line.ToString()));
        }

        public MainWindow MainWindow
        {
            set { mainWindow = value; }
        }

        public IPEndPoint APIServer { get { return apiserver; } }
        public TSObservableCollection<DebugLine> DebugLog { get { return debugLog; } }

        public event FileHashingProgressHandler FileHashingProgress
        {
            add { hasher.FileHashingProgress += value; }
            remove { hasher.FileHashingProgress -= value; }
        }

        #endregion Properties

    }
}
