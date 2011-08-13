using System;

namespace AniDBmini.Collections
{
    public class HashItem : IEquatable<HashItem>
    {
        public string Hash { get; set; }
        public string Name { get; private set; }
        public string Path { get; private set; }

        public double Size { get; private set; }

        /// <summary>
        /// <para>0 - unknown - state is unknown or the user doesn't want to provide this information</para>
        /// <para>1 - on hdd - the file is stored on hdd (but is not shared)</para>
        /// <para>2 - on cd - the file is stored on cd</para>
        /// <para>3 - deleted - the file has been deleted or is not available for other reasons (i.e. reencoded)</para>
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// <para>0 - unwatched</para>
        /// <para>1 - watched</para>
        /// </summary>
        public int Viewed { get; set; }

        public bool Edit { get; set; }

        public HashItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
            Size = new System.IO.FileInfo(path).Length;
        }

        #region IEquatable

        public bool Equals(HashItem other)
        {
            return Path == other.Path;
        }

        #endregion IEquatable
    }
}