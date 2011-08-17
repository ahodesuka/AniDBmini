
#region Using Statements

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using System.Xml.XPath;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public partial class ImportWindow : Window
    {

        #region Fields

        private BackgroundWorker xmlWorker;
        private string xmlPath;
        private bool closePending, isBackup, isWorking;

        private MylistDB m_myList;
        private Dispatcher uiDispatcher = Dispatcher.CurrentDispatcher;

        #endregion Fields

        #region Constructor

        public ImportWindow(MylistDB myList)
        {
            m_myList = myList;
            InitializeComponent();
        }

        #endregion Constructor

        #region Private Methods

        private double formatSize(string size)
        {
            return double.Parse(size.Replace(".", null));
        }

        #endregion

        #region Events

        private void BrowseOnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.Filter = "Xml Files|*.xml|Tar Files|*.tgz;*.tar";

			Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                importFilePath.Text = dlg.FileName;
                importStart.IsEnabled = true;
            }
        }

        private void StartOnClick(object sender, RoutedEventArgs e)
        {
            xmlPath = importFilePath.Text;
            xmlWorker = new BackgroundWorker();
            xmlWorker.WorkerSupportsCancellation = true;

            xmlWorker.DoWork += new DoWorkEventHandler(DoWork);
            xmlWorker.RunWorkerCompleted += (s, _e) =>
            {
                isWorking = false;
                if (!_e.Cancelled && MessageBox.Show("Importing finised!", "Status", MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
                {
                    if (isBackup)
                        File.Delete(MylistDB.dbPath + ".bak");

                    m_myList.Entries.Clear();
                    m_myList.SelectEntries();

                    this.DialogResult = true;
                }
                else
                    importStart.IsEnabled = true;
            };

            if (File.Exists(MylistDB.dbPath))
                if (MessageBox.Show("A mylist database file already exists.\nDo you wish to overwrite it?", "Confirm",
                                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    m_myList.Close();
                    try
                    {
                        File.Move(MylistDB.dbPath, MylistDB.dbPath + ".bak");
                        isBackup = true;
                    }
                    catch (IOException) { }
                    finally { File.Delete(MylistDB.dbPath); }
                }
                else
                    return;

            xmlWorker.RunWorkerAsync();

            isWorking = true;
            importStart.IsEnabled = false;
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            int totalProcessedFiles = 0,
            totalFiles = int.Parse(new XPathDocument(xmlPath).CreateNavigator().Evaluate("count(//file)").ToString());

            using (XmlReader reader = XmlReader.Create(xmlPath))
            {
                reader.ReadToFollowing("mylist");
                if (reader["template"] != "mini")
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        MessageBox.Show("Please ensure you selected a mylist export file that used the xml-mini template.", "Invalid xml template!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }));

                    xmlWorker.CancelAsync();
                    return;
                }

                m_myList.Create();

                List<int> m_groupList = new List<int>();
                List<MylistEntry> m_list = new List<MylistEntry>();

                // <anime>
                while (reader.ReadToFollowing("anime"))
                {
                    while (closePending) Thread.Sleep(500);

                    MylistEntry entry = new MylistEntry();

                    entry.aid = int.Parse(reader["aid"]);
                    entry.type = reader["type"];
                    entry.year = reader["year"];

                    // <titles>
                    reader.ReadToFollowing("default");
                    entry.title = reader.ReadElementContentAsString();
                    uiDispatcher.BeginInvoke(new Action<string>(str => { importFilePath.Text = str; }), "Importing: " + entry.title);

                    reader.ReadToFollowing("nihongo");
                    entry.nihongo = reader.ReadElementContentAsString().FormatNullable();

                    reader.ReadToFollowing("english");
                    entry.english = reader.ReadElementContentAsString().FormatNullable();
                    // </titles>

                    // <episodes>
                    if (!reader.ReadToFollowing("episodes"))
                        goto Finish;

                    entry.eps_total = int.Parse(reader["total"]);

                    XmlReader episodesReader = reader.ReadSubtree();
                    // <episode>
                    while (episodesReader.ReadToFollowing("episode"))
                    {
                        while (closePending) Thread.Sleep(500);

                        EpisodeEntry episode = new EpisodeEntry();

                        episode.eid = int.Parse(episodesReader["eid"]);

                        episode.airdate = episodesReader["aired"] == "-" ? null :
                                          ExtensionMethods.DateTimeToUnixTime(DateTime.Parse(episodesReader["aired"],
                                                                                             System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"))).ToString();
                        episode.watched = Convert.ToBoolean(int.Parse(episodesReader["watched"]));

                        if (Regex.IsMatch(episodesReader["epno"].Substring(0, 1), @"\D"))
                        {
                            episode.spl_epno = episodesReader["epno"];

                            entry.spl_have++;
                            if (episode.watched)
                                entry.spl_watched++;
                        }
                        else
                        {
                            episode.epno = int.Parse(episodesReader["epno"]);

                            entry.eps_have++;
                            if (episode.watched)
                                entry.eps_watched++;
                        }

                        // <titles>
                        episodesReader.ReadToDescendant("english");
                        episode.english = episodesReader.ReadElementContentAsString();

                        episodesReader.ReadToFollowing("nihongo");
                        episode.nihongo = episodesReader.ReadElementContentAsString().FormatNullable();

                        episodesReader.ReadToFollowing("romaji");
                        episode.romaji = episodesReader.ReadElementContentAsString().FormatNullable();
                        // </titles>                        

                        // <files>
                        if (!episodesReader.ReadToFollowing("files"))
                            goto Finish;

                        XmlReader filesReader = episodesReader.ReadSubtree();
                        // <file>
                        while (filesReader.ReadToFollowing("file"))
                        {
                            while (closePending) Thread.Sleep(500);

                            FileEntry file = new FileEntry();

                            file.fid = int.Parse(filesReader["fid"]);
                            file.lid = int.Parse(filesReader["lid"]);
                            file.watcheddate = filesReader["watched"] == "-" ? null :
                                               ExtensionMethods.DateTimeToUnixTime(DateTime.Parse(episodesReader["watched"],
                                                                                                  System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"))).ToString();
                            file.watched = file.watcheddate != null;
                            file.generic = episodesReader["generic"] != null;

                            if (!file.generic) // generic entries do not have this information
                            {
                                if (filesReader["gid"] != null)
                                    file.gid = int.Parse(filesReader["gid"]);

                                file.ed2k = filesReader["ed2k"];
                                file.length = double.Parse(filesReader["length"]);
                                file.size = double.Parse(filesReader["size"]);
                                file.source = filesReader["source"].FormatNullable();
                                file.acodec = filesReader["acodec"].FormatNullable();
                                file.vcodec = filesReader["vcodec"].FormatNullable();
                                file.vres = filesReader["vres"].FormatNullable();
                                
                                if (file.gid != 0 && !m_groupList.Contains(file.gid))
                                {
                                    // <group_name>
                                    filesReader.ReadToFollowing("group_name");
                                    string group_name = filesReader.ReadElementContentAsString();
                                    // </group_name>

                                    // <group_abbr>
                                    filesReader.ReadToFollowing("group_abbr");
                                    string group_abbr = filesReader.ReadElementContentAsString();
                                    // </group_abbr>

                                    m_myList.InsertGroup(file.gid, group_name, group_abbr);
                                    m_groupList.Add(file.gid);
                                }
                            }

                            episode.Files.Add(file);
                            totalProcessedFiles++;
                            importProgressBar.Dispatcher.BeginInvoke(new Action<double, double>((total, processed) =>
                            {
                                importProgressBar.Value = Math.Ceiling(processed / total * 100);
                            }), totalFiles, totalProcessedFiles);
                        // </file>
                        }
                        // </files>
                        filesReader.Close();
                        entry.Episodes.Add(episode);
                    // </episode>
                    }                    
                    // </episodes>
                    episodesReader.Close();

                Finish:
                    m_myList.InsertMylistEntryFromImport(entry);
                // </anime>
                }
             // </mylist>
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (!isWorking && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!isWorking && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string filePath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                FileInfo fi = new FileInfo(filePath);

                if (fi.Extension == ".xml" || fi.Extension == ".tgz" || fi.Extension == ".tar")
                {
                    importFilePath.Text = filePath;
                    importStart.IsEnabled = true;
                }

                e.Handled = true;
            }
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            if (isWorking)
            {
                closePending = true;

                if (MessageBox.Show("Are you sure?\nClosing this window will abort the import process.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                {
                    closePending = false;
                    e.Cancel = true;
                }
                else if (File.Exists(MylistDB.dbPath))
                {
                    m_myList.Entries.Clear();

                    File.Delete(MylistDB.dbPath);

                    if (isBackup)
                        File.Delete(MylistDB.dbPath + ".bak");
                }
            }
        }

        #endregion Events

    }
}