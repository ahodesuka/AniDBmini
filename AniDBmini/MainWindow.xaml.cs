
#region Using Statements

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

using AniDBmini.Collections;
using AniDBmini.HashAlgorithms;

#endregion

namespace AniDBmini
{
    public partial class MainWindow : Window
    {

        #region Fields

        public static string m_AppName = Application.ResourceAssembly.GetName().Name;

        private Forms.NotifyIcon m_notifyIcon;
        private Forms.ContextMenu m_notifyContextMenu = new Forms.ContextMenu();
        private WindowState m_storedWindowState = WindowState.Normal;

        private BackgroundWorker m_Worker;
        private Dispatcher m_Dispatcher = Dispatcher.CurrentDispatcher;

        private AniDBAPI aniDB;
        private MPCAPI mpcApi;

        private DateTime m_hashingStartTime, m_hashingLastTime;

        private int m_storedTabIndex;

        private bool isHashing;
        private double totalQueueSize, ppSize;

        private string[] allowedVideoFiles = { "*.avi", "*.mkv", "*.mov", "*.mp4", "*.mpeg", "*.mpg", "*.ogm" };

        private TSObservableCollection<MylistStat> mylistStatsList = new TSObservableCollection<MylistStat>();
        private TSObservableCollection<HashItem> hashFileList = new TSObservableCollection<HashItem>();
        private TSObservableCollection<AnimeTab> animeTabList = new TSObservableCollection<AnimeTab>();

        #endregion Fields

        #region Constructor

        public MainWindow(AniDBAPI api)
        {
            //MessageBox.Show(DateTime.Parse("02.08.2011 12:14:20", System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")).ToString(WindowsLocale.LocalDateFormat()));

            aniDB = api;
            AniDBAPI.AppendDebugLine("Welcome to AniDBmini, connected to: " + aniDB.APIServer);

            InitializeComponent();

            mylistStats.ItemsSource = mylistStatsList;
            debugListBox.ItemsSource = aniDB.DebugLog;
            hashingListBox.ItemsSource = hashFileList;
            animeTabControl.ItemsSource = animeTabList;

            animeTabList.OnCountChanged += new CountChangedHandler(animeTabList_OnCountChanged);
            aniDB.FileHashingProgress += new FileHashingProgressHandler(OnFileHashingProgress);

            InitializeStats();
            InitializeNotifyIcon();
        }

        #endregion Constructor

        #region Initialize

        /// <summary>
        /// Retrieve, format, and draw stats.
        /// </summary>
        private void InitializeStats()
        {
            int[] stats = aniDB.MyListStats();
            int i = 0;

            foreach (int stat in stats)
            {
                string text = AniDBAPI.statsText[i];
                string value;

                if (text != "x")
                {
                    if (i == 3)
                        value = (Math.Round((stat / 1024f) / (stat > 1048576 ? 1024f : 1), 2)).ToString() + (stat > 1048576 ? "TB" : "GB");
                    else if (i == 16)
                    {
                        int days = (int)Math.Floor((stat / 60f) / 24f);
                        int hours = (int)Math.Floor((((stat / 60f) / 24f) - (int)Math.Floor((stat / 60f) / 24f)) * 24);
                        int minutes = (int)((Math.Round((((stat / 60f) / 24f) - (int)Math.Floor((stat / 60f) / 24f)) * 24, 2) - hours) * 60);
                        value = days + "d " + hours + "h " + minutes + "m";
                    }
                    else if (i >= 10 && i <= 12)
                        value = stat + "%";
                    else
                        value = stat.ToString();

                    mylistStatsList.Add(new MylistStat(text, value));
                }

                i++;
            }
        }

        private void InitializeNotifyIcon()
        {
            m_notifyIcon = new Forms.NotifyIcon();
            m_notifyIcon.Text = this.Title;
            m_notifyIcon.Icon = new System.Drawing.Icon(global::AniDBmini.Properties.Resources.AniDBmini, 16, 16);
            m_notifyIcon.MouseDoubleClick += (s, e) => { this.Show(); WindowState = m_storedWindowState; };
            m_notifyIcon.ContextMenu = m_notifyContextMenu;

            Forms.MenuItem cm_open = new Forms.MenuItem();
            cm_open.Text = "Open";
            cm_open.Click += (s, e) => { Show(); WindowState = m_storedWindowState; };
            m_notifyContextMenu.MenuItems.Add(cm_open);
            m_notifyContextMenu.MenuItems.Add("-");

            Forms.MenuItem cm_exit = new Forms.MenuItem();
            cm_exit.Text = "Exit";
            cm_exit.Click += (s, e) => { this.Close(); };
            m_notifyContextMenu.MenuItems.Add(cm_exit);
        }

        #endregion

        #region Hashing

