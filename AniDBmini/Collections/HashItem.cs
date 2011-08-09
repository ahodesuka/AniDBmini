using System;

namespace AniDBmini.Collections
{
    public class HashItem : IEquatable<HashItem>
    {
        public string Hash { get; set; }
        public string Name { get; private set; }
        public string Path { get; private set; }

        public double Size { get; private set; }

        public int State { get; set; }
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