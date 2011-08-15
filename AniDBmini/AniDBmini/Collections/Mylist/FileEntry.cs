using System;
using System.Data.SQLite;

namespace AniDBmini.Collections
{
    public class FileEntry : IEquatable<FileEntry>
    {

        #region Fields

        public int lid { get; set; }
        public int gid { get; set; }

        public double length { get; set; }
        public double size { get; set; }

        public string ed2k { get; set; }
        public string watcheddate { get; set; }
        public string source { get; set; }
        public string acodec { get; set; }
        public string vcodec { get; set; }
        public string vres { get; set; }
        public string group_name { get; set; }
        public string group_abbr { get; set; }
        public string path { get; set; }

        public bool watched { get; set; }
        public bool generic { get; set; }

        #endregion Fields

        #region Constructors

        public FileEntry() { }

        /// <summary>
        /// Used for an entry that is to be inserted into the database
        /// from a recently hashed HashItem.
        /// </summary>
        /// <param name="item"></param>
        public FileEntry(HashItem item)
        {
            ed2k = item.Hash;
            size = item.Size;

            if (item.State == 1)
                path = item.Path;

            watched = Convert.ToBoolean(item.Viewed);
            generic = false;
        }

        public FileEntry(SQLiteDataReader reader)
        {
            lid = int.Parse(reader["lid"].ToString());
            generic = Convert.ToBoolean(int.Parse(reader["generic"].ToString()));

            if (!generic)
            {
                gid = int.Parse(reader["gid"].ToString());

                ed2k = reader["ed2k"].ToString();

                length = int.Parse(reader["length"].ToString());
                size = double.Parse(reader["size"].ToString());

                source = reader["source"].ToString();
                acodec = reader["acodec"].ToString();
                vcodec = reader["vcodec"].ToString();
                vres = reader["vres"].ToString();
                group_name = reader["group_name"].ToString();
                group_abbr = reader["group_abbr"].ToString();
                path =  !string.IsNullOrEmpty(reader["path"].ToString()) ?
                        reader["path"].ToString() : null;
            }

            watcheddate = !string.IsNullOrEmpty(reader["watcheddate"].ToString()) ?
                          ExtensionMethods.UnixTimeToDateTime(reader["watcheddate"].ToString()).ToShortDateString() + " " +
                          ExtensionMethods.UnixTimeToDateTime(reader["watcheddate"].ToString()).ToShortTimeString() : null;

            watched = watcheddate != null;
        }

        #endregion Constructors

        #region IEquatable

        public bool Equals(FileEntry other)
        {
            return lid == other.lid;
        }

        #endregion IEquatable

    }
}
