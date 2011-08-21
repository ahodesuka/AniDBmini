
#region Using Statements

using System;
using System.Collections.Generic;
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
        public int aid { get; set; }
        public int epno { get; set; }

        public double length { get; set; }
        public double size { get; set; }

        public string spl_epno { get; set; }
        public string english { get; set; }
        public string nihongo { get; set; }
        public string romaji { get; set; }

        public UnixTimestamp airdate { get; set; }

        public bool watched { get; set; }
        public bool hasFiles { get; set; }

        private List<FileEntry> _files = new List<FileEntry>();
        public List<FileEntry> Files { get { return _files; } }

        #endregion Properties

        #region Constructors

        public EpisodeEntry() { }

        public EpisodeEntry(SQLiteDataReader reader)
        {
            eid = int.Parse(reader["eid"].ToString());
            aid = int.Parse(reader["aid"].ToString());
            
            if ((spl_epno = reader["spl_epno"].ToString().FormatNullable()) == null)
                epno = int.Parse(reader["epno"].ToString());

            length = double.Parse(reader["length"].ToString());
            size = double.Parse(reader["size"].ToString());

            english = reader["english"].ToString();
            nihongo = !string.IsNullOrEmpty(reader["nihongo"].ToString()) ?
                      reader["nihongo"].ToString() : null;
            romaji = !string.IsNullOrEmpty(reader["romaji"].ToString()) ?
                     reader["romaji"].ToString() : null;

            if (!string.IsNullOrEmpty(reader["airdate"].ToString()))
                airdate = double.Parse(reader["airdate"].ToString());

            watched = Convert.ToBoolean(int.Parse(reader["watched"].ToString()));
            hasFiles = Convert.ToBoolean(int.Parse(reader["hasFiles"].ToString()));
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
        protected void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion

    }
}
