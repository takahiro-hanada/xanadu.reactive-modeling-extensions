using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;

namespace Xanadu
{
    public static partial class ReactiveModelingExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection) => new ObservableCollection<T>(collection);

        public static ReadOnlyObservableCollection<T> ToReadOnlyObservableCollection<T>(this ObservableCollection<T> list) => new ReadOnlyObservableCollection<T>(list);

        public static T AddTo<T>(this T item, ICollection<IDisposable> collection) where T : IDisposable
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            collection.Add(item);
            
            return item;
        }

        public static T RemoveFrom<T>(this T item, ICollection<IDisposable> collection) where T : IDisposable
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            collection.Remove(item);
            
            return item;
        }

        public static IObservable<PropertyChangedEventArgs> ObservePropertyChanged(this INotifyPropertyChanged target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                h => (sender, e) => h(e),
                h => target.PropertyChanged += h,
                h => target.PropertyChanged -= h
                );
        }

        public static IObservable<Unit> ObservePropertyChanged<T, TProperty>(this T target, Expression<Func<T, TProperty>> propertySelector)
            where T : INotifyPropertyChanged
        {
            var propertyChanged = target.ObservePropertyChanged();

            if (propertySelector == null) throw new ArgumentNullException(nameof(propertySelector));

            var propertyName = ((MemberExpression)propertySelector.Body).Member.Name;

            return propertyChanged.Where(e => e.PropertyName == propertyName).Select(_ => Unit.Default);
        }

        public static IObservable<NotifyCollectionChangedEventArgs> ObserveCollectionChanged(this INotifyCollectionChanged target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => (sender, e) => h(e),
                h => target.CollectionChanged += h,
                h => target.CollectionChanged -= h
                );
        }

        // Not published yet.
        static IObservable<ListChangedEventArgs> ObserveListChanged(this IBindingList target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return Observable.FromEvent<ListChangedEventHandler, ListChangedEventArgs>(
                h => (sender, e) => h(e),
                h => target.ListChanged += h,
                h => target.ListChanged -= h
                );
        }

        public static IObservable<TProperty> ObserveProperty<T, TProperty>(this T target, Expression<Func<T, TProperty>> propertySelector)
            where T : INotifyPropertyChanged
        {
            var propertyChanged = target.ObservePropertyChanged(propertySelector);

            var getter = propertySelector.Compile();

            return propertyChanged.Select(_ => getter(target)).StartWith(getter(target));
        }

        // public static IObservable<CollectionChangedReactiveArgs<T>> ObserveCollectionChangedReactive<T, TCollection>(this TCollection target)
        //     where TCollection : IEnumerable<T>, INotifyCollectionChanged
        public static IObservable<CollectionChangedReactiveArgs<T>> ObserveCollectionChangedReactive<T>(this IEnumerable<T> target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            else if (target is INotifyCollectionChanged notifyCollectionChanged)
                return ObserveCollectionChangedReactive(target, notifyCollectionChanged);

            else if (target is IBindingList bindingList)
                return ObserveCollectionChangedReactive(target, bindingList);

            else
                throw new ArgumentOutOfRangeException(nameof(target));
        }

        static IObservable<CollectionChangedReactiveArgs<T>> ObserveCollectionChangedReactive<T>(IEnumerable<T> ts, INotifyCollectionChanged ntc) => ntc
            .ObserveCollectionChanged()
            .Select(e =>
            {
                var buffer = ts.ToArray();

                var infos = default(CollectionChangedReactiveArgs<T>[]);

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:

                        infos = Enumerable
                            .Range(e.NewStartingIndex, e.NewItems.Count)
                            .Select((sourceIndex, changedIndex) => new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Insert, sourceIndex, (T)e.NewItems[changedIndex]))
                            .ToArray();
                        break;

                    case NotifyCollectionChangedAction.Remove:

                        infos = Enumerable
                            .Range(e.OldStartingIndex, e.OldItems.Count)
                            .Select((sourceIndex, changedIndex) => new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Remove, sourceIndex, (T)e.OldItems[changedIndex]))
                            .Reverse()
                            .ToArray();
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        {
                            var olds = Enumerable
                                .Range(e.OldStartingIndex, e.OldItems.Count)
                                .Select((index, changedItemsIndex) => new { index, item = (T)e.OldItems[changedItemsIndex] });

                            var news = Enumerable
                                .Range(e.NewStartingIndex, e.NewItems.Count)
                                .Select((index, changedItemsIndex) => new { index, item = (T)e.NewItems[changedItemsIndex] });

                            infos = olds.Join(news, o => o.index, n => n.index, (o, n) => new { o.index, oldItem = o.item, newItem = n.item })
                                .Select(on => new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Replace, on.index, (T)on.oldItem, (T)on.newItem))
                                .ToArray();

                            break;
                        }
                    case NotifyCollectionChangedAction.Move:
                        {
                            var olds = Enumerable
                                .Range(e.OldStartingIndex, e.OldItems.Count)
                                .Select((index, changedItemsIndex) => new { index, changedItemsIndex, item = (T)e.OldItems[changedItemsIndex] });

                            var news = Enumerable
                                .Range(e.NewStartingIndex, e.NewItems.Count)
                                .Select((index, changedItemsIndex) => new { index, changedItemsIndex, item = (T)e.NewItems[changedItemsIndex] });

                            var q =
                                from o in olds
                                join n in news on o.changedItemsIndex equals n.changedItemsIndex
                                select new[]
                                {
                                    new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Remove, o.index, o.item),
                                    new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Insert, n.index, n.item),
                                };

                            infos = q.SelectMany(array => array).ToArray();

                            break;
                        }
                }

                return new { buffer, infos };
            })
            .StartWith(new { buffer = ts.ToArray(), infos = default(CollectionChangedReactiveArgs<T>[]) })
            .Scan((o, n) => new
            {
                n.buffer,
                infos = n.infos ?? o.buffer.Select((item, index) => new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Remove, index, item)).Reverse().ToArray()
            })
            .Select(o => o.infos ?? new CollectionChangedReactiveArgs<T>[0])
            .SelectMany(o => o.ToObservable())
            ;

        static IObservable<CollectionChangedReactiveArgs<T>> ObserveCollectionChangedReactive<T>(IEnumerable<T> ts, IBindingList bl) => bl
            .ObserveListChanged()
            .Select(args => new { args, buffer = ts.ToArray(), bufferPrevious = default(T[])})
            .StartWith(new { args = default(ListChangedEventArgs), buffer = ts.ToArray(), bufferPrevious = default(T[]) })
            .Scan((previous, current) => new { current.args, current.buffer, bufferPrevious = previous.buffer })
            .Skip(1)
            .Select(o =>
            {
                switch (o.args.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                        return new[]
                        {
                            new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Insert, o.args.NewIndex  , o.buffer[o.args.NewIndex] )
                        };

                    case ListChangedType.ItemDeleted:
                        return new[]
                        {
                            new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Remove, o.args.NewIndex, o.bufferPrevious[o.args.NewIndex])
                        };

                    case ListChangedType.ItemChanged:
                        return new[]
                        {
                            new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Replace
                                , o.args.NewIndex
                                , o.bufferPrevious[o.args.NewIndex]
                                , o.buffer[o.args.NewIndex]
                                )
                        };

                    case ListChangedType.ItemMoved:
                        return new[]
                        {
                            new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Remove
                                , o.args.OldIndex
                                , o.bufferPrevious[o.args.OldIndex]
                                ),
                            new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Insert
                                , o.args.NewIndex
                                , o.buffer[o.args.NewIndex]
                                ),
                        };

                    case ListChangedType.Reset:
                        return o.bufferPrevious.Select((item, index) => new CollectionChangedReactiveArgs<T>(CollectionChangedReactiveAction.Remove, index, item)).Reverse().ToArray();

                    default:
                        return new CollectionChangedReactiveArgs<T>[0];
                }
            })
            .SelectMany(o => o.ToObservable())
            .Where(o => o != null);
    }
}
