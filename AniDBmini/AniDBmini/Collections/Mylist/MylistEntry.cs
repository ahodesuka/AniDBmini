using System;
using System.Collections.Generic;

namespace AniDBmini.Collections
{
    public class MylistEntry : IEquatable<MylistEntry>
    {

        #region Fields

        public int aid, eps_total, eps_my, eps_watched;
        public double size;
        public string type, title, nihongo, english, startdate, enddate;
        public bool complete, watched;

        public List<EpisodeEntry> Episodes = new List<EpisodeEntry>();

        #endregion Fields

        #region Constructors

        public MylistEntry() { }

        #endregion Constructors

        #region IEquatable

        public bool Equals(MylistEntry other)
        {
            return aid == other.aid;
        }

        #endregion IEquatable

    }
}
