
#region Using Statements

using System;
using System.Collections.Generic;

#endregion Using Statements

namespace AniDBmini.Collections
{
    public class MylistEntry
    {

        #region Properties

        private List<MylistEntry> children = new List<MylistEntry>();
        public List<MylistEntry> Children { get { return children; } }

        public bool HasChildren { get; private set; }

        public int ID { get; private set; }

        public string Col0 { get; private set; }
        public string Col1 { get; private set; }
        public string Col2 { get; private set; }
        public string Col3 { get; private set; }
        public string Col4 { get; private set; }
        public string Col5 { get; private set; }

        public object OriginalEntry { get; set; }

        #endregion Properties

        #region Constructors

        public static MylistEntry FromAnime(AnimeEntry entry)
        {
            MylistEntry m_entry = new MylistEntry();
            TimeSpan length = TimeSpan.FromSeconds(entry.length);

            m_entry.OriginalEntry = entry;
            m_entry.ID = entry.aid;
            m_entry.HasChildren = entry.eps_have > 0;

            m_entry.Col0 = entry.title;
            m_entry.Col1 = String.Format("{0}/{1}{2}", entry.eps_have,
                                                       (entry.eps_total == 0) ? "TBC" : entry.eps_total.ToString(),
                                                       (entry.spl_have > 0) ? String.Format("+{0}", entry.spl_have) : null);
            m_entry.Col2 = String.Format("{0}/{1}{2}", entry.eps_watched,
                                                       entry.eps_have,
                                                       (entry.spl_watched > 0) ? String.Format("+{0}", entry.spl_watched) : null);
            m_entry.Col3 = entry.year;
            m_entry.Col4 = length.ToFormatedLength();
            m_entry.Col5 = entry.size.ToFormattedBytes();

            return m_entry;
        }

        public static MylistEntry FromEpisode(EpisodeEntry entry)
        {
            MylistEntry m_entry = new MylistEntry();
            TimeSpan length = TimeSpan.FromSeconds(entry.length);

            m_entry.OriginalEntry = entry;
            m_entry.ID = entry.eid;
            m_entry.HasChildren = entry.hasFiles;

            m_entry.Col0 = String.Format("{0}: {1}", (entry.spl_epno == null) ? entry.epno.ToString() : entry.spl_epno, entry.english);
            m_entry.Col2 = entry.watched ? "Yes" : "No";

            if (entry.airdate != null)
                m_entry.Col3 = entry.airdate.ToDateTime(false).ToShortDateString();

            m_entry.Col4 = length.ToFormatedLength();
            m_entry.Col5 = entry.size.ToFormattedBytes();

            return m_entry;
        }

        public static MylistEntry FromFile(FileEntry entry)
        {
            MylistEntry m_entry = new MylistEntry();
            TimeSpan length = TimeSpan.FromSeconds(entry.length);

            m_entry.OriginalEntry = entry;
            m_entry.ID = entry.fid;

            string fileInfo = String.Empty;

            if (!string.IsNullOrEmpty(entry.vres) || !string.IsNullOrEmpty(entry.source) ||
                !string.IsNullOrEmpty(entry.vcodec) || !string.IsNullOrEmpty(entry.acodec))
                fileInfo = String.Format(" ({0} {1} {2} {3})", entry.vres, entry.source, entry.vcodec, "- " + entry.acodec);

            if (!entry.generic)
                m_entry.Col0 = String.Format("[{0}]{1}", entry.Group.group_abbr, fileInfo.Replace("  ", " "));
            else
                m_entry.Col0 = "generic file";

            if (entry.watcheddate != null)
                m_entry.Col2 = String.Format("{0} {1}", entry.watcheddate.ToDateTime().ToShortDateString(),
                                                        entry.watcheddate.ToDateTime().ToShortTimeString());
            m_entry.Col4 = length.ToFormatedLength();
            m_entry.Col5 = entry.size.ToFormattedBytes();

            return m_entry;
        }

        #endregion Constructors

    }
}
