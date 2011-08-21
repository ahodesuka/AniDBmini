
#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class MylistModel : ITreeModel
    {

        #region SortDescription

        private struct SortDescription
        {
            public ListSortDirection direction;
            public DataGridColumn column;
        };

        #endregion

        #region Properties

        private MylistDB m_myList;
        private SortDescription m_currentSort;

        private List<MylistEntry> _root = new List<MylistEntry>();
        public List<MylistEntry> Root { get { return _root; } }

        #endregion Properties

        #region Constructor

        public MylistModel(MylistDB myList)
        {
            m_myList = myList;

            foreach (AnimeEntry a in m_myList.SelectEntries())
                this.Root.Add(MylistEntry.FromAnime(a));
        }

        #endregion Constructor

        #region ITreeModel

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                return Root;
            else
                return (parent as MylistEntry).Children;
        }

        public void FetchChildren(object parent)
        {
            MylistEntry entry = parent as MylistEntry;

            if (entry.OriginalEntry is AnimeEntry)
                foreach (EpisodeEntry e in m_myList.SelectEpisodes(entry.ID))
                    entry.Children.Add(MylistEntry.FromEpisode(e));
            else if (entry.OriginalEntry is EpisodeEntry)
                foreach (FileEntry f in m_myList.SelectFiles(entry.ID))
                    entry.Children.Add(MylistEntry.FromFile(f));
        }

        public bool HasChildren(object parent)
        {
            return (parent as MylistEntry).HasChildren;
        }

        public ITreeModel Refresh()
        {
            if (m_currentSort.column != null)
                return this.Sort(m_currentSort.direction, m_currentSort.column);

            return new MylistModel(m_myList);
        }

        public ITreeModel Sort(ListSortDirection lsd, DataGridColumn column)
        {
            MylistModel model = new MylistModel(m_myList);
            m_currentSort = new SortDescription { direction = lsd, column = column };

            model.Root.Sort(new MylistSort(lsd, column));

            return model;
        }

        #endregion ITreeModel

    }
}
