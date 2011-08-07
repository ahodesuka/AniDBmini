using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace AniDBmini.Collections
{
    public class CountChangedArgs : EventArgs
    {
        public int newCount { get; private set; }
        public int oldCount { get; private set; }

        public CountChangedArgs(int n_Count, int o_Count)
        {
            newCount = n_Count;
            oldCount = o_Count;
        }
    }

    public delegate void CountChangedHandler(object sender, CountChangedArgs args);

	public class TSObservableCollection<T> : ObservableCollection<T>,
                                                     INotifyPropertyChanged
	{
        public event CountChangedHandler OnCountChanged = delegate { };

		private SynchronizationContext SynchronizationContext;
        private int collectionCount;

		public TSObservableCollection()
		{
			SynchronizationContext = SynchronizationContext.Current;

			// current synchronization context will be null if we're not in UI Thread
			if (SynchronizationContext == null)
				throw new InvalidOperationException("This collection must be instantiated from UI Thread, if not, you have to pass SynchronizationContext to constructor.");
		}

		public TSObservableCollection(SynchronizationContext synchronizationContext)
		{
			if (synchronizationContext == null)
				throw new ArgumentNullException("synchronizationContext");

			this.SynchronizationContext = synchronizationContext;
		}

        private int CollectionCount
        {
            set
            {
                OnCountChanged(this, new CountChangedArgs(value, collectionCount));
                collectionCount = value;
            }
        }

        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (collectionCount != this.Count)
                CollectionCount = this.Count;
        }

		protected override void ClearItems()
		{
			this.SynchronizationContext.Send(new SendOrPostCallback((param) => base.ClearItems()), null);
		}

		protected override void InsertItem(int index, T item)
		{
			if (!this.Contains(item))
				this.SynchronizationContext.Send(new SendOrPostCallback((param) => base.InsertItem(index, item)), null);
		}

		protected override void RemoveItem(int index)
		{
			this.SynchronizationContext.Send(new SendOrPostCallback((param) => base.RemoveItem(index)), null);
		}

		protected override void SetItem(int index, T item)
		{
			this.SynchronizationContext.Send(new SendOrPostCallback((param) => base.SetItem(index, item)), null);
		}

		protected override void MoveItem(int oldIndex, int newIndex)
		{
			this.SynchronizationContext.Send(new SendOrPostCallback((param) => base.MoveItem(oldIndex, newIndex)), null);
		}
	}
}
