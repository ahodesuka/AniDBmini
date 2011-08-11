using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AniDBmini.Collections
{
    public class MylistEntry : IEquatable<MylistEntry>
    {

        #region Properties

        public int aid { get; set; }
        public int eps_total { get; set; }
        public int eps_my { get; set; }
        public int eps_watched { get; set; }

        public double size { get; set; }

        public string type { get; set; }
        public string title { get; set; }
        public string nihongo { get; set; }
        public string english { get; set; }
        public string startdate { get; set; }
        public string enddate { get; set; }

        public bool complete { get; set; }
        public bool watched { get; set; }

        public List<EpisodeEntry> Episodes { get; set; }

        #endregion Fields

        #region Constructors

        public MylistEntry() { }

        public MylistEntry(SQLiteDataReader reader)
        {
            aid = int.Parse(reader["aid"].ToString());
            type = reader["type"].ToString();

            title = reader["title"].ToString();
            nihongo = reader["nihongo"].ToString();
            english = reader["english"].ToString();

            //startdate = reader["startdate"].ToString();
            //enddate = reader["enddate"].ToString();

            eps_total = int.Parse(reader["eps_total"].ToString());
            eps_my = int.Parse(reader["eps_my"].ToString());
            eps_watched = int.Parse(reader["eps_watched"].ToString());

            //complete = Convert.ToBoolean(int.Parse(reader["complete"].ToString()));
            //watched = Convert.ToBoolean(int.Parse(reader["watched"].ToString()));

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
