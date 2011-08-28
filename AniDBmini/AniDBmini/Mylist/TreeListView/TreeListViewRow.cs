
#region Using Statements

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class TreeListViewRow : DataGridRow,
                                   INotifyPropertyChanged
    {

        #region Properties

        private TreeNode _node;
        public TreeNode Node
        {
            get { return _node; }
            internal set
            {
                _node = value;
                OnPropertyChanged("Node");
            }
        }

        #endregion

        #region Protected Overrides

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Node != null)
            {
                switch (e.Key)
                {
                    case Key.Right:
                        e.Handled = true;
                        if (!Node.IsExpanded)
                        {
                            Node.IsExpanded = true;
                            ChangeFocus(Node);
                        }
                        else if (Node.Children.Count > 0)
                            ChangeFocus(Node.Children[0]);
                        break;
                    case Key.Left:
                        e.Handled = true;
                        if (Node.IsExpanded && Node.IsExpandable)
                        {
                            Node.IsExpanded = false;
                            ChangeFocus(Node);
                        }
                        else
                            ChangeFocus(Node.Parent);
                        break;
                    case Key.Subtract:
                        e.Handled = true;
                        Node.IsExpanded = false;
                        ChangeFocus(Node);
                        break;

                    case Key.Add:
                        e.Handled = true;
                        Node.IsExpanded = true;
                        ChangeFocus(Node);
                        break;
                    case Key.Multiply:
                        e.Handled = true;
                        ExpandAll(Node);
                        ChangeFocus(Node);
                        break;
                }
            }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (Node != null)
            {
                string keyChar = e.Text;
                TreeNode tn = Node.Tree.Root.Children.FirstOrDefault(x => x != Node && x.Index > Node.Index &&
                    (x.Tag as MylistEntry).Col0.StartsWith(keyChar, StringComparison.CurrentCultureIgnoreCase));

                if (tn != null || (tn = Node.Tree.Root.Children.FirstOrDefault(x => x != Node &&
                        (x.Tag as MylistEntry).Col0.StartsWith(keyChar, StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    tn.Tree.ScrollIntoView(tn);
                    e.Handled = true;
                }
            }

            if (!e.Handled)
                base.OnTextInput(e);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (Node != null && FindAncestor<RowExpander>(e.OriginalSource as DependencyObject) == null)
                Node.Tree.SetIsExpanded(Node, !Node.IsExpanded, true);
            else
                base.OnMouseDoubleClick(e);
        }

        #endregion Protected Overrides

        #region Private Methods

        private void ExpandAll(TreeNode node)
        {
            node.IsExpanded = true;
            foreach (TreeNode child in node.Children)
                ExpandAll(child);
        }

        private void ChangeFocus(TreeNode node)
        {
            var tree = node.Tree;
            if (tree != null)
            {
                var item = tree.ItemContainerGenerator.ContainerFromItem(node) as TreeListViewRow;

                if (item != null)
                    item.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                else
                    tree.PendingFocusNode = node;
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                    return (T)current;

                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }

        #endregion Private Methods

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion INotifyPropertyChanged

    }
}