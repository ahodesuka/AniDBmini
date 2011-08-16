
#region Using Statements

using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;

#endregion Using Statements

namespace AniDBmini.Collections
{
    public class MylistEntry : IEquatable<MylistEntry>
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

        public ObservableCollection<EpisodeEntry> Episodes { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Create an empty entry, used while importing.
        /// </summary>
        public MylistEntry()
        {
            Episodes = new ObservableCollection<EpisodeEntry>();
        }

        /// <summary>
        /// Create a mylistentry from a SQLiteDataReader.
        /// </summary>
        /// <param name="reader"></param>
        public MylistEntry(SQLiteDataReader reader)
        {
            aid = int.Parse(reader["aid"].ToString());
            type = reader["type"].ToString();

            title = reader["title"].ToString();
            nihongo = reader["nihongo"].ToString();
            english = reader["english"].ToString();

            year = reader["year"].ToString();

            eps_total = int.Parse(reader["eps_total"].ToString());
            eps_have = int.Parse(reader["eps_have"].ToString());
            spl_have = int.Parse(reader["spl_have"].ToString());
            eps_watched = int.Parse(reader["eps_watched"].ToString());
            spl_watched = int.Parse(reader["spl_watched"].ToString());

            size = double.Parse(reader["size"].ToString());
        }

        #endregion Constructors

        #region IEquatable

        public bool Equals(MylistEntry other)
        {
            return aid == other.aid;
        }

        #endregion IEquatable

    }
}
