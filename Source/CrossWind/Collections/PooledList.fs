namespace CrossWind.Collections

open System
open System.Buffers
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Diagnostics
open System.Linq
open System.Runtime.CompilerServices
open CrossWind.Runtime

/// <summary>
/// List implementation using <see cref="ArrayPool{T}"/> for storing elements. This implementation is based on <see cref="List{T}"/> and <see href="https://github.com/jtmueller/Collections.Pooled"/>.
/// </summary>
/// <typeparam name="T">Type of element.</typeparam>
[<DebuggerDisplay("Count = {Count}")>]
type PooledList<'T> =
    val mutable private count : int
    val private pool : ArrayPool<'T>
    val mutable private items : 'T []

    /// <summary>
    /// Constructs a <see cref="PooledList{T}"/>. The list is initialized using a new storage array with minimum storage and default <see cref="ArrayPool{T}"/>.
    /// </summary>
    new () = new PooledList<'T> 0
    /// <summary>
    /// Constructs a <see cref="PooledList{T}"/> with storage <paramref name="capacity"/>. The list is initialized using a new storage array from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="capacity">Initial capacity of the list.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than zero.</exception>
    new (capacity) =
        if capacity < 0 then
            ThrowHelpers.ThrowArgumentOutOfRangeException(ExceptionArgument.Capacity, ArgumentOutOfRange_NeedNonNegNum)

        let pool = ArrayPool<'T>.Shared

        { count = 0 ; pool = pool ; items = pool.Rent capacity }
    /// <summary>
    /// Constructs a <see cref="PooledList{T}"/>. The list is initialized using a new storage array from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="lst">The list to copy elements from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="lst"/> is <c>null</c>.</exception>
    new (lst : _ PooledList) =
        ArgumentNullException.ThrowIfNull lst
        let pool = ArrayPool<'T>.Shared
        let items = pool.Rent lst.count

        for i = 0 to lst.count - 1 do
            items.[i] <- lst.items.[i]

        { count = lst.Count ; pool = pool ; items = items }
    /// <summary>
    /// Constructs a <see cref="PooledList{T}"/>. The list is initialized using a new storage array from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="collection">The collection to copy elements from.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> is <c>null</c>.</exception>
    new (collection : _ ICollection) =
        ArgumentNullException.ThrowIfNull collection
        let pool = ArrayPool<'T>.Shared
        let items = pool.Rent collection.Count
        collection.CopyTo(items, 0)

        { count = collection.Count ; pool = pool ; items = items }
    /// <summary>
    /// Constructs a <see cref="PooledList{T}"/>. The list is initialized using a new storage array from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="enumerable">Enumerable to copy elements from.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerable"/> is <c>null</c>.</exception>
    new (enumerable : 'T seq) =
        ArgumentNullException.ThrowIfNull enumerable
        let pool = ArrayPool<'T>.Shared
        let items = enumerable.ToArray()

        { count = items.Length ; pool = pool ; items = items }
    /// <summary>
    /// Constructs a <see cref="PooledList{T}"/>. The list is initialized using a new storage array from <see cref="ArrayPool{T}"/>. The <paramref name="arr"/> must not be a <code>null</code> reference.
    /// </summary>
    /// <param name="arr">The array to copy elements from.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arr"/> is <c>null</c>.</exception>
    new (arr : 'T []) =
        ArgumentNullException.ThrowIfNull arr
        let pool = ArrayPool<'T>.Shared
        let items = pool.Rent arr.Length
        Array.Copy(arr, items, arr.Length)

        { count = arr.Length ; pool = pool ; items = items }
    /// <summary>
    /// Create a new <see cref="PooledList{T}"/> from an existing <see cref="'T Span"/>
    /// </summary>
    /// <param name="span">The span to copy elements from.</param>
    new (span : 'T Span) = { count = span.Length ; pool = ArrayPool<'T>.Shared ; items = span.ToArray() }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member internal x.Resize newLength shrinking =
        let newItems = x.pool.Rent newLength
        if shrinking then x.count <- newLength

        x.items.AsSpan(0, x.count).CopyTo(newItems.AsSpan())

        x.pool.Return(x.items, RuntimeHelpers.IsReferenceOrContainsReferences<'T>())
        x.items <- newItems

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member internal x.EnsureCapacity minCapacity = if minCapacity > x.items.Length then x.Resize minCapacity false

    member internal x.InsertSpanRange (index, insertCount) =
        let newSize = x.count + insertCount
        let itemsToCopy = x.count - index
        let mutable itemsSpan = x.items.AsSpan()

        if newSize <= x.items.Length then
            itemsSpan.Slice(index, itemsToCopy).CopyTo(itemsSpan.Slice(index + x.count, itemsToCopy))
        else
            let newArray = x.pool.Rent(newSize)
            let newArraySpan = newArray.AsSpan()

            itemsSpan.Slice(0, index).CopyTo(newArraySpan.Slice(0, index))

            if index < x.count then
                itemsSpan.Slice(index, itemsToCopy).CopyTo(newArraySpan.Slice(index + x.count))
            //else itemsSpan.Slice(0, count).CopyTo(newArraySpan)

            x.pool.Return(x.items, RuntimeHelpers.IsReferenceOrContainsReferences<'T>())
            x.items <- newArray
            itemsSpan <- newArraySpan

        x.count <- newSize
        itemsSpan.Slice(index, insertCount)
    /// <summary>
    /// Get a <see cref="Span{T}"/> of the internal <see cref="'T[]"/>. Elements can be added in batches.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> of the internal <see cref="'T[]"/>.</returns>
    /// <param name="rangeSize">The size of the range to get.</param>
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member x.GetSpanRange rangeSize = x.InsertSpanRange(x.count, rangeSize)

    /// <summary>
    /// Add an element to the end of <see cref="PooledList{T}"/>.
    /// </summary>
    /// <param name="item">Element to be added.</param>
    member x.Add item =
        let count = x.count
        x.EnsureCapacity(count + 1)
        x.items.[count] <- item
        x.count <- count + 1
    /// <summary>
    /// Add a range of elements to the end of <see cref="PooledList{T}"/>.
    /// </summary>
    /// <param name="collection">The collection to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> is <c>null</c>.</exception>
    member x.AddRange collection =
        ArgumentNullException.ThrowIfNull collection

        for item in collection do
            x.Add item
    /// <summary>
    /// Get a <see cref="ReadOnlyCollection{T}"/> of the internal <see cref="'T[]"/>.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyCollection{T}"/> of the internal <see cref="'T[]"/>.</returns>
    member x.AsReadOnly () = ReadOnlyCollection x
    /// <summary>
    /// Get a <see cref="'T Span"/> of the internal <see cref="'T[]"/>.
    /// </summary>
    /// <returns>A <see cref="'T Span"/> of the internal <see cref="'T[]"/>.</returns>
    member x.AsSpan () = x.items.AsSpan(0, x.count)

    /// <summary>
    /// Get/Set <see cref="PooledList{T}"/> capacity to hold elements.
    /// </summary>
    member x.Capacity
        with get () = x.items.Length
        and set value = if value <> x.items.Length then x.Resize value (value < x.count)

    member private x.CheckIndex (index, insertion) =
        if insertion then
            if uint index > uint x.count then
                ThrowHelpers.ThrowArgumentOutOfRangeException(ExceptionArgument.Index, ArgumentOutOfRange_ListInsert)
        else if uint index >= uint x.count then
            ThrowHelpers.ThrowArgumentOutOfRange_IndexException()
    /// <summary>
    /// Clear all elements from <see cref="PooledList{T}"/>.
    /// </summary>
    /// <remarks>
    /// This method is an O(1) operation. It does not trim the internal array.
    /// </remarks>
    member x.Clear () = x.count <- 0
    /// <summary>
    /// Check if <see cref="PooledList{T}"/> contains an element.
    /// </summary>
    /// <param name="item">The element to check for.</param>
    /// <returns><c>true</c> if <see cref="PooledList{T}"/> contains the element.</returns>
    /// <remarks>
    /// This method is an O(n) operation.
    /// </remarks>
    member x.Contains item =
        let mutable i = 0
        let items = x.items
        let mutable found = false

        if typeof<'T>.IsValueType then
            while not found && i < x.count do
                found <- EqualityComparer<'T>.Default.Equals (items.[i], item)
                i <- i + 1
        else
            let comparer = EqualityComparer<'T>.Default

            while not found && i < x.count do
                found <- comparer.Equals(items.[i], item)
                i <- i + 1

        found
    /// <summary>
    /// Copy the elements of <see cref="PooledList{T}"/> to the given <paramref name="arr"/>.
    /// </summary>
    /// <param name="arr">The array to copy to.</param>
    /// <param name="index">The index to start copying at.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arr"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than zero.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="arr"/> is not large enough to hold the elements.</exception>
    /// <remarks>
    /// This method is an O(n) operation.
    /// </remarks>
    member x.CopyTo (arr : 'T [], index : int) =
        arr |> ArgumentNullException.ThrowIfNull

        (x.items.AsSpan(0, x.count)).CopyTo(arr.AsSpan(index, x.count))

    /// <summary>
    /// Count of elements in <see cref="PooledList{T}"/>.
    /// </summary>
    member x.Count = x.count

    /// <summary>
    /// Span of free capacity available in <see cref="PooledList{T}"/>.
    /// </summary>
    member x.FreeCapacity =
        let c = x.count
        x.count <- x.items.Length
        x.items.AsSpan(c)
    /// <summary>
    /// Get an enumerator for <see cref="PooledList{T}"/>.
    /// </summary>
    /// <returns>An enumerator for <see cref="PooledList{T}"/>.</returns>
    /// <remarks>
    /// This method is an O(1) operation.
    ///</remarks>
    member x.GetEnumerator () = new Enumerator<'T>(x)

    member x.IndexBasedCopy (indexedCounts : _ [], count) =
        let newItems = x.pool.Rent count

        for struct (index, count) in indexedCounts do
            let item = x.items.[index]

            for i = index to index + count - 1 do
                newItems.[i] <- item

        x.pool.Return x.items
        x.items <- newItems

    member x.IndexBasedCopy (indexedCounts : _ []) =
        let mutable totalCount = 0

        for struct (_, count) in indexedCounts do
            totalCount <- totalCount + count

        x.IndexBasedCopy(indexedCounts, totalCount)

    member x.IndexOf item = Array.IndexOf(x.items, item, 0, x.count)
    /// <summary>
    /// Get/Set an element at the given <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the element to get/set.</param>
    /// <returns>The element at the given <paramref name="index"/>.</returns>
    member x.Item
        with get index =
            x.CheckIndex(index, false)
            x.items.[index]
        and set index value =
            x.CheckIndex(index, false)
            x.items.[index] <- value
    /// <summary>
    /// Insert an element at the given <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="item">The element to insert.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than zero.</exception>
    member x.Insert (index, item) =
        let count = x.count
        x.EnsureCapacity(count + 1)
        let arr = x.items
        x.CheckIndex(index, true)

        if count <> index then Array.Copy(arr, index, arr, index + 1, count - index)

        arr.[index] <- item
        x.count <- count + 1

    member x.Remove item =
        let index = Array.IndexOf(x.items, item, 0, x.count)

        if index < 0 then
            false
        else
            index |> x.RemoveAt
            true

    member x.RemoveAt index =
        let mutable count = x.count

        if uint (index) < uint (count) then
            count <- count - 1
            Array.Copy(x.items, index + 1, x.items, index, count - index)
            x.count <- count

            if RuntimeHelpers.IsReferenceOrContainsReferences<'T>() then
                x.items.[count] <- Unchecked.defaultof<_>
        else
            ThrowHelpers.ThrowArgumentOutOfRange_IndexException()

    member x.RemoveRange (start, count) =
        let totalCount = x.count
        let rangeEnd = count + start
        let tailCount = totalCount - rangeEnd
        let items = x.items.AsSpan()

        if tailCount > 0 then
            items.Slice(rangeEnd).CopyTo(items.Slice(start, tailCount))

    member x.TrimExcess () =
        if x.count < x.items.Length
           && x.count
              |> CollectionHelpers.SizeToIndex
              |> CollectionHelpers.IndexToSize < x.items.Length then
            x.Resize x.count true
    /// <summary>
    /// Disposes the internal array used for storage.
    /// </summary>
    member x.Dispose () =
        x.pool.Return(x.items, RuntimeHelpers.IsReferenceOrContainsReferences<'T>())
        x.items <- null

    interface IDisposable with
        member x.Dispose () = x.Dispose()

    interface IList<'T> with
        member x.Add item = x.Add item
        member x.Clear () = x.Clear()
        member x.Contains item = x.Contains item
        member x.CopyTo (array, arrayIndex) = x.CopyTo(array, arrayIndex)
        member x.Count = x.count
        member x.GetEnumerator () : Collections.IEnumerator = x.GetEnumerator()
        member x.GetEnumerator () : 'T IEnumerator = x.GetEnumerator()
        member x.IndexOf item = x.IndexOf item
        member x.Insert (index, item) = x.Insert(index, item)
        member _.IsReadOnly = false

        member x.Item
            with get index = x.[index]
            and set index value = x.[index] <- value

        member x.Remove item = x.Remove item
        member x.RemoveAt index = x.RemoveAt index
/// <summary>
/// A struct based Enumerator for <see cref="PooledList{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
and [<Struct ; NoComparison ; NoEquality>] Enumerator<'T> =
    new (list : 'T PooledList) = { list = list ; index = 0 ; current = Unchecked.defaultof<_> }

    val mutable internal list : 'T PooledList
    val mutable internal index : int
    val mutable internal current : 'T
    /// <summary>
    /// Gets the element at the current position of the enumerator.
    /// </summary>
    /// <returns>The element at the current position of the enumerator.</returns>
    member x.Current = x.current

    /// <summary>
    /// Advances the enumerator to the next element of the list.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the enumerator was successfully advanced to the next element;
    /// <c>false</c> if the enumerator has passed the end of the list.
    /// </returns>
    member x.MoveNext () =
        if uint (x.index) < uint (x.list.Count) then
            x.current <- x.list.[x.index]
            x.index <- x.index + 1
            true
        else
            x.current <- Unchecked.defaultof<_>
            false
    /// <summary>
    /// Disposes the enumerator.
    /// </summary>
    member x.Dispose () =
        x.index <- Int32.MinValue
        x.current <- Unchecked.defaultof<_>
    /// <summary>
    /// Resets the enumerator to the beginning of the list.
    /// </summary>
    /// <remarks>
    /// This method is an O(1) operation.
    /// </remarks>
    member x.Reset () =
        x.index <- 0
        x.current <- Unchecked.defaultof<_>

    interface IEnumerator<'T> with
        member x.Current : obj = x.current :> _
        member x.Current = x.current
        member x.MoveNext () = x.MoveNext()
        member x.Reset () = x.Reset()
        member x.Dispose () = x.Dispose()
