namespace CrossWind.Collections

open CrossWind.Runtime
open System
open HashHelpers
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Diagnostics

module rec PooledHash =
    open System.Collections

    [<NoComparison ; NoEquality>]
    type InsertionBehavior =
        /// <summary>
        /// The default insertion behavior.
        /// </summary>
        | None
        /// <summary>
        /// Specifies that an existing entry with the same key should be overwritten if encountered.
        /// </summary>
        | OverwriteExisting
        /// <summary>
        /// Specifies that if an existing entry with the same key is encountered, an exception should be thrown.
        /// </summary>
        | ThrowOnExisting

    type IKeyValuePair<'T, 'TKey, 'TValue> =
        abstract GetKey : entry : 'T -> 'TKey
        abstract GetValue : entry : 'T -> 'TValue
        abstract SetKey : key : 'TKey * entry : 'T -> 'T
        abstract SetValue : value : 'TValue * entry : 'T -> 'T
        abstract SetKeyValue : key : 'TKey * value : 'TValue -> 'T

    [<NoComparison ; NoEquality>]
    type DictionaryKeyValue<'TKey, 'TValue> =
        struct
            interface IKeyValuePair<KeyValuePair<'TKey, 'TValue>, 'TKey, 'TValue> with
                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.GetKey entry = entry.Key

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.GetValue entry = entry.Value

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.SetKey (key, entry) = KeyValuePair(key, entry.Value)

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.SetValue (value, entry) = KeyValuePair(entry.Key, value)

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.SetKeyValue (key, value) = KeyValuePair(key, value)
        end

    [<NoComparison ; NoEquality>]
    type HashSetKeyValue<'T> =
        struct
            interface IKeyValuePair<'T, 'T, 'T> with
                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.GetKey entry = entry

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.GetValue entry = entry

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.SetKey (key, _) = key

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.SetValue (value, _) = value

                [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
                override _.SetKeyValue (key, _) = key
        end

    [<NoComparison ; NoEquality>]
    type Entry<'T, 'TAccess> =
        struct
            val mutable HashCode : uint32
            val mutable Next : int32
            val mutable Entry : 'T
        end

    let inline GetBucketAndIndex
        hashCode
        (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>)
        (bucketIndex : _ outref)
        =
        bucketIndex <- FastMod hashCode state.entries.LongLength state.fastModMultiplier
        &state.hashBuckets.GetBucketReference bucketIndex

    let inline GetBucket hashCode (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        &state.hashBuckets.GetBucketReference(FastMod hashCode state.entries.LongLength state.fastModMultiplier)

    let inline GetBucketIndex hashCode (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        FastMod hashCode state.entries.LongLength state.fastModMultiplier

    let inline copyEntries
        (oldEntries : Entry<'T, 'TAccess> Span)
        (newEntries : _ Span)
        skipDeletions
        forceNewHashCodes
        resizeMode
        (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>)
        =
        let mutable newCount = 0

        for i = 0 to oldEntries.Length - 1 do
            let mutable entry = &newEntries.[newCount]
            entry <- oldEntries.[i]

            if skipDeletions
               || entry.Next <> Int32.MinValue then
                if forceNewHashCodes
                   && RuntimeHelpers.IsReferenceOrContainsReferences<'TKey>() then
                    entry.HashCode <-
                        entry.Entry
                        |> (Unchecked.defaultof<'TAccess>).GetKey
                        |> state.comparer.GetHashCode
                        |> uint

                let bucket = &GetBucket entry.HashCode state //&GetBucket state.buckets state.fastModMultiplier entry.HashCode
                entry.Next <- bucket

                if resizeMode then
                    bucket <- newCount
                else
                    bucket <- state.count
                    state.count <- state.count + 1

                newCount <- newCount + 1

    let inline ResizeAndHash
        capacity
        forceNewHashCodes
        (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>)
        =
        let bucket = GetBucketAndMultiplier(capacity, &state.fastModMultiplier)
        state.hashBuckets.Resize(capacity)

        if state.count > 0 then
            let oldEntries = state.entries
            state.entries <- bucket |> state.entryPool.RentFromBucket
            let oldSpan = oldEntries.AsSpan(0, state.count)
            let newSpan = state.entries.AsSpan(0, state.count)

            if state.freeCount = 0 then
                copyEntries oldSpan newSpan false forceNewHashCodes true state
            else
                copyEntries oldSpan newSpan true forceNewHashCodes true state

            state.entryPool.Return(oldEntries, RuntimeHelpers.IsReferenceOrContainsReferences<Entry<'T, 'TAccess>>())
        else
            state.entryPool.Return(state.entries, RuntimeHelpers.IsReferenceOrContainsReferences<Entry<'T, 'TAccess>>())
            state.entries <- bucket |> state.entryPool.RentFromBucket

    let Resize capacity (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        ResizeAndHash capacity false state

    let inline matchKeys
        (entry : Entry<'T, 'TAccess>)
        key
        (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>)
        =
        let entryKey = (Unchecked.defaultof<'TAccess>).GetKey entry.Entry

        if state.comparer.IsNull() then
            EqualityComparer.Default.Equals(entryKey, key)
        else
            // Reference Types always have a valid comparer in the state variable
            state.comparer.Equals(entryKey, key)

    let inline getHashCode value (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        if state.comparer.IsNull() then
            value
            |> EqualityComparer<'TKey>.Default.GetHashCode
        else
            value |> state.comparer.GetHashCode
        |> uint

    let inline GetBucketFromState hashCode (state : PooledHash<'T, 'TKey, 'TValue, _, _>) =
        FastMod hashCode state.hashBuckets.Length state.fastModMultiplier

    let inline FindEntryAndLastIndex
        keyToSearch
        returnLastEntry
        ([<InlineIfLambdaAttribute>] success)
        ([<InlineIfLambdaAttribute>] failure)
        (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>)
        =
        let mutable lastEntry = -1
        let hashCode = getHashCode keyToSearch state
        let bucket = GetBucketIndex hashCode state

        let mutable entryIndex =
            bucket
            |> state.hashBuckets.GetBucketReference

        while (if (uint) entryIndex < (uint) state.entries.Length then
                   let entry = &state.entries.[entryIndex]

                   if entry.HashCode = hashCode
                      && matchKeys entry keyToSearch state then
                       success hashCode bucket entryIndex lastEntry
                       false
                   else
                       if returnLastEntry then lastEntry <- entryIndex else entryIndex <- entry.Next

                       true
               else
                   failure hashCode bucket
                   false

        ) do
            ()

        entryIndex

    let inline FindEntry
        keyToSearch
        ([<InlineIfLambdaAttribute>] success)
        ([<InlineIfLambdaAttribute>] failure)
        (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>)
        =
        FindEntryAndLastIndex
            keyToSearch
            false
            (fun hashCode bucket entry _ -> success hashCode bucket entry)
            failure
            state

    let FindEntryIndex keyToSearch (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        FindEntry keyToSearch (fun _ _ _ -> ()) (fun _ _ -> ()) state

    let TryInsert key (value : 'TValue) behavior (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        let mutable ret = true
        let count = state.count

        if count = state.entries.Length then Resize(count + 1) state

        FindEntry
            key
            (fun _ _ entryIndex ->
                match behavior with
                | OverwriteExisting ->
                    let entry = &state.entries.[entryIndex]

                    entry.Entry <- (Unchecked.defaultof<'TAccess>).SetValue(value, entry.Entry)
                | ThrowOnExisting -> ThrowHelpers.ThrowAddingDuplicateWithKeyArgumentException(key)
                | _ -> ret <- false
            )
            (fun hashCode bucketIndex ->
                let entry = &state.entries.[count]
                let bucket = &state.hashBuckets.GetBucketReference(bucketIndex)

                entry.Entry <- (Unchecked.defaultof<'TAccess>).SetKeyValue(key, value)

                entry.HashCode <- hashCode
                entry.Next <- bucket
                state.count <- count + 1
                bucket <- state.count
            )
            state
        |> ignore

        ret

    let Remove key (state : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
        let entries = state.entries

        FindEntryAndLastIndex
            key
            true
            (fun _ bucket entryIndex lastEntry ->
                let mutable entry = entries.[entryIndex]

                match lastEntry with
                | -1 -> state.hashBuckets.[bucket] <- entry.Next + 1
                | _ -> entries.[lastEntry].Next <- entry.Next

                entry.Next <- Int32.MinValue
            )
            (fun _ _ -> ())
            state
        |> uint < uint (entries.Length)
    /// <summary>
    /// A hash implementation that uses pooled storage based on <see cref="Dictionary{TKey,TValue}"/> and <see href="https://github.com/jtmueller/Collections.Pooled"/>.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [<DebuggerDisplay("Count = {Count}") ;
      DebuggerTypeProxy(typeof<DebugView.ICollectionDebugView<_>>) ;
      NoComparison ;
      NoEquality>]
    type PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer when 'TAccess :> IKeyValuePair<'T, 'TKey, 'TValue> and 'TAccess : struct and 'KeyComparer :> IEqualityComparer<'TKey>> =
        val mutable count : int32
        val mutable freeCount : int32
        val mutable fastModMultiplier : uint64
        val mutable entryPool : PrimeSizedArrayPool<Entry<'T, 'TAccess>>
        val mutable hashBuckets : HashBucket
        val mutable entries : Entry<'T, 'TAccess> []
        val mutable comparer : 'KeyComparer

        new (capacity, keyComparer) =
            if capacity < 0 then
                ThrowHelpers.ThrowIndexArgumentOutOfRange_NeedNonNegNumException()

            let bucketIndex =
                capacity
                |> CollectionHelpers.SizeToIndex

            let pool = PrimeSizedArrayPool()

            { count = 0
              freeCount = 0
              fastModMultiplier = PrimeMultiplier.[bucketIndex]
              entryPool = pool
              hashBuckets = new HashBucket(PrimesDoublingInSize.[bucketIndex])
              entries = bucketIndex |> pool.RentFromBucket
              comparer = keyComparer |> ApplyComparer }

        new (capacity : int) =
            new PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>(
                capacity,
                if
                    typeof<'KeyComparer>.Equals (typeof<EqualityComparer<'TKey>>)
                    && RuntimeHelpers.IsReferenceOrContainsReferences<'TKey>()
                then
                    EqualityComparer<'TKey>.Default.As ()
                else
                    Unchecked.defaultof<_>
            )

        new (items : 'T [], keyComparer) =
            let pool = PrimeSizedArrayPool()

            let bucketIndex =
                items.Length
                |> CollectionHelpers.SizeToIndex

            let entries = bucketIndex |> pool.RentFromBucket
            Array.Copy(items, entries, items.Length)

            { count = items.Length
              freeCount = 0
              fastModMultiplier = PrimeMultiplier.[bucketIndex]
              entryPool = pool
              hashBuckets = new HashBucket(PrimesDoublingInSize.[bucketIndex])
              entries = entries
              comparer = keyComparer |> ApplyComparer }

        new (items : _ []) =
            new PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>(
                items,
                if
                    typeof<'KeyComparer>.Equals (typeof<EqualityComparer<'TKey>>)
                    && RuntimeHelpers.IsReferenceOrContainsReferences<'TKey>()
                then
                    EqualityComparer<'TKey>.Default.As ()
                else
                    Unchecked.defaultof<_>
            )

        member x.Add (key, value) =
            x
            |> TryInsert key value ThrowOnExisting
            |> ignore

        member x.ContainsKey key = (x |> FindEntryIndex key) <> -1
        member x.Count = x.count

        member x.Item
            with get key =
                x.entries.[FindEntry key (fun _ _ _ -> ()) (fun _ _ -> ThrowHelpers.ThrowKeyNotFoundException key) x]
                    .Entry
                |> (Unchecked.defaultof<'TAccess>).GetValue
            and set key value =
                x
                |> TryInsert key value OverwriteExisting
                |> ignore

        member x.Keys =
            (seq {
                for entry in x.entries do
                    yield
                        entry.Entry
                        |> (Unchecked.defaultof<'TAccess>).GetKey
            })

        member x.Remove key = x |> Remove key

        member x.Dispose () =
            x.hashBuckets.Dispose()
            x.entryPool.Return(x.entries)
            x.entries <- null

        interface IDisposable with
            [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
            override x.Dispose () = x.Dispose()

        interface IDictionary<'TKey, 'TValue> with
            member x.Add (key, value) = NotImplementedException() |> raise
            member x.ContainsKey (key) = x.ContainsKey key

            member x.Item
                with get key = NotImplementedException() |> raise
                and set key v = NotImplementedException() |> raise

            member x.Keys = NotImplementedException() |> raise
            member x.Remove key = x.Remove key
            member x.TryGetValue (key, value) = NotImplementedException() |> raise
            member x.Values = NotImplementedException() |> raise

        interface IEnumerable with
            member x.GetEnumerator () : IEnumerator = NotImplementedException() |> raise

        interface ICollection<KeyValuePair<'TKey, 'TValue>> with
            member x.Add item = NotImplementedException() |> raise
            member x.Clear () = NotImplementedException() |> raise
            member x.Contains item = NotImplementedException() |> raise
            member x.CopyTo (array, arrayIndex) = NotImplementedException() |> raise
            member x.Count = NotImplementedException() |> raise
            member x.IsReadOnly = NotImplementedException() |> raise
            member x.GetEnumerator () = NotImplementedException() |> raise
            member x.Remove item = NotImplementedException() |> raise
    /// <summary>
    /// A struct based Enumerator for <see cref="PooledDictionary{TKey, TValue}"/>.
    /// </summary>
    and [<Struct ; NoComparison ; NoEquality>] Enumerator<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer when 'TAccess :> IKeyValuePair<'T, 'TKey, 'TValue> and 'TAccess : struct and 'KeyComparer :> IEqualityComparer<'TKey>> =
        new (dictionary : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>) =
            { dictionary = dictionary ; index = 0 ; current = Unchecked.defaultof<_> }

        val mutable internal dictionary : PooledHash<'T, 'TKey, 'TValue, 'TAccess, 'KeyComparer>
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
        member x.MoveNext () : bool =
            if uint (x.index) < uint (x.dictionary.Count) then
                //x.current <- x.dictionary.[x.index]
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
            member x.Current : 'T = x.current
            member x.MoveNext () = x.MoveNext()
            member x.Reset () = x.Reset()
            member x.Dispose () = x.Dispose()

open PooledHash

type PooledHashSet<'T, 'Comparer when 'Comparer :> IEqualityComparer<'T>> =
    PooledHash<'T, 'T, 'T, HashSetKeyValue<'T>, 'Comparer>

type PooledDictionary<'TKey, 'TValue, 'KeyComparer when 'KeyComparer :> IEqualityComparer<'TKey>> =
    PooledHash<KeyValuePair<'TKey, 'TValue>, 'TKey, 'TValue, DictionaryKeyValue<'TKey, 'TValue>, 'KeyComparer>
