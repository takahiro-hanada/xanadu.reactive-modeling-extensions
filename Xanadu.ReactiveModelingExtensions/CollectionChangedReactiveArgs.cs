using System;

namespace Xanadu
{
    public sealed class CollectionChangedReactiveArgs<T>
    {
        public CollectionChangedReactiveAction Action { get; }

        public int Index { get; }

        public T OldItem { get; }

        public T NewItem { get; }

        CollectionChangedReactiveArgs(CollectionChangedReactiveAction action, int index)
        {
            if (action == CollectionChangedReactiveAction.None) throw new ArgumentOutOfRangeException(nameof(action));
            
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            Action = action;

            Index = index;
        }

        public CollectionChangedReactiveArgs(CollectionChangedReactiveAction action, int index, T item) : this(action, index)
        {
            switch (action)
            {
                case CollectionChangedReactiveAction.Insert:

                    NewItem = item;

                    break;

                case CollectionChangedReactiveAction.Remove:

                    OldItem = item;

                    break;

                default:

                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public CollectionChangedReactiveArgs(CollectionChangedReactiveAction action, int index, T oldItem, T newItem) : this(action, index)
        {
            if (action == CollectionChangedReactiveAction.Replace)
            {
                OldItem = oldItem;

                NewItem = newItem;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
    }
}
