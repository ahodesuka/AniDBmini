
#region Using Statments

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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

        public static T FindAncestor<T>(this DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                T o = obj as T;
                if (o != null)
                    return o;

                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        /// <summary>
        /// Find the first child of the specified type (the child must exist)
        /// by walking down the logical/visual trees
        /// Will throw an exception if a matching child does not exist. If you're not sure, use the TryFindChild method instead.
        /// </summary>
        /// <typeparam name="T">The type of child you want to find</typeparam>
        /// <param name="parent">The dependency object whose children you wish to scan</param>
        /// <returns>The first descendant of the specified type</returns>
        /// <remarks> usage: myWindow.FindChild<StackPanel>() </StackPanel></remarks>
        public static T FindChild<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            return parent.FindChild<T>(child => true);
        }

        /// <summary>
        /// Find the first child of the specified type (the child must exist)
        /// by walking down the logical/visual trees, which meets the specified criteria
        /// Will throw an exception if a matching child does not exist. If you're not sure, use the TryFindChild method instead.
        /// </summary>
        /// <typeparam name="T">The type of child you want to find</typeparam>
        /// <param name="parent">The dependency object whose children you wish to scan</param>
        /// <param name="predicate">The child object is selected if the predicate evaluates to true</param>
        /// <returns>The first matching descendant of the specified type</returns>
        /// <remarks> usage: myWindow.FindChild<StackPanel>( child => child.Name == "myPanel" ) </StackPanel></remarks>
        public static T FindChild<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return parent.FindChildren<T>(predicate).First();
        }

        /// <summary>
        /// Use this overload if the child you're looking may not exist.
        /// </summary>
        /// <typeparam name="T">The type of child you're looking for</typeparam>
        /// <param name="parent">The dependency object whose children you wish to scan</param>
        /// <param name="foundChild">out param - the found child dependencyobject, null if the method returns false</param>
        /// <returns>True if a child was found, else false</returns>
        public static bool TryFindChild<T>(this DependencyObject parent, out T foundChild)
            where T : DependencyObject
        {
            return parent.TryFindChild<T>(child => true, out foundChild);
        }

        /// <summary>
        /// Use this overload if the child you're looking may not exist.
        /// </summary>
        /// <typeparam name="T">The type of child you're looking for</typeparam>
        /// <param name="parent">The dependency object whose children you wish to scan</param>
        /// <param name="predicate">The child object is selected if the predicate evaluates to true</param>
        /// <param name="foundChild">out param - the found child dependencyobject, null if the method returns false</param>
        /// <returns>True if a child was found, else false</returns>
        public static bool TryFindChild<T>(this DependencyObject parent, Func<T, bool> predicate, out T foundChild)
            where T : DependencyObject
        {
            foundChild = null;
            var results = parent.FindChildren<T>(predicate);
            if (results.Count() == 0)
                return false;

            foundChild = results.First();
            return true;
        }

        /// <summary>
        /// Get a list of descendant dependencyobjects of the specified type and which meet the criteria
        /// as specified by the predicate
        /// </summary>
        /// <typeparam name="T">The type of child you want to find</typeparam>
        /// <param name="parent">The dependency object whose children you wish to scan</param>
        /// <param name="predicate">The child object is selected if the predicate evaluates to true</param>
        /// <returns>The first matching descendant of the specified type</returns>
        /// <remarks> usage: myWindow.FindChildren<StackPanel>( child => child.Name == "myPanel" ) </StackPanel></remarks>
        public static IEnumerable<T> FindChildren<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            var children = new List<DependencyObject>();

            if ((parent is Visual) || (parent is Visual3D))
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
