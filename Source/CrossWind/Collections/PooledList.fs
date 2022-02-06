namespace CrossWind.Collections

open System
open System.Collections.Generic
open System.Diagnostics
open System.Buffers
open System.Runtime.CompilerServices
open CrossWind.Runtime
open System.Collections.ObjectModel

module PooledList =
    /// <summary>
    /// List implementation using <see cref="ArrayPool{T}"/> for storing elements. This implementation is based on <see cref="List{T}"/> and <see href="https://github.com/jtmueller/Collections.Pooled"/>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    [<DebuggerDisplay("Count = {Count}")>]
    type PooledList<'T> =
        val mutable private count: int
        val private pool: ArrayPool<'T>
        val mutable private items: 'T []

        /// <summary>
        /// Constructs a <see cref="PooledList{T}"/>. The list is initialized using a new storage array with minimum storage and default <see cref="ArrayPool{T}"/>.
        /// </summary>
        new() = new PooledList<'T> 0

        new(capacity) =
            let pool = ArrayPool<'T>.Shared

            { count = 0
              pool = pool
              items = pool.Rent capacity }

        new(lst: _ PooledList) =
            let pool = ArrayPool<'T>.Shared
            let items = pool.Rent lst.count

            for i = 0 to lst.count - 1 do
                items.[i] <- lst.items.[i]

            { count = lst.Count
              pool = pool
              items = items }

        new(lst: _ ICollection) =
            let pool = ArrayPool<'T>.Shared
            let items = pool.Rent lst.Count
            lst.CopyTo(items, 0)

            { count = lst.Count
              pool = pool
              items = items }

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
#if DEBUG
        member internal x.Resize newLength shrinking =
#else
        member inline internal x.Resize newLength shrinking =
