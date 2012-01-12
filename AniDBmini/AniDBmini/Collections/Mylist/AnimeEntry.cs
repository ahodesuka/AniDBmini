
#region Using Statements

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

#endregion Using Statements

namespace AniDBmini.Collections
{
    public class AnimeEntry : IEquatable<AnimeEntry>,
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
        public string romaji { get; set; }
        public string nihongo { get; set; }
        public string english { get; set; }
        public string year { get; set; }

        public bool complete { get; set; }

        private List<EpisodeEntry> _episodes = new List<EpisodeEntry>();
        public List<EpisodeEntry> Episodes
        {
            get { return _episodes; }
        }

        public string title
        {
            get
            {
                switch (MainWindow.animeLang)
                {
                    case "english":
                        if (!string.IsNullOrEmpty(english)) return english;
                        else goto default;
                    case "nihongo":
                        if (!string.IsNullOrEmpty(nihongo)) return nihongo;
                        else goto default;
                    default:
                        return romaji;
                }
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Create an empty entry, used while importing.
        /// </summary>
        public AnimeEntry() { }

        /// <summary>
        /// Create a AnimeEntry from a SQLiteDataReader.
        /// </summary>
        public AnimeEntry(SQLiteDataReader reader)
        {
            aid = int.Parse(reader["aid"].ToString());
            type = reader["type"].ToString();

            length = double.Parse(reader["length"].ToString());
            size = double.Parse(reader["size"].ToString());

            romaji = reader["romaji"].ToString();
            nihongo = reader["nihongo"].ToString();
            english = reader["english"].ToString();

            year = reader["year"].ToString();

            eps_total = int.Parse(reader["eps_total"].ToString());
            eps_have = int.Parse(reader["eps_have"].ToString());
            spl_have = int.Parse(reader["spl_have"].ToString());
            eps_watched = int.Parse(reader["eps_watched"].ToString());
            spl_watched = int.Parse(reader["spl_watched"].ToString());

            complete = (eps_total == eps_have && eps_have > 0);
        }

        #endregion Constructors

        #region IEquatable

        public bool Equals(AnimeEntry other)
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
