
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

        private BackgroundWorker XmlWorker;
        private string xmlPath;
        private bool closePending, isWorking;

        private Dispatcher uiDispatcher = Dispatcher.CurrentDispatcher;

        #endregion Fields

        #region Constructor

        public ImportWindow()
        {
            InitializeComponent();
        }

        #endregion Constructor

        #region Private Methods

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            MylistLocal myList = new MylistLocal();
            myList.Create();

            List<int> m_groupList = new List<int>();
            List<MylistEntry> m_list = new List<MylistEntry>();

            int totalProcessedFiles = 0,
                totalFiles = int.Parse(new XPathDocument(xmlPath).CreateNavigator().Evaluate("count(//file)").ToString());

            using (XmlReader reader = XmlReader.Create(xmlPath))
            {
                reader.ReadToFollowing("mylist");
                while (reader.ReadToFollowing("anime"))
                {
                    while (closePending) { Thread.Sleep(500); }

                    MylistEntry entry = new MylistEntry();

                    entry.aid = int.Parse(reader["aid"]);
                    entry.type = reader["type"];

                    reader.ReadToFollowing("default"); // <titles>
                    entry.title = reader.ReadElementContentAsString();
                    uiDispatcher.BeginInvoke(new Action<string>(str => { importFilePath.Text = str; }), "Importing: " + entry.title);

                    reader.ReadToFollowing("nihongo");
                    entry.nihongo = reader.ReadElementContentAsString();
                    entry.nihongo = string.IsNullOrWhiteSpace(entry.nihongo) ? null : entry.nihongo;

                    reader.ReadToFollowing("english");
                    entry.english = reader.ReadElementContentAsString();
                    entry.english = string.IsNullOrWhiteSpace(entry.english) ? null : entry.english;

                    reader.Read();
                    reader.ReadToFollowing("date"); // <date/>
                    entry.startdate = reader["start"];
                    entry.enddate = reader["end"] == "?" ? null : reader["end"];

                    reader.ReadToFollowing("eps"); // <eps/>
                    entry.eps_total = int.Parse(reader["total"]);
                    entry.eps_my = int.Parse(reader["my"]);
                    entry.eps_watched = int.Parse(reader["watched"]);

                    reader.ReadToFollowing("status"); // <status/>
                    entry.complete = Convert.ToBoolean(int.Parse(reader["complete"]));
                    entry.watched = Convert.ToBoolean(int.Parse(reader["watched"]));
                    entry.size = formatSize(reader["size"]);

                    if (!reader.ReadToFollowing("episodes")) // <episodes>
                        goto Finish;

                    XmlReader episodesReader = reader.ReadSubtree();
                    while (episodesReader.ReadToFollowing("episode"))
                    {
                        while (closePending) { Thread.Sleep(500); }

                        EpisodeEntry episode = new EpisodeEntry();
                                
                        episode.eid = int.Parse(episodesReader["eid"]);
                        if (Regex.IsMatch(episodesReader["epno"].Substring(0, 1), @"\D"))
                        {
                            episode.epno = int.Parse(episodesReader["epno"].Substring(1));
                            episode.type = episodesReader["epno"].Substring(0, 1);
                        }
                        else
                            episode.epno = int.Parse(episodesReader["epno"]);

                        episodesReader.ReadToDescendant("english"); // <titles>
                        episode.english = episodesReader.ReadElementContentAsString();

                        episodesReader.ReadToFollowing("nihongo");
                        episode.nihongo = episodesReader.ReadElementContentAsString();
                        episode.nihongo = string.IsNullOrWhiteSpace(episode.nihongo) ? null : episode.nihongo;

                        episodesReader.ReadToFollowing("romaji");
                        episode.romaji = episodesReader.ReadElementContentAsString();
                        episode.romaji = string.IsNullOrWhiteSpace(episode.romaji) ? null : episode.romaji;

                        episodesReader.ReadToFollowing("date"); // <date/>
                        episode.airdate = episodesReader["aired"] == "-" ? null : episodesReader["aired"];

                        episodesReader.ReadToFollowing("status"); // <status/>
                        episode.watched = Convert.ToBoolean(int.Parse(episodesReader["watched"]));

                        if (!episodesReader.ReadToFollowing("files")) // <files>
                            goto Finish;

                        XmlReader filesReader = episodesReader.ReadSubtree();
                        while (filesReader.ReadToFollowing("file"))
                        {
                            while (closePending) { Thread.Sleep(500); }

                            FileEntry file = new FileEntry();

                            file.lid = int.Parse(filesReader["lid"]);
                            file.addeddate = filesReader["added"];
                            file.watcheddate = filesReader["watched"] == "-" ? null : filesReader["watched"];
                            file.watched = file.watcheddate != null;
                            file.generic = episodesReader["generic"] == null ? false : true;

                            if (!file.generic) // generic entries do not have this information
                            {
                                if (filesReader["gid"] != null)
                                    file.gid = int.Parse(filesReader["gid"]);

                                file.ed2k = filesReader["ed2k"];
                                file.length = int.Parse(filesReader["length"]);
                                file.size = int.Parse(filesReader["lid"]);
                                file.source = formatNullable(filesReader["source"]);
                                file.vcodec = formatNullable(filesReader["vcodec"]);
                                file.acodec = formatNullable(filesReader["acodec"]);

                                if (file.gid != 0 && !m_groupList.Contains(file.gid)) // add the group if it has not been added yet
                                {
                                    filesReader.ReadToFollowing("group_name");
                                    string group_name = filesReader.ReadElementContentAsString();

                                    filesReader.ReadToFollowing("group_abbr");
                                    string group_abbr = filesReader.ReadElementContentAsString();

                                    myList.InsertGroup(file.gid, group_name, group_abbr);
                                    m_groupList.Add(file.gid);
                                }
                            }

                            episode.Files.Add(file);
                            totalProcessedFiles++;
                            importProgressBar.Dispatcher.BeginInvoke(new Action<double, double>((total, processed) =>
                            {
                                importProgressBar.Value = Math.Ceiling(processed / total * 100);
                            }), totalFiles, totalProcessedFiles);
                        }

                        filesReader.Close();
                        entry.Episodes.Add(episode);                        
                    }

                    episodesReader.Close();

                Finish:
                    myList.InsertMylistEntry(entry);
                }
            }
        }

        private string formatNullable(string str)
        {
            return string.IsNullOrWhiteSpace(str) || str == "unknown" ? null : str;
        }

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
            XmlWorker = new BackgroundWorker();

            XmlWorker.DoWork += new DoWorkEventHandler(DoWork);
            XmlWorker.RunWorkerCompleted += (s, _e) =>
            {
                isWorking = false;
                if (MessageBox.Show("Importing finised!", "Status", MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
                    this.Close();
            };

            if (File.Exists(MylistLocal.dbPath))
                if (MessageBox.Show("A mylist database file already exists.\nDo you wish to overwrite it?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                    File.Delete(MylistLocal.dbPath);
                else
                    return;

            XmlWorker.RunWorkerAsync();

            isWorking = true;
            importStart.IsEnabled = false;
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
                else if (File.Exists(MylistLocal.dbPath))
                    File.Delete(MylistLocal.dbPath);
            }
        }

        #endregion Events

    }
}
