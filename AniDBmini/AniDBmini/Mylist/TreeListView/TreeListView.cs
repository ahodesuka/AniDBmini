
#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class TreeListView : DataGrid
    {

        #region Properties

        /// <summary>
        /// Internal collection of rows representing visible nodes, actually displayed in the ListView
        /// </summary>
        internal ObservableCollectionAdv<TreeNode> Rows
        {
            get;
            private set;
        }


        private ITreeModel _model;
        public ITreeModel Model
        {
            get { return _model; }
            set
            {
                if (_model != value)
                {
                    StoreNodes();

                    _model = value;
                    _root.Children.Clear();
                    Rows.Clear();
                    CreateChildrenNodes(_root);

                    RestoreNodes();
                }
            }
        }

        private TreeNode _root;
        internal TreeNode Root
        {
            get { return _root; }
        }

        public ReadOnlyCollection<TreeNode> Nodes
        {
            get { return Root.Nodes; }
        }

        internal TreeNode PendingFocusNode
        {
            get;
            set;
        }

        public ICollection<TreeNode> SelectedNodes
        {
            get { return SelectedItems.Cast<TreeNode>().ToArray(); }
        }

        public TreeNode SelectedNode
        {
            get
            {
                if (SelectedItems.Count > 0)
                    return SelectedItems[0] as TreeNode;
                else
                    return null;
            }
            set { SelectedItem = value; }
        }

        private List<TreeNode> s_ExpandedNodes = new List<TreeNode>();
        private TreeNode s_SelectedNode;

        #endregion Properties

        #region Constructor

        public TreeListView()
        {
            Rows = new ObservableCollectionAdv<TreeNode>();
            _root = new TreeNode(this, null);
            _root.IsExpanded = true;
            ItemsSource = Rows;
            ItemContainerGenerator.StatusChanged += ItemContainerGeneratorStatusChanged;
        }

        #endregion Constructor

        #region Protected Overrides

        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeListViewRow container = new TreeListViewRow();
            Binding isSelectedBinding = new Binding("IsSelected");
            isSelectedBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(container, TreeListViewRow.IsSelectedProperty, isSelectedBinding);
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewRow;
        }

        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            ListSortDirection sortDirection = ListSortDirection.Ascending;
            Nullable<ListSortDirection> currentSortDirection = eventArgs.Column.SortDirection;

            if (currentSortDirection.HasValue &&
            currentSortDirection.Value == ListSortDirection.Ascending)
                sortDirection = ListSortDirection.Descending;

            foreach (DataGridColumn c in this.Columns)
                c.SortDirection = null;

            eventArgs.Column.SortDirection = sortDirection;
            this.Model = this.Model.Sort(sortDirection, eventArgs.Column);
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            var ti = (TreeListViewRow)element;
            var node = (TreeNode)item;
            ti.Node = node;
            base.PrepareContainerForItemOverride(element, node);
        }

        #endregion Protected Overrides

        #region Private Methods

        private void ItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && PendingFocusNode != null)
            {
                var item = ItemContainerGenerator.ContainerFromItem(PendingFocusNode) as TreeListViewRow;
                if (item != null) item.Focus();
                PendingFocusNode = null;
            }
        }

        private IEnumerable GetChildren(TreeNode parent)
        {
            if (Model != null)
                return Model.GetChildren(parent.Tag);
            else
                return null;
        }

        private bool HasChildren(TreeNode parent)
        {
            if (parent == Root)
                return true;
            else if (Model != null)
                return Model.HasChildren(parent.Tag);
            else
                return false;
        }

        private void CreateChildrenRows(TreeNode node)
        {
            int index = Rows.IndexOf(node);

            if (index >= 0 || node == _root) // ignore invisible nodes
            {
                var nodes = node.AllVisibleChildren.ToArray();
                Rows.InsertRange(index + 1, nodes);
            }
        }

        private void StoreNodes()
        {
            s_ExpandedNodes = Rows.Where(x => x.IsExpanded).ToList();
            s_SelectedNode = this.SelectedNode;
        }

        private void RestoreNodes()
        {
            foreach (TreeNode node in s_ExpandedNodes)
            {
                TreeNode tn = Rows.FirstOrDefault(x =>
                (x.Tag as MylistEntry).OriginalEntry.GetType() == (node.Tag as MylistEntry).OriginalEntry.GetType() &&
                (x.Tag as MylistEntry).ID == (node.Tag as MylistEntry).ID);

                if (tn != null)
                    SetIsExpanded(tn, true);
            }

            if (s_SelectedNode != null &&
            (s_SelectedNode = Rows.FirstOrDefault(x =>
            (x.Tag as MylistEntry).OriginalEntry.GetType() == (s_SelectedNode.Tag as MylistEntry).OriginalEntry.GetType() &&
            (x.Tag as MylistEntry).ID == (s_SelectedNode.Tag as MylistEntry).ID)) != null)
                ScrollIntoView(s_SelectedNode);
        }

        #endregion Private Methods

        #region Internal Methods

        internal void Refresh()
        {
            this.Model = this.Model.Refresh();
        }

        internal new void ScrollIntoView(object item)
        {
            this.SelectedItem = item;
            base.ScrollIntoView(item, this.ColumnFromDisplayIndex(0));

            Dispatcher.BeginInvoke(new Action(delegate
            {
                while (this.SelectedItem == null)
                    this.SelectedItem = item;

                TreeListViewRow tlItem = this.ItemContainerGenerator.ContainerFromIndex(this.SelectedIndex) as TreeListViewRow;
                tlItem.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }));
        }

        internal void SetIsExpanded(TreeNode node, bool value)
        {
            if (value)
            {
                if (!node.IsChildrenFetched && node != Root)
                {
                    this.Model.FetchChildren(node.Tag);
                    node.IsChildrenFetched = true;
                    node.AssignIsExpanded(value);

                    CreateChildrenNodes(node);
                    ScrollIntoView(node);
                }
                else
                {
                    node.AssignIsExpanded(value);
                    CreateChildrenRows(node);

                    if (node != Root)
                        ScrollIntoView(node);
                }
            }
            else
            {
                DropChildrenRows(node, false);
                node.AssignIsExpanded(value);
                ScrollIntoView(node);
            }
        }

        internal void CreateChildrenNodes(TreeNode node)
        {
            var children = GetChildren(node);
            if (children != null)
            {
                int rowIndex = Rows.IndexOf(node);
                node.ChildrenSource = children as INotifyCollectionChanged;

                foreach (object obj in children)
                {
                    TreeNode child = new TreeNode(this, obj);
                    child.HasChildren = HasChildren(child);
                    node.Children.Add(child);
                }

                Rows.InsertRange(rowIndex + 1, node.Children.ToArray());
            }

            node.HasChildren = HasChildren(node);
        }

        internal void DropChildrenRows(TreeNode node, bool removeParent)
        {
            int start = Rows.IndexOf(node);
            if (start >= 0 || node == _root) // ignore invisible nodes
            {
                int count = node.VisibleChildrenCount;
                if (removeParent)
                    count++;
                else
                    start++;

                Rows.RemoveRange(start, count);
            }
        }

        internal void InsertNewNode(TreeNode parent, object tag, int rowIndex, int index)
        {
            TreeNode node = new TreeNode(this, tag);
            if (index >= 0 && index < parent.Children.Count)
                parent.Children.Insert(index, node);
            else
            {
                index = parent.Children.Count;
                parent.Children.Add(node);
            }

            Rows.Insert(rowIndex + index + 1, node);
        }

        #endregion Internal Methods

    }
}