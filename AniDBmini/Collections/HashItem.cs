using System;

namespace AniDBmini.Collections
{
    public class HashItem : IEquatable<HashItem>
    {
        private string hash, name, path;
        private double size;
        private int state, viewed;
        private bool edit;

        public HashItem(string _path)
        {
            path = _path;
            name = System.IO.Path.GetFileName(path);
            size = new System.IO.FileInfo(path).Length;
        }

        public bool Equals(HashItem other)
        {
            if (path == other.Path && size == other.Size && name == other.Name)
                return true;
            else
                return false;
        }

        public string Hash { get { return hash; } set { hash = value; } }
        public string Name { get { return name; } }
        public string Path { get { return path; } }

        public double Size { get { return size; } }

        public int State { get { return state; } set { state = value; } }
        public int Viewed { get { return viewed; } set { viewed = value; } }

        public bool Edit { get { return edit; } set { edit = value; } }
    }
}