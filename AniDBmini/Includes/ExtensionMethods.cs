using System;
using System.Globalization;
using System.Linq;

using AniDBmini.Collections;

namespace AniDBmini
{
    public static class ExtensionMethods
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static ThreadSafeObservableCollection<T> RemoveAll<T>(this ThreadSafeObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
                coll.Remove(itemToRemove);

            return coll;
        }

        public static DateTime UnixTimeToDateTime(string text)
        {
            double seconds = double.Parse(text, CultureInfo.InvariantCulture);
            return Epoch.AddSeconds(seconds);
        }

        public static void OpenWebPage(string url)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = GetDefaultBrowserPath();
            p.StartInfo.Arguments = url;
            p.Start();
        }

        private static string GetDefaultBrowserPath()
        {
            string key = @"HTTP\shell\open\command";
            using (Microsoft.Win32.RegistryKey registrykey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(key, false))
            {
                return ((string)registrykey.GetValue(null, null)).Split('"')[1];
            }
        }
    }
}
