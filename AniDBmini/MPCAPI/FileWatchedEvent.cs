using System;

namespace AniDBmini
{
    public class FileWatchedArgs : EventArgs
    {
        public string Path { get; private set; }

        public FileWatchedArgs(string f_Path)
        {
            Path = f_Path;
        }
    }

    public delegate void FileWatchedHandler(object sender, FileWatchedArgs args);
}
