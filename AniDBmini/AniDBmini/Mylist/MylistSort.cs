
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
            int result;

            switch ((string)column.Header)
            {
                case "Title":
                    myCompare = (a, b) => { return a.title.CompareTo(b.title) * dir; };
                    break;
                case "Eps":
                    myCompare = (a, b) =>
                    {
                        result = a.eps_have.CompareTo(b.eps_have);
                        if (result == 0) result = a.eps_total.CompareTo(b.eps_total);
                        if (result == 0) result = a.spl_have.CompareTo(b.spl_have);
                        if (result == 0) result = a.title.CompareTo(b.title) * dir;
                        return result * dir;
                    };
                    break;
                case "Watched":
                    myCompare = (a, b) =>
                    {
                        result = a.eps_watched.CompareTo(b.eps_watched);
                        if (result == 0) result = a.eps_total.CompareTo(b.eps_total);
                        if (result == 0) result = a.spl_watched.CompareTo(b.spl_watched);
                        if (result == 0) result = a.title.CompareTo(b.title) * dir;
                        return result * dir;
                    };
                    break;
                case "Year":
                    myCompare = (a, b) =>
                    {
                        result = a.year.CompareTo(b.year);
                        if (result == 0) result = a.title.CompareTo(b.title) * dir;
                        return result * dir;
                    };
                    break;
                case "Length":
                    myCompare = (a, b) =>
                    {
                        result = a.length.CompareTo(b.length);
                        if (result == 0) result = a.title.CompareTo(b.title) * dir;
                        return result * dir;
                    };
                    break;
                case "Size":
                    myCompare = (a, b) =>
                    {
                        result = a.size.CompareTo(b.size);
                        if (result == 0) result = a.title.CompareTo(b.title) * dir;
                        return result * dir;
                    };
                    break;
            }
        }

        int IComparer.Compare(object X, object Y)
        {
            return myCompare((MylistEntry)X, (MylistEntry)Y);
        }
    }
}
