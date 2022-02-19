namespace CrossWind.Collections

open System.Diagnostics
open CrossWind.Runtime
open HashHelpers
open System.Collections.Generic
open System
open CrossWind.Runtime.CollectionHelpers
open System.Runtime.CompilerServices

[<CustomEquality ; NoComparison>]
type Entry<'TKey, 'TValue> =
    struct
        /// <summary>
        /// The calculated hash code for the key.
        /// </summary>
        val mutable HashCode : uint32
        /// <summary>
        /// The next entry, if any, in the dictionary which shares the same bucket.
        /// </summary>
        val mutable Next : int32
        /// <summary>
        /// The key for this entry.
        /// </summary>
        val mutable Key : 'TKey
        /// <summary>
        /// The value for the <see cref="Key"/>.
        /// </summary>
        val mutable Value : 'TValue

        override x.Equals o =
            o.IsOfType<Entry<'TKey, 'TValue>>()
            && x.Equals(o.As<_, Entry<'TKey, 'TValue>>())

        override x.GetHashCode () = x.HashCode |> int

        member left.Equals (right : Entry<'TKey, 'TValue>) =
            left.HashCode = right.HashCode
            && left.Next = right.Next
            && EqualityComparer<'TKey>.Default.Equals (left.Key, right.Key)
            && EqualityComparer<'TValue>.Default.Equals (left.Value, right.Value)

        interface IEquatable<Entry<'TKey, 'TValue>> with
            member left.Equals (right : Entry<'TKey, 'TValue>) = left.Equals(right)
    end

