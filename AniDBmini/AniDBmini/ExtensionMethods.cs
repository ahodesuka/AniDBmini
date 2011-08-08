
#region Using Statments

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public static class ExtensionMethods
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static TSObservableCollection<T> RemoveAll<T>(this TSObservableCollection<T> coll, Func<T, bool> condition)
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

        public static string Truncate(this string s, int length, bool atWord, bool addEllipsis)
        {
            if (s == null || s.Length <= length)
                return s;

            length -= addEllipsis ? 3 : 0;
            string s2 = s.Substring(0, length);

            if (atWord)
            {
                List<char> alternativeCutOffs = new List<char>() { ' ', ',', '.', '?', '/', ':', ';', '\'', '\"', '\'', '-' };

                int lastSpace = s2.LastIndexOf(' ');

                if (lastSpace != -1 && (s.Length >= length + 1 && !alternativeCutOffs.Contains(s.ToCharArray()[length])))
                    s2 = s2.Remove(lastSpace);
            }

            if (addEllipsis)
                s2 += "...";

            return s2;
        }
    }
}
