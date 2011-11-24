
#region Using Statments

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public static class ExtensionMethods
    {
        public enum BYTE_UNIT
        {
            B,
            KB,
            MB,
            GB,
            TB,
            PB,
            EB,
            ZB,
            YB
        };

        /// <summary>
        /// Returns a given value of a given object.
        /// </summary>
        public static object GetPropValue(object src, string propName)
        {
            System.Reflection.PropertyInfo pi;
            if ((pi = src.GetType().GetProperty(propName)) != null)
                return pi.GetValue(src, null);

            return null;
        }

        public static TSObservableCollection<T> RemoveAll<T>(this TSObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
                coll.Remove(itemToRemove);

            return coll;
        }

        /// <summary>
        /// Used for estimated time remaining.
        /// </summary>
        public static string ToHMS(this TimeSpan ts)
        {
            return String.Format("{0}h {1}m {2}s", (int)ts.TotalHours, ts.Minutes.ToString("00"), ts.Seconds.ToString("00"));
        }

        public static string ToFormatedLength(this TimeSpan ts)
        {
            return String.Format("{0}{1}m", ((int)ts.TotalHours > 0) ? String.Format("{0}h ", (int)ts.TotalHours) : null,
                                            ts.Minutes.ToString("00"));
        }

        /// <summary>
        /// Simplifies an amount of bytes to the smallest unit.
        /// </summary>
        public static string ToFormattedBytes(this double size)
        {
            int unit = 0;

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }

            return String.Format("{0:0.#} {1}", size, (BYTE_UNIT)unit);
        }

        /// <summary>
        /// Convert bytes to a specific unit.
        /// </summary>
        public static string ToFormattedBytes(this double size, BYTE_UNIT outUnit)
        {
            int unitIndex = (int)outUnit;

            for (int i = 0; i < unitIndex; ++i)
                size /= 1024;

            return String.Format("{0:0.#} {1}", size, outUnit);
        }

        /// <summary>
        /// Convert from a certain unit to another.
        /// </summary>
        public static string ToFormattedBytes(this double size, BYTE_UNIT inUnit, BYTE_UNIT outUnit)
        {
            int unitIndex = ((int)outUnit > (int)inUnit) ?
                            (int)outUnit - (int)inUnit : (int)inUnit - (int)outUnit;

            for (int i = 0; i < unitIndex; ++i)
                size /= 1024;

            return String.Format("{0:0.##} {1}", size, outUnit);
        }

        public static string FormatNullable(this string str)
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

        public static string FormatAudioCodec(string ac)
        {
            switch (ac)
            {
                case "WMA (also DivX Audio)":
                    return "WMA";
                case "Vorbis (Ogg Vorbis)":
                    return "Vorbis";
                default:
                    return ac;
            }
        }

        public static string FormatVideoCodec(string vc)
        {
            switch (vc)
            {
                case "DivX UNK":
                    return "DivX";
                case "DivX5 (also DivX6)":
                    return "DivX5/6";
                case "RealVideo Other":
                case "RealVideo 9/10 (also RV40)":
                    return "RealVideo";
                case "MS MP4x (also WMV1/2)":
                    return "MS MP4x";
                case "WMV9 (also WMV3)":
                    return "WMV9";
                default:
                    return vc;
            }
        }
    }
}
