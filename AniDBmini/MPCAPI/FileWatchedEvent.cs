using System;
using AniDBmini.Collections;

namespace AniDBmini
{
    public class FileWatchedArgs : EventArgs
    {
        public HashItem Item { get; private set; }

        public FileWatchedArgs(string f_Path)
        {
            Item = new HashItem(f_Path);
        }
    }

    public delegate void FileWatchedHandler(object sender, FileWatchedArgs args);
}
