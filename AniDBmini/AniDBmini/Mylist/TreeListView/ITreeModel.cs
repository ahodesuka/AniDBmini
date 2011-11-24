using System.Collections;
using System.ComponentModel;
using System.Windows.Controls;

namespace AniDBmini
{
    public interface ITreeModel
    {
        /// <summary>
        /// Get list of children of the specified parent
        /// </summary>
        IEnumerable GetChildren(object parent);

        /// <summary>
        /// Retrives children.
        /// </summary>
        void FetchChildren(object parent);

        /// <summary>
        /// Returns wheather specified parent has any children or not.
        /// </summary>
        bool HasChildren(object parent);

        /// <summary>
        /// Recreates the model.
        /// </summary>
        ITreeModel Refresh();

        /// <summary>
        /// Sorts the model.
        /// </summary>
        ITreeModel Sort(ListSortDirection lsd, DataGridColumn column);
    }
}
