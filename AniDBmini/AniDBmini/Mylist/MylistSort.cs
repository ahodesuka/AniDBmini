
#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class MylistSort : IComparer<MylistEntry>
    {
        private delegate int TwoArgDelegate(AnimeEntry arg1, AnimeEntry arg2);
        private TwoArgDelegate m_listCompare;

        public MylistSort(ListSortDirection direction, DataGridColumn column)
        {
            int dir = (direction == ListSortDirection.Ascending) ? 1 : -1;
            int result = 0;

            m_listCompare = (a, b) =>
            {
                switch (column.Header as string)
                {
                    case "Title":
                        return String.Compare(a.title, b.title, CultureInfo.CurrentCulture, CompareOptions.OrdinalIgnoreCase) * dir;
                    case "Eps":
                        result = a.eps_have.CompareTo(b.eps_have);
                        if (result == 0) result = a.eps_total.CompareTo(b.eps_total);
                        if (result == 0) result = a.spl_have.CompareTo(b.spl_have);
                        if (result == 0) goto Title;
                        return result * dir;
                    case "Watched":
                        result = a.eps_watched.CompareTo(b.eps_watched);
                        if (result == 0) result = a.eps_total.CompareTo(b.eps_total);
                        if (result == 0) result = a.spl_watched.CompareTo(b.spl_watched);
                        if (result == 0) goto Title;
                        return result * dir;
                    case "Year":
                        result = a.year.CompareTo(b.year);
                        if (result == 0) goto Title;
                        return result * dir;
                    case "Length":
                        result = a.length.CompareTo(b.length);
                        if (result == 0) goto Title;
                        return result * dir;
                    case "Size":
                        result = a.size.CompareTo(b.size);
                        if (result == 0) goto Title;
                        return result * dir;
                }

            Title:
                return String.Compare(a.title, b.title, CultureInfo.CurrentCulture, CompareOptions.OrdinalIgnoreCase) * 1;

            };
        }

        int IComparer<MylistEntry>.Compare(MylistEntry X, MylistEntry Y)
        {
            return m_listCompare(X.OriginalEntry as AnimeEntry, 
                Y.OriginalEntry as AnimeEntry);
        }
    }
}
