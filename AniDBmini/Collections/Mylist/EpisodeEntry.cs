using System.Collections.Generic;

namespace AniDBmini.Collections
{
    public class EpisodeEntry
    {

        #region Fields

        public int eid, epno;
        public string type, english, nihongo, romaji, airdate;
        public bool watched;

        public List<FileEntry> Files = new List<FileEntry>();

        #endregion Fields

        #region Constructors

        public EpisodeEntry() { }

        #endregion Constructors

    }
}
