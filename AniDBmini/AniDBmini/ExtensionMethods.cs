
#region Using Statments

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public static class ExtensionMethods
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

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

        public static double DateTimeToUnixTime(DateTime date)
        {
            TimeSpan span = date - Epoch;
            return span.TotalSeconds;
        }

        public static string ToFormatedString(this TimeSpan ts)
        {
            return String.Format("{0}h {1}m {2}s", (int)ts.TotalHours, ts.Minutes.ToString("00"), ts.Seconds.ToString("00"));
        }

        public static string ToFormatedStringSimple(this TimeSpan ts)
        {
            return String.Format("{0}h {1}m", (int)ts.TotalHours, ts.Minutes.ToString("00"));
        }

        public static string ToFormatedStringSimplify(this TimeSpan ts)
        {
            string time = String.Empty;

            if ((int)ts.TotalHours > 0)
                time += String.Format("{0}h ", (int)ts.TotalHours);

            return String.Format("{0}{1}m", time, ts.Minutes.ToString("00"));
        }

        public static string ToFormatedBytes(this double size)
        {
            int unit = 0;

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }

            return String.Format("{0:0.#} {1}", size, units[unit]);
        }

        public static string ToFormatedBytes(this double size, string bUnit)
        {
            int unitIndex;

            if ((unitIndex = Array.IndexOf(units, bUnit)) == -1)
                return size.ToFormatedBytes();

            for (int i = 0; i < unitIndex; ++i)
                size /= 1024;
            
            return String.Format("{0:0.#} {1}", size, units[unitIndex]);
        }

        public static string formatNullable(this string str)
        {
            return string.IsNullOrWhiteSpace(str) || str == "unknown" || str == "0x0" ? null : str;
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

        public static T FindAncestor<T>(this DependencyObject obj) where T : DependencyObject
        {
            return obj.FindAncestor(typeof(T)) as T;
        }

        public static DependencyObject FindAncestor(this DependencyObject obj, Type ancestorType)
        {
            if (obj is Visual)
            {
                var tmp = VisualTreeHelper.GetParent(obj);
                while (tmp != null && !ancestorType.IsAssignableFrom(tmp.GetType()))
                    tmp = VisualTreeHelper.GetParent(tmp);

                return tmp;
            }

            return null;
        }

        public static T FindChild<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            return parent.FindChild<T>(child => true);
        }

        public static T FindChild<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return parent.FindChildren<T>(predicate).First();
        }

        public static IEnumerable<T> FindChildren<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            var children = new List<DependencyObject>();

            if (parent is Visual)
            {
                var visualChildrenCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int childIndex = 0; childIndex < visualChildrenCount; childIndex++)
                    children.Add(VisualTreeHelper.GetChild(parent, childIndex));
            }
            foreach (var logicalChild in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                if (!children.Contains(logicalChild))
                    children.Add(logicalChild);

            foreach (var child in children)
            {
                var typedChild = child as T;
                if ((typedChild != null) && predicate.Invoke(typedChild))
                    yield return typedChild;

                foreach (var foundDescendant in FindChildren(child, predicate))
                    yield return foundDescendant;
            }
            yield break;
        }
    }
}
