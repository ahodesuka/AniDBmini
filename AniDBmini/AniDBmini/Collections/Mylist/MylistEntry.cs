
#region Using Statements

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;

#endregion Using Statements

namespace AniDBmini.Collections
{
    public class MylistEntry : IEquatable<MylistEntry>,
                               INotifyPropertyChanged
	{

        #region Properties

        public int aid { get; set; }
        public int eps_total { get; set; }
        public int eps_have { get; set; }
        public int spl_have { get; set; }
        public int eps_watched { get; set; }
        public int spl_watched { get; set; }

        public double length { get; set; }
        public double size { get; set; }

        public string type { get; set; }
        public string title { get; set; }
        public string nihongo { get; set; }
        public string english { get; set; }
        public string year { get; set; }

        public bool complete { get; set; }
        public bool watched { get; set; }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    RaisePropertyChanged("IsExpanded");
                }
            }
        }

        private bool isFetched;
        public bool IsFetched
        {
            get { return isFetched; }
            set
            {
                if (isFetched != value)
                {
                    isFetched = value;
                    RaisePropertyChanged("IsFetched");
                }
            }
        }

        private ObservableCollection<EpisodeEntry> episodes = new ObservableCollection<EpisodeEntry>();
        public ObservableCollection<EpisodeEntry> Episodes
        {
            get { return episodes; }
            set
            {
                if (episodes != value)
                {
                    episodes = value;
                    RaisePropertyChanged("Episodes");
                }
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Create an empty entry, used while importing.
        /// </summary>
        public MylistEntry() { }

        /// <summary>
        /// Create a mylistentry from a SQLiteDataReader.
        /// </summary>
        /// <param name="reader"></param>
        public MylistEntry(SQLiteDataReader reader)
        {
            aid = int.Parse(reader["aid"].ToString());
            type = reader["type"].ToString();

            length = double.Parse(reader["length"].ToString());
            size = double.Parse(reader["size"].ToString());

            title = reader["title"].ToString();
            nihongo = reader["nihongo"].ToString();
            english = reader["english"].ToString();

            year = reader["year"].ToString();

            eps_total = int.Parse(reader["eps_total"].ToString());
            eps_have = int.Parse(reader["eps_have"].ToString());
            spl_have = int.Parse(reader["spl_have"].ToString());
            eps_watched = int.Parse(reader["eps_watched"].ToString());
            spl_watched = int.Parse(reader["spl_watched"].ToString());
        }

        #endregion Constructors

        #region IEquatable

        public bool Equals(MylistEntry other)
        {
            return aid == other.aid;
        }

        #endregion IEquatable

        #region INotifyPropertyChanged

        /// <summary>
        /// event for INotifyPropertyChanged.PropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// raise the PropertyChanged event
        /// </summary>
        /// <param name="propName"></param>
        protected void RaisePropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion

    }
}