        private void addRowToHashTable(string path)
        {
            HashItem item = new HashItem(path);
            hashFileList.Add(item);

            m_Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!isHashing && !hashingStartButton.IsEnabled)
                    hashingStartButton.IsEnabled = true;
                else if (isHashing)
                    totalQueueSize += item.Size;
            }));
        }

        private void beginHashing()
        {
            hashingStartButton.IsEnabled = false;
            hashingStopButton.IsEnabled = isHashing = true;

            totalQueueSize = 0;

            for (int i = 0; i < hashFileList.Count; i++)
                totalQueueSize += hashFileList[i].Size;

            m_Worker = new BackgroundWorker();
            m_Worker.WorkerSupportsCancellation = true;

            m_Worker.DoWork += (s, e) =>
            {
                while (hashFileList.Count > 0 && isHashing)
                {
                    if (m_Worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    HashItem _temp = aniDB.ed2kHash(hashFileList[0]);

                    if (isHashing && _temp != null) // if we have not aborted remove item from queue and process
                    {
                        hashFileList[0] = _temp;
                        m_Dispatcher.BeginInvoke(new Action<HashItem>(FinishHash), hashFileList[0]);

                        hashFileListRemove(hashFileList[0]);
                    }
                }
            };
            m_Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnRunWorkerCompleted);

            m_hashingStartTime = m_hashingLastTime = DateTime.Now;
            m_Worker.RunWorkerAsync();
        }

        private void hashFileListRemove(HashItem item, bool _e = false)
        {
            if (isHashing && _e)
                totalQueueSize -= item.Size;

            hashFileList.Remove(item);

            if (hashFileList.Count == 0)
                hashingStartButton.IsEnabled = hashingStopButton.IsEnabled = false;
        }

        #endregion Hashing

        #region Events

        private void ShowOpionsWindow(object sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow();
            options.Owner = this;
            if (options.ShowDialog() == true && mpcApi != null)
                mpcApi.LoadConfig();
        }

        private void mpchcLaunch(object sender, RoutedEventArgs e)
        {
            if (mpcApi == null || !mpcApi.isHooked || !mpcApi.FocusMPC())
            {
                if (File.Exists(ConfigFile.Read("mpcPath").ToString()))
                {
                    mpcApi = new MPCAPI(this);
                    mpcApi.OnFileWatched += new FileWatchedHandler(OnFileWatched);
                }
                else
                    MessageBox.Show("Media Player Classic - Home Cinema not found!\nPlease ensure you have selected the mpc-hc executable inside the options.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnFileWatched(object sender, FileWatchedArgs e)
        {
            HashItem item = new HashItem(e.Path);
            item.State = item.Viewed = 1;

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                item = aniDB.ed2kHash(item);
                aniDB.AddToMyList(item);

                if (ConfigFile.Read("mpcShowOSD").ToBoolean())
                    mpcApi.ShowWatchedOSD();
            }));
        }

        private void randomAnimeButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            ContextMenu cm = btn.ContextMenu;
            cm.PlacementTarget = btn;
            cm.IsOpen = true;
        }

        private void randomAnimeLabelContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            this.Cursor = Cursors.Wait;

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                int aid = 0;
                AnimeTab aTab = null;

                m_Dispatcher.Invoke(new Action(delegate { aniDB.RandomAnime(int.Parse(mi.Tag.ToString()), out aid); }), TimeSpan.FromSeconds(1), null);

                if (aid != 0)
                {
                    Thread.Sleep(2000); // Flood prevention.
                    m_Dispatcher.Invoke(new Action(delegate { aniDB.Anime(aid, out aTab); }), TimeSpan.FromSeconds(1), null);

                    if (aTab != null)
                        animeTabList.Add(aTab);
                }

                this.Dispatcher.BeginInvoke(new Action(delegate { this.Cursor = Cursors.Arrow; }), null);
            }));
        }

        private void clearDebugLog(object sender, RoutedEventArgs e)
        {
            aniDB.DebugLog.Clear();
        }

        private void hashingListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Array.Sort(files);

                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo fi = new FileInfo(files[i]);

                    if (allowedVideoFiles.Contains<string>("*" + fi.Extension))
                        addRowToHashTable(fi.FullName);
                }
            }
        }

        private void removeSelectedItems(object sender, RoutedEventArgs e)
        {
            while (hashingListBox.SelectedItems.Count > 0)
                hashFileListRemove((HashItem)hashingListBox.SelectedItems[0], true);
        }

        private void hashingListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                removeSelectedItems(sender, null);
        }

        private void startHashingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isHashing)
                beginHashing();
        }

        private void hashingStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isHashing)
            {
                m_Worker.CancelAsync();
                aniDB.cancelHashing();

                OnRunWorkerCompleted(sender, null);
            }
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            hashingStopButton.IsEnabled = isHashing = false;
            fileProgressBar.Value = totalProgressBar.Value = ppSize = 0;
            hashingStartButton.IsEnabled = hashFileList.Count > 0;
            timeRemainingTextBlock.Text = String.Empty;
        }

        private void addFilesButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Video Files|" + String.Join(";", allowedVideoFiles) + "|All Files|*.*";
            dlg.Multiselect = true;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
                {
                    for (int i = 0; i < dlg.FileNames.Length; i++)
                        addRowToHashTable(dlg.FileNames[i]);
                }));
            }
        }

        private void addFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            Forms.FolderBrowserDialog dlg = new Forms.FolderBrowserDialog();
            dlg.ShowNewFolderButton = false;

            Forms.DialogResult result = dlg.ShowDialog();
            if (result == Forms.DialogResult.OK)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
                {
                    foreach (string _file in Directory.GetFiles(dlg.SelectedPath, "*.*")
                                                      .Where(s => allowedVideoFiles.Contains("*" + Path.GetExtension(s).ToLower())))
                        addRowToHashTable(_file);
                    foreach (string dir in Directory.GetDirectories(dlg.SelectedPath))
                        try
                        {
                            foreach (string _file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                                                              .Where(s => allowedVideoFiles.Contains("*" + Path.GetExtension(s).ToLower())))
                                addRowToHashTable(_file);
                        }
                        catch (UnauthorizedAccessException) { }
                }));
            }
        }

        private void OnFileHashingProgress(object sender, FileHashingProgressArgs e)
        {
            double fileProg = e.ProcessedSize / e.TotalSize * 100;
            double totalProg = (e.ProcessedSize + ppSize) / totalQueueSize * 100;
            string remainingString = String.Empty;

            if (DateTime.Now.Subtract(m_hashingLastTime).TotalMilliseconds >= 1000)
            {
                TimeSpan remainingSpan = TimeSpan.FromSeconds((DateTime.Now.Subtract(m_hashingStartTime).TotalSeconds / (ppSize + e.ProcessedSize)) * (totalQueueSize - (ppSize + e.ProcessedSize)));
                remainingString = String.Format("Estimated time remaining: {0}h {1}m {2}s", Math.Round(remainingSpan.TotalHours), remainingSpan.Minutes, remainingSpan.Seconds);
                m_hashingLastTime = DateTime.Now;
            }

            m_Dispatcher.BeginInvoke(new Action(delegate
            {
                if (isHashing)
                {
                    if (remainingString != string.Empty)
                        timeRemainingTextBlock.Text = remainingString;

                    fileProgressBar.Value = fileProg;
                    totalProgressBar.Value = totalProg;
                }
            }));
        }

        private void FinishHash(HashItem item)
        {
            ppSize += item.Size;

            if (addToMyListCheckBox.IsChecked == true)
            {
                item.Viewed = watchedCheckBox.IsChecked == true ? 1 : 0;
                item.State = stateComboBox.SelectedIndex;

                aniDB.AddToMyList(item);
            }
        }

        private void debugListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (debugListBoxScrollViewer.VerticalOffset == debugListBoxScrollViewer.ScrollableHeight)
                debugListBoxScrollViewer.ScrollToBottom();
        }

        private void OnStateChanged(object sender, EventArgs args)
        {
            if (this.WindowState == WindowState.Minimized)
                this.Hide();
            else
                m_storedWindowState = this.WindowState;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = !this.IsVisible;
        }

        private void ImportList_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow import = new ImportWindow();
            import.Owner = this;
            import.ShowDialog();
        }

        private void OnTabCloseClick(object sender, RoutedEventArgs e)
        {
            Button s = (Button)sender;
            animeTabList.RemoveAll((x) => x.AnimeID == int.Parse(s.Tag.ToString()));
        }

        private void animeTabList_OnCountChanged(object sender, CountChangedArgs e)
        {
            if (e.oldCount == 1 && e.newCount == 0) // no more tabs
            {
                animeTabItem.Visibility = System.Windows.Visibility.Collapsed;
                ((TabItem)mainTabControl.Items[m_storedTabIndex]).Focus();
            }
            else
            {
                if (e.oldCount == 0 && e.newCount == 1) // first tab
                {
                    animeTabItem.Visibility = System.Windows.Visibility.Visible;
                    m_storedTabIndex = mainTabControl.SelectedIndex;
                }

                animeTabItem.Focus();
                animeTabControl.SelectedIndex = e.newCount - 1;
            }
        }

        private void AnimeURLClick(object sender, RoutedEventArgs e)
        {
            Image s_img = (Image)sender;
            System.Diagnostics.Process.Start(s_img.Tag.ToString());
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            aniDB.Logout();
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }

        #endregion

        #region Properties

        public string wTitle
        {
            get { return this.Title; }
            set
            {
                this.Title = value;
                m_notifyIcon.Text = value.Truncate(63, false, true);
            }
        }

        #endregion

    }
}
