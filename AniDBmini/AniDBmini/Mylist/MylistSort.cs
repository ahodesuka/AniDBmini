
#region Using Statements

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Controls;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class MylistSort : IComparer
    {
        private delegate int TwoArgDelegate(MylistEntry arg1, MylistEntry arg2);
        private TwoArgDelegate myCompare;

        public MylistSort(ListSortDirection direction, DataGridColumn column)
        {
            int dir = (direction == ListSortDirection.Ascending) ? 1 : -1;

            myCompare = (a, b) =>
            {
                int result;
                switch ((string)column.Header)
                {
                    case "Title":
                        return a.title.CompareTo(b.title) * dir;
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
                    default:
                        return 0;
                }

                Title:
                    return a.title.CompareTo(b.title) * 1;
            };
        }

        int IComparer.Compare(object X, object Y)
        {
            return myCompare((MylistEntry)X, (MylistEntry)Y);
        }
    }
}