/// <summary>
/// A dictionary implementation that uses pooled storage based on <see cref="Dictionary{TKey,TValue}"/> and <see href="https://github.com/jtmueller/Collections.Pooled"/>.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[<DebuggerDisplay("Count = {Count}") ; DebuggerTypeProxy(typeof<DebugView.ICollectionDebugView<_>>)>]
type PooledDictionary1<'TKey, 'TValue, 'KeyComparer when 'KeyComparer :> IEqualityComparer<'TKey>>
    (
        multiplier,
        primeIndex,
        keyComparer
    ) =
    let mutable count = 0
    let mutable freeCount = 0
    let mutable fastModMultiplier = multiplier
    let bucketPool = PrimeSizedArrayPool<int>.Shared
    let entryPool = PrimeSizedArrayPool<Entry<'TKey, 'TValue>>.Shared
    let mutable buckets = primeIndex |> bucketPool.RentFromBucket
    let mutable entries = primeIndex |> entryPool.RentFromBucket
    let comparer : 'KeyComparer = keyComparer |> ApplyComparer

    let ReturnArrays () =
        bucketPool.Return(buckets)
        buckets <- null
        entryPool.Return(entries, RuntimeHelpers.IsReferenceOrContainsReferences<Entry<'TKey, 'TValue>>())
        entries <- null

    new (capacity, keyComparer) =
        let primeIndex = capacity |> SizeToIndex
        PooledDictionary1(PrimeMultiplier.[primeIndex], primeIndex, keyComparer)

    new (capacity) = PooledDictionary1(capacity, Unchecked.defaultof<_>)
    new (keyComparer) = PooledDictionary1(0, keyComparer)

    member _.Comparer = comparer
    member _.Count = count - freeCount

    member inline internal _.GetBucket (hashCode, bucketIndex : _ outref) =
        bucketIndex <- FastMod hashCode buckets.LongLength fastModMultiplier
        &buckets.[bucketIndex]

    member inline internal _.GetBucket (hashCode) = &buckets.[FastMod hashCode buckets.LongLength fastModMultiplier]

    member inline internal _.GetBucketIndex (hashCode) = FastMod hashCode buckets.LongLength fastModMultiplier

    member inline internal x.copyEntries
        (
            oldEntries : Entry<_, 'TValue> Span,
            newEntries : _ Span,
            skipDeletions,
            forceNewHashCodes,
            resizeMode
        ) =
        let mutable newCount = 0

        for i = 0 to oldEntries.Length - 1 do
            let mutable entry = &newEntries.[newCount]
            entry <- oldEntries.[i]

            if skipDeletions
               || entry.Next <> Int32.MinValue then
                if forceNewHashCodes
                   && RuntimeHelpers.IsReferenceOrContainsReferences<'TKey>() then
                    entry.HashCode <-
                        entry.Key
                        |> comparer.GetHashCode
                        |> uint

                let bucket = &x.GetBucket entry.HashCode
                entry.Next <- bucket - 1
                newCount <- newCount + 1

                bucket <- if resizeMode then newCount else count + newCount

    [<MethodImpl(MethodImplOptions.AggressiveOptimization)>]
    member inline internal x.ResizeAndHash capacity forceNewHashCodes =
        let bucket = GetBucketAndMultiplier(capacity, &fastModMultiplier)
        buckets <- bucketPool.ReturnAndRentFromBucket(buckets, bucket, true)

        if count > 0 then
            let oldEntries = entries
            entries <- bucket |> entryPool.RentFromBucket
            let oldSpan = oldEntries.AsSpan(0, count)
            let newSpan = entries.AsSpan(0, count)

            if freeCount = 0 then
                x.copyEntries (oldSpan, newSpan, false, forceNewHashCodes, true)
            else
                x.copyEntries (oldSpan, newSpan, true, forceNewHashCodes, true)

            entryPool.Return(oldEntries, RuntimeHelpers.IsReferenceOrContainsReferences<Entry<'TKey, 'TValue>>())
        else
            entryPool.Return(entries, RuntimeHelpers.IsReferenceOrContainsReferences<Entry<'TKey, 'TValue>>())
            entries <- bucket |> entryPool.RentFromBucket

    [<MethodImpl(MethodImplOptions.AggressiveOptimization)>]
    member internal x.Resize capacity = x.ResizeAndHash capacity false

    [<MethodImpl(MethodImplOptions.AggressiveOptimization)>]
    member internal x.Resize () = x.ResizeAndHash x.Count false

    member inline internal x.FindEntry
        (
            key,
            returnLastEntry,
            [<InlineIfLambdaAttribute>] success,
            [<InlineIfLambdaAttribute>] failure
        ) =
        let mutable lastEntry = -1

        if typeof<'TKey>.IsValueType then
            let hashCode =
                key
                |> EqualityComparer<'TKey>.Default.GetHashCode
                |> uint

            let bucket = x.GetBucketIndex hashCode
            let mutable entryIndex = buckets.[bucket] - 1

            while (if (uint) entryIndex < (uint) entries.Length then
                       let entry = &entries.[entryIndex - 1]

                       if entry.HashCode = hashCode
                          && EqualityComparer<'TKey>.Default.Equals (entry.Key, key) then
                           success hashCode bucket entryIndex lastEntry
                           false
                       else
                           if returnLastEntry then lastEntry <- entryIndex

                           entryIndex <- entry.Next
                           true
                   else
                       failure hashCode bucket
                       false

            ) do
                ()

            entryIndex
        else
            let hashCode = key |> comparer.GetHashCode |> uint
            let bucket = x.GetBucketIndex hashCode
            let mutable entryIndex = buckets.[bucket] - 1

            while (if (uint) entryIndex < (uint) entries.Length then
                       let entry = &entries.[entryIndex - 1]

                       if
                           entry.HashCode = hashCode
                           && comparer.Equals(entry.Key, key)
                       then
                           success hashCode bucket entryIndex lastEntry
                           false
                       else
                           if returnLastEntry then lastEntry <- entryIndex

                           entryIndex <- entry.Next
                           true
                   else
                       failure hashCode bucket
                       false

            ) do
                ()

            entryIndex

    member internal x.FindEntry (key : 'TKey) = x.FindEntry(key, false, (fun _ _ _ _ -> ()), (fun _ _ -> ()))

    member inline internal x.FindEntry
        (
            key : 'TKey,
            [<InlineIfLambdaAttribute>] success,
            [<InlineIfLambdaAttribute>] failure
        ) =
        x.FindEntry(key, false, (fun hashCode bucket entry _ -> success hashCode bucket entry), failure)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member x.TryGetValue (key, value : _ outref) =
        let mutable v = Unchecked.defaultof<_>

        let r =
            x.FindEntry(key, (fun _ _ entryIndex -> v <- entries.[entryIndex].Value), (fun _ _ -> ()))
            |> uint < uint (entries.Length)

        value <- v
        r

    member x.Item
        with get key =
            match x.TryGetValue(key) with
            | true, value -> value
            | false, _ ->
                ThrowHelpers.ThrowKeyNotFoundException(key)
                Unchecked.defaultof<_>
        and set key value =
            x.TryInsert(key, value, PooledHash.ThrowOnExisting)
            |> ignore

    member x.TrimExcess (capacity) =
        if capacity < x.Count then
            ThrowHelpers.ThrowArgumentOutOfRangeException(ExceptionArgument.Capacity)

        x.Resize capacity

    member x.Remove (key, value : _ outref) =
        let mutable v = Unchecked.defaultof<_>

        let r =
            x.FindEntry(
                key,
                true,
                (fun _ bucket entryIndex lastEntry ->
                    let mutable entry = entries.[entryIndex]
                    v <- entry.Value

                    match lastEntry with
                    | -1 -> buckets.[bucket] <- entry.Next + 1
                    | _ -> entries.[lastEntry].Next <- entry.Next

                    entry.Next <- Int32.MinValue
                ),
                (fun _ _ -> ())
            )
            |> uint < uint (entries.Length)

        value <- v
        r

    member x.Remove (key) =
        x.FindEntry(
            key,
            true,
            (fun _ bucket entryIndex lastEntry ->
                let mutable entry = entries.[entryIndex]

                match lastEntry with
                | -1 -> buckets.[bucket] <- entry.Next + 1
                | _ -> entries.[lastEntry].Next <- entry.Next

                entry.Next <- Int32.MinValue
            ),
            (fun _ _ -> ())
        )
        |> uint < uint (entries.Length)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member x.TryAdd (key, value) = x.TryInsert(key, value, PooledHash.None)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member x.Add (key, value) =
        x.TryInsert(key, value, PooledHash.ThrowOnExisting)
        |> ignore

    member inline internal _.EntriesSpan (start, length) = entries.AsSpan(start, length)
    member inline internal _.SkipDeletions = freeCount > 0

    member x.AddRange (source : _ IEnumerable) =
        match source with
        | :? PooledDictionary1<'TKey, 'TValue, 'KeyComparer> as collection ->
            let collectionCount = collection.Count
            let dictionaryCount = x.Count
            x.Resize(collectionCount + dictionaryCount)

            match collection.SkipDeletions with
            | true ->
                x.copyEntries (
                    collection.EntriesSpan(0, collectionCount),
                    x.EntriesSpan(dictionaryCount, collectionCount),
                    true,
                    true,
                    false
                )
            | false ->
                x.copyEntries (
                    collection.EntriesSpan(0, collectionCount),
                    x.EntriesSpan(dictionaryCount, collectionCount),
                    false,
                    true,
                    false
                )
        | :? array<KeyValuePair<'TKey, 'TValue>> as arr ->
            let dictionaryCount = x.Count
            x.Resize(arr.Length + dictionaryCount)

            for pair in arr do
                x.Add(pair.Key, pair.Value)
        | _ ->
            for pair in source do
                x.Add(pair.Key, pair.Value)

    member internal x.TryInsert (key, value, behavior) =
        x.FindEntry(
            key,
            (fun _ _ entryIndex ->
                match behavior with
                | PooledHash.OverwriteExisting -> entries.[entryIndex].Value <- value
                | PooledHash.ThrowOnExisting -> ThrowHelpers.ThrowAddingDuplicateWithKeyArgumentException(key)
                | _ -> ()
            ),
            (fun hashCode bucketIndex ->
                if count = entries.Length then x.Resize(count + 1)

                let entry = &entries.[count]
                let bucket = &buckets.[bucketIndex]
                entry.Key <- key
                entry.Value <- value
                entry.HashCode <- hashCode
                entry.Next <- bucket - 1
                count <- count + 1
                bucket <- count
            )

        )
