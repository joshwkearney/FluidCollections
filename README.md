# FluidCollections

All programs manage changing data, but most languages and libraries don't have a consistent way to model the relationships between and among differing collections of this data. Coordinating updates across an entire program can be a nightmare with standard collections, and the logic is often clouded by loops, locking, synchronization logic, and constant state-checking. And *even if* one manages to program all of this correctly, the data still must be reorganized into an `ObservableCollection` or equivalent to display on the UI. Managing data like this is possible with a simple, single threaded program, but throw in some multi-threaded, parallel, and asynchronous logic into your program and this quickly becomes an unworkable nightmare. If any of this sounds familiar (and even if it doesn't), Fluid Collections are for you!

In short, Fluid Collections allow you to specify the relationships between your data, and the library makes it happen auto-magically. This is the same idea as the [Reactive Extensions](http://reactivex.io/) for .NET, but applied to collections instead of sequences. If you don't know much about reactive extensions, don't worry! It's definitely possible to use this library without knowing too much about Rx, but a working knowledge certainly helps. This library defines certain reactive collections that are extremely simple, easy to use, and just seem to *flow* together (and hence the name).

## The ReactiveSet

The `ReactiveSet<T>` is the basic unit of FluidCollections, and right now the only reactive collection (A `ReactiveDictionary<T1,T2>` is in the works). Here is a demo of a basic use case:

``` C#
ReactiveSet<int> set1 = new ReactiveSet<int>();
ReactiveSet<int> set2 = new ReactiveSet<int>();

IReactiveSet<int> union = set1.Union(set2).Where(x => x > 3);

set1.Add(2);
set1.Add(3);

set2.Add(3);
set2.Add(4);

Console.WriteLine(union.Contains(2)); // False
Console.WriteLine(union.Contains(4)); // True
```

To start with, we define two reactive sets. These function with the same rules as normal sets (no duplicates, no order), but with a reactive twist. When the union set is defined, some linq-like operations are used to compose the two original sets, and this composition yields a new ReactiveSet. Here's the reactive part: no matter what happens to the original sets, the new `union` set will always reflect the result of the union operation, followed by the filter; no syncing required! Notice that the union set updates **after** it is defined, in stark contrast to normal collections.

## Operations
FluidCollections was designed with linq and rx in mind, so most of these operations should feel very familiar. Each operation produces a new reactive set that automatically updates itself with the changes that occur in the parent set(s). I haven't listed all the operations below, so I encourage you to explore the library yourself!

#### Set operations
Reactive sets are... well sets. So naturally, standard set operations are allowed

```C#
set1.Union(set2);
set1.Intersection(set2);
set1.Except(set2);
set1.SymmetricExcept(set2);
```

#### Select/Where
Just like linq and rx, you can transform and filter reactive sets

```C#
set1.Where(x => x <= 9);
set1.Select(x => x + 75);
```

#### Aggregate and friends
Aggregate on reactive sets is similar to that of linq, but with one major exception: The aggregate for reactive sets is a *running* total, not an instantaneous total. As such, it returns an IObservable of the result. Aggregate essentially converts the changes from a reactive set into a running total by using the supplied add function or remove function, depending on the type of update. After updating the total, the new result is pushed to the `IObservable`

```C#
// ReactiveSet<T>.Aggregate(seed, addFunction, removeFunction)
IObservable<int> sum = set1.Aggregate(0, (total, item) => total + item, (total, item) => total - item);

// Implemented with aggregate
IObservable<int> realSum = set1.Sum();
IObservable<int> product = set1.Product();
IObservable<int> count = set1.Count();
IObservable<IImmutableSet<int>> sets = set1.ToImmutableSets();
```

#### ToReactiveSet
After a chain of other non-buffering operations, use `ToReactiveSet()` to buffer the result into an internal collection that can be traversed. (See variants for more info)

```C#
// Set3 is non-buffering. No count property. Also can't enumerate the elements
IReactiveSet<int> set3 = set1.Intersection(set2).Where(x => x > 4);

// This stores all the resulting elements in an internal set, and so is a bit more concrete
ICollectedReactiveSet<int> bufferedSet3 = set3.ToReactiveSet();

// Instantaneous count property
Console.WriteLine(bufferedSet3.Count);

// Can traverse the elements currently in the set
// Since the set is reactive, both the elements and the count can unpredictably change
foreach (int num in bufferedSet3.AsEnumerable()) {
    Console.WriteLine(num);
}
```

#### OrderBy
OrderBy buffers a reactive set into an ordered reactive set (see below), using either the default comparer or a provided property to sort on.

```C#
IOrderedReactiveSet<int> set4 = set1.OrderBy(x => x /* Normally a useful property */);

Console.WriteLine(set4.Min);
Console.WriteLine(set4.Max);

// Indexing works!
Console.WriteLine(set4.IndexOf(4));
Console.WriteLine(set4[2]);
```

## ReactiveSet Variants
- `ReactiveSet<T>` - The most basic reactive set, is used for the "source" for more dependent reactive sets. Is mutable, and supports additions and removals

- `IReactiveSet<T>` - This represents a non-buffered reactive set, meaning that the elements are not stored in an underlying collection. The benefit is that it uses almost no additional memory, making it very efficient. Think of it as the `IEnumerable` of FluidCollections. Most extension methods return an `IReactiveSet`, and it is not directly modifiable. Despite not buffering elements, there is a contains method.

- `ICollectedReactiveSet<T>` - Think of this as the `ICollection` of FluidCollections. This is the same as a normal reactive set, but there is a count property as well as an `AsEnumerable()` method that lets you traverse the elements directly. It is buffered and stores a copy of the elements in its own internal set. This is usually the end result of a chain of operations performed on another reactive set so you can actually use the set with other code.

- `IOrderedReactiveSet<T>` - This type of reactive set is both sorted and indexed, and provides appropriate members accordingly. All operations are performed in O(log n) time, including indexing. Because of this, an ordered reactive set implements `INotifyCollectionChanged`, making it perfect for UI binding! (for those interested, this is implemented with a custom weight-balanced order statistics tree).

## Contributing
This project is just in its infancy, and I'm not attached to particular API's, classes, or methods. Right now, I simply want to make the library as good as it can get, and backwards compatibly can come later. So I encourage you, contribute! There is so much that can be done with this library that I won't have time to implement, and I'd like to hear any suggestions for improvements you have. Going forward, I'd like to build this project into a one-stop-shop for reactive collections, while at the same time keeping it:

- Easy to use. Knowing linq should be enough to "dot in" and figure out how to use nearly all of the operations.
- Performant, but not at the expense of safety. Speed is always a bonus, but I also want the operations to have a certain plug-and-play feel that comes with linq.
- Intuitive. The name of the project is "fluid" after all, and the updates should Just Happen™ without knowledge of the inner workings.

I've tried to achieve these goals with the current version, and if anybody out there want to help build this library, send over a pull request! (I won't bite)

## Acknowledgments
Credit where credit is due, and I'm not afraid to admit that I never would have created this project were it not for [DynamicData](https://github.com/RolandPheasant/DynamicData). DynamicData is wonderful, and a truly spectacular piece of engineering. When I first discovered it, I was hooked and tried to convert some of my personal project to observable lists and caches. However, upon doing so I realized that indexed lists were not always ideal for my situations, and caches sometimes were cumbersome. I began brainstorming, and soon realized that unordered sets were ideal for update notifications, and that reordering can be accomplished with trees. Implementing my set idea became a challenge, and this project was the result. So to DyamicData I say thank you for making me see collections in an entirely different light.
