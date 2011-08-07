namespace AniDBmini.Collections
{
    public class FileEntry
    {

        #region Fields

        public int lid, gid, length;
        public double size;
        public string ed2k, addeddate, watcheddate, source, vcodec, acodec, group_name, group_abbr;
        public bool watched, generic;

        #endregion Fields

        #region Constructors

        public FileEntry() { }

        #endregion Constructors

    }
}
