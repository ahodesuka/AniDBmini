
#region Using Statements

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;

using AniDBmini;

#endregion Using Statements

namespace AniDBmini.Collections
{
    public class EpisodeEntry : IEquatable<EpisodeEntry>,
                               INotifyPropertyChanged
	{

        #region Properties

        public int eid { get; set; }
        public int epno { get; set; }

        public double length { get; set; }
        public double size { get; set; }

        public string spl_epno { get; set; }
        public string english { get; set; }
        public string nihongo { get; set; }
        public string romaji { get; set; }
        public string airdate { get; set; }

        public bool watched { get; set; }
        public bool genericOnly { get; set; }

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

        public ObservableCollection<FileEntry> Files { get; set; }

        #endregion Properties

        #region Constructors

        public EpisodeEntry()
        {
            Files = new ObservableCollection<FileEntry>();
        }

        public EpisodeEntry(SQLiteDataReader reader)
        {
            eid = int.Parse(reader["eid"].ToString());
            
            if ((spl_epno = reader["spl_epno"].ToString().FormatNullable()) == null)
                epno = int.Parse(reader["epno"].ToString());

            length = double.Parse(reader["length"].ToString());
            size = double.Parse(reader["size"].ToString());

            english = reader["english"].ToString();
            nihongo = !string.IsNullOrEmpty(reader["nihongo"].ToString()) ?
                      reader["nihongo"].ToString() : null;
            romaji = !string.IsNullOrEmpty(reader["romaji"].ToString()) ?
                     reader["romaji"].ToString() : null;

            airdate = !string.IsNullOrEmpty(reader["airdate"].ToString()) ?
                      ExtensionMethods.UnixTimeToDateTime(reader["airdate"].ToString()).ToShortDateString() : null;

            watched = Convert.ToBoolean(int.Parse(reader["watched"].ToString()));
            genericOnly = Convert.ToBoolean(int.Parse(reader["genericOnly"].ToString()));
        }

        #endregion Constructors

        #region IEquatable

        public bool Equals(EpisodeEntry other)
        {
            return eid == other.eid;
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
