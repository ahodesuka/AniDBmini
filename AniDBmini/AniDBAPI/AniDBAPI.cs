
#region Using Statements

using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class AniDBAPI
	{

		#region Fields

        private MainWindow mainWindow;
		private Ed2k hasher = new Ed2k();

        private UdpClient conn = new UdpClient();
        private IPEndPoint apiserver;

        private byte[] data = new byte[1400];
        private bool isLoggedIn;
        private string sessionKey, user, pass;

        private DispatcherTimer addTimer;
        private List<HashItem> addToMyList = new List<HashItem>();

		private static ThreadSafeObservableCollection<DebugLine> debugLog;

        public string[] statsText = { "Total anime in mylist",
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

		#endregion Fields

		#region Constructor

		public AniDBAPI(string server, int port, int localPort)
        {
#if !OFFLINE
            apiserver = new IPEndPoint(IPAddress.Any, localPort);
            conn.Connect(server, port); //TODO: Check if connect was succesful or not
#endif
            InitializeAddTimer();
        }

        #endregion Constructor

        #region Initialize

        private void InitializeAddTimer()
        {
            addTimer = new DispatcherTimer();
            addTimer.Interval = TimeSpan.FromSeconds(4);
            addTimer.Tick += (s, e) =>
            {
                if (addToMyList.Count == 0)
                    addTimer.Stop();
                else
                    MyListAdd(addToMyList[0]);
            };
        }

        #endregion Initialize

        #region AUTH

        public bool Login(string user, string pass)
        {
            APIResponse response = Execute("AUTH user=" + user + "&pass=" + pass + "&protover=3&client=anidbmini&clientver=1&enc=UTF8");

            if (response.Code == 200 || response.Code == 201) // successful login
            {
                sessionKey = response.Message.Split(' ')[1];
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
            Execute("LOGOUT", true);
        }

        #endregion AUTH

        #region DATA

        public AnimeTab Anime(int animeID)
        {
            APIResponse response = Execute("ANIME aid=" + animeID + "&amask=b2e05efe400080");

            if (response.Code == 230)
                return new AnimeTab(Regex.Split(response.Message, "\n")[1]);
            else
                return null;
        }

        #endregion DATA

        #region MYLIST

        public void AddToMyList(HashItem item)
        {
            if (!addToMyList.Contains(item))
            {
                addToMyList.Add(item);

                if (!addTimer.IsEnabled)
                {
                    MyListAdd(addToMyList[0]);
                    addTimer.Start();
                }
            }
        }

		private void MyListAdd(HashItem item)
		{
            string r_msg = string.Empty;
            APIResponse response = Execute("MYLISTADD size=" + item.Size + 
                                                     "&ed2k=" + item.Hash + 
                                                     "&viewed=" + item.Viewed + 
                                                     "&state=" + item.State + (item.Edit ? "&edit=1" : null));
            switch (response.Code)
            {
                case 210:
                    r_msg = "Added " + item.Name + " to mylist.";
                    break;
                case 310:
                    addToMyList[0].Edit = true;
                    return;
                case 311:
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

            addToMyList.Remove(item);
		}

        public int[] MyListStats()
        {
            string r_msg = Execute("MYLISTSTATS", true).Message;
            return Array.ConvertAll<string, int>(Regex.Split(r_msg, "\n")[1].Split('|'), delegate(string s) { return int.Parse(s); });
        }

        public AnimeTab RandomAnime(int type)
        {
            string r_msg = Execute("RANDOMANIME type=" + type).Message;
            int randomID = int.Parse(Regex.Split(r_msg, "\n")[1].Split('|')[0]);

            return Anime(randomID);
        }

        #endregion MYLIST

        #region NOTIFY

        /// <summary>
        /// not in use.
        /// </summary>
        private bool Uptime()
        {
            return Execute("UPTIME", true).Code == 501 ? false : true;
        }

        #endregion NOTIFY

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

		public static void AppendDebugLine(string line)
        {
			debugLog.Add(new DebugLine(DateTime.Now.ToLongTimeString(), line.ToString()));
        }

        /// <summary>
        /// Executes the current command.
        /// </summary>
        /// <returns>Response from server.</returns>
        private APIResponse Execute(string cmd, bool isSingleCMD = false)
        {
#if !OFFLINE
            string e_cmd = cmd;
            string e_response = string.Empty;

            if (isLoggedIn)
                e_cmd += (isSingleCMD ? " " : "&") + "s=" + sessionKey;

            data = Encoding.UTF8.GetBytes(e_cmd);
            conn.Send(data, data.Length);
            data = conn.Receive(ref apiserver);

            e_response = Encoding.UTF8.GetString(data, 0, data.Length);
            int e_code = int.Parse(e_response.Substring(0, 3));

            switch (e_code)
            {
                case 501:
                case 502:
                case 506:
                    isLoggedIn = false;
                    if (Login(user, pass))
                        return Execute(cmd, isSingleCMD);
                    else
                    {
                        var login = new LoginWindow();
                        login.Show();
                        mainWindow.Close();
                        return null;
                    }
                default:
                    return new APIResponse(e_response, e_code);
            }
#else
            return new APIResponse("offline", 200);
#endif
        }

		#endregion Private Methods

		#region Properties

        public MainWindow MainWindow
        {
            set { mainWindow = value; }
        }

		public ThreadSafeObservableCollection<DebugLine> DebugLog
        {
			get { return debugLog; }
            set
            {
                if (value != null)
                {
                    debugLog = value;

					AppendDebugLine("Welcome to AniDBmini, connected to: " + apiserver);
                }
            }
        }

        public event FileHashingProgressHandler FileHashingProgress
        {
            add { hasher.FileHashingProgress += value; }
            remove { hasher.FileHashingProgress -= value; }
        }

        #endregion Properties

    }
}