#endif
            let newItems = x.pool.Rent newLength
            if shrinking then x.count <- newLength

            x
                .items
                .AsSpan(0, x.count)
                .CopyTo(newItems.AsSpan())

            x.pool.Return(x.items, RuntimeHelpers.IsReferenceOrContainsReferences<'T>())
            x.items <- newItems

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
#if DEBUG
        member internal x.EnsureCapacity minCapacity =
#else
        member inline internal x.EnsureCapacity minCapacity =
#endif
            if minCapacity > x.items.Length then
                x.Resize minCapacity false

        member internal x.InsertSpanRange(index, insertCount) =
            let newSize = x.count + insertCount
            let itemsToCopy = x.count - index
            let mutable itemsSpan = x.items.AsSpan()

            if newSize <= x.items.Length then
                itemsSpan
                    .Slice(index, itemsToCopy)
                    .CopyTo(itemsSpan.Slice(index + x.count, itemsToCopy))
            else
                let newArray = x.pool.Rent(newSize)
                let newArraySpan = newArray.AsSpan()

                itemsSpan
                    .Slice(0, index)
                    .CopyTo(newArraySpan.Slice(0, index))

                if index < x.count then
                    itemsSpan
                        .Slice(index, itemsToCopy)
                        .CopyTo(newArraySpan.Slice(index + x.count))
                //else itemsSpan.Slice(0, count).CopyTo(newArraySpan)

                x.pool.Return(x.items, RuntimeHelpers.IsReferenceOrContainsReferences<'T>())
                x.items <- newArray
                itemsSpan <- newArraySpan

            x.count <- newSize
            itemsSpan.Slice(index, insertCount)

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member x.InsertSpanRange rangeSize = x.InsertSpanRange(x.count, rangeSize)

        /// <summary>
        /// Add an element to the end of <see cref="PooledList{T}"/>.
        /// </summary>
        /// <param name="item">Element to be added.</param>
        member x.Add item =
            let count = x.count
            x.EnsureCapacity(count + 1)
            x.items.[count] <- item
            x.count <- count + 1

        member x.AddRange collection =
            for item in collection do
                x.Add item

        member x.AsReadOnly() = ReadOnlyCollection x

        member x.AsSpan() = x.items.AsSpan(0, x.count)

        /// <summary>
        /// Get/Set <see cref="PooledList{T}"/> capacity to hold elements.
        /// </summary>
        member x.Capacity
            with get () = x.items.Length
            and set value =
                if value <> x.items.Length then
                    x.Resize value (value < x.count)

        member private x.CheckIndex(index, insertion) =
            if insertion then
                if uint index > uint x.count then
                    ThrowHelpers.ThrowArgumentOutOfRangeException(
                        ExceptionArgument.Index,
                        ArgumentOutOfRange_ListInsert
                    )
            else if uint index >= uint x.count then
                ThrowHelpers.ThrowArgumentOutOfRange_IndexException()

        member x.Clear() = x.count <- 0

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

        member x.CopyTo(arr: 'T [], index: int) =
            arr |> ArgumentNullException.ThrowIfNull

            (x.items.AsSpan(0, x.count))
                .CopyTo(arr.AsSpan(index, x.count))

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

        member x.GetEnumerator() = new Enumerator<'T>(x)

        member x.IndexBasedCopy(indexedCounts: _ [], count) =
            let newItems = x.pool.Rent count

            for struct (index, count) in indexedCounts do
                let item = x.items.[index]

                for i = index to index + count - 1 do
                    newItems.[i] <- item

            x.pool.Return x.items
            x.items <- newItems

        member x.IndexBasedCopy(indexedCounts: _ []) =
            let mutable totalCount = 0

            for struct (_, count) in indexedCounts do
                totalCount <- totalCount + count

            x.IndexBasedCopy(indexedCounts, totalCount)

        member x.IndexOf item =
            Array.IndexOf(x.items, item, 0, x.count)

        member x.Item
            with get index =
                x.CheckIndex(index, false)
                x.items.[index]
            and set index value =
                x.CheckIndex(index, false)
                x.items.[index] <- value

        member x.Insert(index, item) =
            let count = x.count
            x.EnsureCapacity(count + 1)
            let arr = x.items
            x.CheckIndex(index, true)

            if count <> index then
                Array.Copy(arr, index, arr, index + 1, count - index)

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

        member x.RemoveRange(start, count) =
            let totalCount = x.count
            let rangeEnd = count + start
            let tailCount = totalCount - rangeEnd
            let items = x.items.AsSpan()

            if tailCount > 0 then
                items
                    .Slice(rangeEnd)
                    .CopyTo(items.Slice(start, tailCount))

        member x.TrimExcess() =
            if x.count < x.items.Length
               && x.count
                  |> CollectionHelpers.SizeToIndex
                  |> CollectionHelpers.IndexToSize < x.items.Length then
                x.Resize x.count true

        member x.Dispose() =
            x.pool.Return x.items
            x.items <- null

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        interface IList<'T> with
            member x.Add item = x.Add item
            member x.Clear() = x.Clear()
            member x.Contains item = x.Contains item
            member x.CopyTo(array, arrayIndex) = x.CopyTo(array, arrayIndex)
            member x.Count = x.count
            member x.GetEnumerator() : Collections.IEnumerator = x.GetEnumerator()
            member x.GetEnumerator() : 'T IEnumerator = x.GetEnumerator()
            member x.IndexOf item = x.IndexOf item
            member x.Insert(index, item) = x.Insert(index, item)
            member _.IsReadOnly = false

            member x.Item
                with get index = x.[index]
                and set index value = x.[index] <- value

            member x.Remove item = x.Remove item
            member x.RemoveAt index = x.RemoveAt index

    and [<Struct; NoComparison; NoEquality>] Enumerator<'T> =
        new(list: 'T PooledList) =
            { list = list
              index = 0
              current = Unchecked.defaultof<_> }

        val mutable internal list: 'T PooledList
        val mutable internal index: int
        val mutable internal current: 'T
        member x.Current = x.current

        member x.MoveNext() =
            if uint (x.index) < uint (x.list.Count) then
                x.current <- x.list.[x.index]
                x.index <- x.index + 1
                true
            else
                x.current <- Unchecked.defaultof<_>
                false

        member x.Dispose() =
            x.index <- Int32.MinValue
            x.current <- Unchecked.defaultof<_>

        member x.Reset() =
            x.index <- 0
            x.current <- Unchecked.defaultof<_>

        interface IEnumerator<'T> with
            member x.Current: obj = x.current :> _
            member x.Current = x.current
            member x.MoveNext() = x.MoveNext()
            member x.Reset() = x.Reset()
            member x.Dispose() = x.Dispose()
