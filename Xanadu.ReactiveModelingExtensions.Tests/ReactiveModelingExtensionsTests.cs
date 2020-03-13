using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using Xunit;

namespace Xanadu
{
    public class ReactiveModelingExtensionsTests
    {
        [Fact]
        public void ObservePropertyChangedTest()
        {
            const string newName = "Jack";

            var person = new PersonX();

            var changedPropertyNameList = new List<string>();

            person
                .ObservePropertyChanged()
                .Select(e => e.PropertyName)
                .Subscribe(changedPropertyNameList.Add);

            person.Name = newName;

            Assert.Single(changedPropertyNameList);
            Assert.All(changedPropertyNameList, s => Assert.Equal(nameof(person.Name), s));
        }

        [Fact]
        public void ObserveCollectionChangedTest()
        {
            const string value1 = "1";
            const string value2 = "2";
            const string value3 = "3";

            var values = new ObservableCollection<string>();

            var changedEventArgsList = new List<NotifyCollectionChangedEventArgs>();

            values
                .ObserveCollectionChanged()
                .Subscribe(changedEventArgsList.Add);

            values.Add(value1);
            values.Add(value2);
            values.Move(1, 0);
            values[1] = value3;
            values.Remove(value3);
            values.Clear();

            Assert.Equal(6, changedEventArgsList.Count);
            Assert.Equal(NotifyCollectionChangedAction.Add, changedEventArgsList[0].Action);
            Assert.Equal(NotifyCollectionChangedAction.Add, changedEventArgsList[1].Action);
            Assert.Equal(NotifyCollectionChangedAction.Move, changedEventArgsList[2].Action);
            Assert.Equal(NotifyCollectionChangedAction.Replace, changedEventArgsList[3].Action);
            Assert.Equal(NotifyCollectionChangedAction.Remove, changedEventArgsList[4].Action);
            Assert.Equal(NotifyCollectionChangedAction.Reset, changedEventArgsList[5].Action);
        }

        [Fact]
        public void ObservePropertyTest()
        {
            const string name0 = null;
            const string name1 = "Jack";
            const string name2 = "John";

            var person = new PersonX() { Name = name0 };

            var propertyValueList = new List<string>();

            person
                .ObserveProperty(o => o.Name)
                .Subscribe(propertyValueList.Add);
            person.Name = name1;
            person.Name = name2;
            person.Name = name2;
            person.Name = name0;

            Assert.Equal(4, propertyValueList.Count);
            Assert.Equal(name0, propertyValueList[0]);
            Assert.Equal(name1, propertyValueList[1]);
            Assert.Equal(name2, propertyValueList[2]);
            Assert.Equal(name0, propertyValueList[3]);
        }

        [Fact]
        public void ObserveCollectionChangedReactiveTest()
        {
            const string value1 = "1";
            const string value2 = "2";
            const string value3 = "3";

            var values = new ObservableCollection<string>();

            var argsList = new List<CollectionChangedReactiveArgs<string>>();

            values
                .ObserveCollectionChangedReactive()
                .Subscribe(argsList.Add);

            values.Add(value1);
            values.Add(value2);
            values.Move(1, 0);
            values[1] = value3;
            values.Remove(value3);
            values.Clear();

            Assert.Equal(7, argsList.Count);
            Assert.Equal(CollectionChangedReactiveAction.Insert, argsList[0].Action);
            Assert.Equal(CollectionChangedReactiveAction.Insert, argsList[1].Action);
            Assert.Equal(CollectionChangedReactiveAction.Remove, argsList[2].Action);
            Assert.Equal(CollectionChangedReactiveAction.Insert, argsList[3].Action);
            Assert.Equal(CollectionChangedReactiveAction.Replace, argsList[4].Action);
            Assert.Equal(CollectionChangedReactiveAction.Remove, argsList[5].Action);
            Assert.Equal(CollectionChangedReactiveAction.Remove, argsList[6].Action);
        }

        [Fact]
        public void ObserveCollectionChangedReactiveTestForBindingLIst()
        {
            const string value1 = "1";
            const string value2 = "2";
            const string value3 = "3";
            const string value4 = "4";

            var values = new BindingList<string>();

            var argsList = new List<CollectionChangedReactiveArgs<string>>();

            values
                .ObserveCollectionChangedReactive()
                .Subscribe(argsList.Add);

            values.Add(value1);
            values.Add(value2);
            values[1] = value3;
            values.Remove(value3);
            values.Add(value4);
            values.Clear();

            Assert.Equal(7, argsList.Count);

            Assert.Equal(CollectionChangedReactiveAction.Insert, argsList[0].Action);
            Assert.Equal(value1, argsList[0].NewItem);

            Assert.Equal(CollectionChangedReactiveAction.Insert, argsList[1].Action);
            Assert.Equal(value2, argsList[1].NewItem);

            Assert.Equal(CollectionChangedReactiveAction.Replace, argsList[2].Action);
            Assert.Equal(value2, argsList[2].OldItem);
            Assert.Equal(value3, argsList[2].NewItem);

            Assert.Equal(CollectionChangedReactiveAction.Remove, argsList[3].Action);
            Assert.Equal(value3, argsList[3].OldItem);

            Assert.Equal(CollectionChangedReactiveAction.Insert, argsList[4].Action);
            Assert.Equal(value4, argsList[4].NewItem);

            Assert.Equal(CollectionChangedReactiveAction.Remove, argsList[5].Action);
            Assert.Equal(value4, argsList[5].OldItem);

            Assert.Equal(CollectionChangedReactiveAction.Remove, argsList[6].Action);
            Assert.Equal(value1, argsList[6].OldItem);
        }
    }
}
