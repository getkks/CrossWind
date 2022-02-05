namespace CrossWind.Collections.Tests

open System
open System.Collections.Generic
open System.Linq
open Swensen.Unquote
open CrossWind.Runtime
open CrossWind.Tests

[<AbstractClass>]
type ``ICollection<'T> Tests``<'T when 'T: equality>() =
    inherit ``IEnumerable<'T> Tests``<'T>()
    abstract ``Add Remove and Clear throws NotSupported`` : bool
    default _.``Add Remove and Clear throws NotSupported`` = false

    abstract DefaultValueAllowed : bool
    default _.DefaultValueAllowed = true

    abstract ``Default Value when not allowed throws`` : bool
    default _.``Default Value when not allowed throws`` = true

    abstract DuplicateValuesAllowed : bool
    default _.DuplicateValuesAllowed = true

    abstract ``ICollection generic CopyTo index larger than array count throw type`` : Type
    default _.``ICollection generic CopyTo index larger than array count throw type`` = typeof<ArgumentException>

    abstract InvalidValues : 'T seq
    default _.InvalidValues: _ seq = Array.Empty<'T>()

    abstract IsReadOnly : bool
    default _.IsReadOnly = false

    abstract AddToCollection : collection: 'T ICollection * numberOfItemsToAdd: int -> unit

    default x.AddToCollection(collection, numberOfItemsToAdd) =
        let mutable seed = 9600
        let comparer = x.GetIEqualityComparer()

        while collection.Count < numberOfItemsToAdd do
            let toAdd = x.CreateT(seed)
            seed <- seed + 1

            while collection.Contains(toAdd, comparer)
                  || x.InvalidValues.Contains(toAdd, comparer) do
                let toAdd = x.CreateT(seed)
                seed <- seed + 1

            collection.Add toAdd

    /// <summary>
    /// Creates an instance of an ICollection{T} that can be used for testing.
    /// </summary>
    /// <returns>An instance of an ICollection{T} that can be used for testing.</returns>
    abstract GenericICollectionFactory : unit -> 'T ICollection

    /// <summary>
    /// Creates an instance of an ICollection{T} that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned ICollection{T} contains.</param>
    /// <returns>An instance of an ICollection{T} that can be used for testing.</returns>
    abstract GenericICollectionFactory : count: int -> 'T ICollection

    default x.GenericICollectionFactory count =
        let collection = x.GenericICollectionFactory()
        x.AddToCollection(collection, count)
        collection

    override x.GenericIEnumerableFactory count = x.GenericICollectionFactory count

    override x.GetModifyEnumerables operations =
        seq {
            if x.``Add Remove and Clear throws NotSupported`` then
                match operations with
                | Add ->
                    ModifyEnumerable<'T> (fun enumerable ->
                        let casted = enumerable.As<_, 'T ICollection>()
                        2344 |> x.CreateT |> casted.Add
                        true)
                | Remove ->
                    ModifyEnumerable<'T> (fun enumerable ->
                        let casted = enumerable.As<_, 'T ICollection>()

                        if casted.Count > 0 then
                            0 |> casted.ElementAt |> casted.Remove |> ignore

                            true
                        else
                            false)
                | Clear ->
                    ModifyEnumerable<'T> (fun enumerable ->
                        let casted = enumerable.As<_, 'T ICollection>()

                        if casted.Count > 0 then
                            casted.Clear()
                            true
                        else
                            false)
                | _ -> ()
        }

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add after Clear() succeeds`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            collection.Clear()
            x.AddToCollection(collection, 5)
            test <@ collection.Count = 5 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add after Remove() succeeds`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let mutable seed = 840
            let collection = count |> x.GenericICollectionFactory
            let mutable toAdd = x.CreateT(seed)

            while collection.Contains(toAdd) do
                seed <- seed + 1
                toAdd <- x.CreateT(seed)

            collection.Add toAdd
            toAdd |> collection.Remove |> ignore
            collection.Add toAdd

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add after Remove() for all items succeeds`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let mutable seed = 840
            let collection = count |> x.GenericICollectionFactory

            for _ = 0 to count - 1 do
                0
                |> collection.ElementAt
                |> collection.Remove
                |> ignore

            x.CreateT(seed) |> collection.Add
            test <@ collection.Count = 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add after Remove() any item succeeds`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let mutable seed = 840
            let collection = count |> x.GenericICollectionFactory
            let items = collection.ToList()
            let mutable toAdd = x.CreateT(seed)

            while collection.Contains(toAdd) do
                seed <- seed + 1
                toAdd <- x.CreateT(seed)

            collection.Add toAdd
            toAdd |> collection.Remove |> ignore
            seed <- seed + 1
            toAdd <- x.CreateT(seed)

            while collection.Contains(toAdd) do
                seed <- seed + 1
                toAdd <- x.CreateT(seed)

            toAdd |> collection.Add
            toAdd |> items.Add
            CollectionEquality.EqualUnOrdered(collection, items)

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add default value`` count =
        if x.DefaultValueAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            Unchecked.defaultof<_> |> collection.Add
            test <@ collection.Count = count + 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add duplicate value`` count =
        if x.DuplicateValuesAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            let duplicateValue = x.CreateT(700)
            duplicateValue |> collection.Add
            duplicateValue |> collection.Add
            test <@ collection.Count = count + 2 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add invalid value at the begining of collection`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            for invalidValue in x.InvalidValues do
                let collection = 0 |> x.GenericICollectionFactory
                invalidValue |> collection.Add

                for i = 1 to count do
                    i |> x.CreateT |> collection.Add

                test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add invalid value at the end of collection`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            for invalidValue in x.InvalidValues do
                let collection = count |> x.GenericICollectionFactory
                invalidValue |> collection.Add
                test <@ collection.Count = count * 2 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add invalid value in the middle of collection`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            for invalidValue in x.InvalidValues do
                let collection = count |> x.GenericICollectionFactory
                invalidValue |> collection.Add

                for i = 1 to count do
                    i |> x.CreateT |> collection.Add

                test <@ collection.Count = count * 2 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Add to readonly collection`` count =
        if x.IsReadOnly
           || x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            raises<NotSupportedException> <@ 0 |> x.CreateT |> collection.Add @>
            test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Clear()`` count =
        let collection = count |> x.GenericICollectionFactory
        let mutable count = count

        match x.IsReadOnly
              || x.``Add Remove and Clear throws NotSupported``
            with
        | true -> raises<NotSupportedException> <@ collection.Clear() @>
        | _ ->
            count <- 0
            collection.Clear()

        test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Clear() called repeatedly`` count =
        let collection = count |> x.GenericICollectionFactory
        let mutable count = count

        match x.IsReadOnly
              || x.``Add Remove and Clear throws NotSupported``
            with
        | true ->
            for _ = 1 to 3 do
                raises<NotSupportedException> <@ collection.Clear() @>
        | _ ->
            count <- 0

            for _ = 1 to 3 do
                collection.Clear()

        test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Contains() on collection containing default value`` count =
        if x.DefaultValueAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            Unchecked.defaultof<_> |> collection.Add
            test <@ collection.Contains Unchecked.defaultof<_> = true @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Contains() on collection not containing default value`` count =
        if x.DefaultValueAllowed then
            let collection = count |> x.GenericICollectionFactory
            test <@ collection.Contains Unchecked.defaultof<_> = false @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Contains() on collection when default value is not allowed`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory

            match x.``Default Value when not allowed throws`` with
            | true -> raises<NotSupportedException> <@ collection.Contains Unchecked.defaultof<_> @>
            | _ -> test <@ collection.Contains Unchecked.defaultof<_> = false @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Contains() throws ArgumentException for invalid values`` count =
        let collection = count |> x.GenericICollectionFactory

        for invalidValue in x.InvalidValues do
            raises<ArgumentException> <@ collection.Contains invalidValue @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Contains() valid values present in collection`` count =
        let collection = count |> x.GenericICollectionFactory

        for value in collection do
            test <@ collection.Contains value = true @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Contains() valid values not present in collection`` count =
        let collection = count |> x.GenericICollectionFactory
        let mutable seed = 4315
        let mutable value = seed |> x.CreateT

        while value |> collection.Contains do
            seed <- seed + 1
            value <- seed |> x.CreateT

        test <@ collection.Contains value = false @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> duplicate valid values`` count =
        if x.DuplicateValuesAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            let value = 12 |> x.CreateT

            for _ = 1 to 2 do
                value |> collection.Add

            test <@ collection.Count = count + 2 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() larger array`` count =
        let collection = count |> x.GenericICollectionFactory
        let arr = (count * 3 / 2) |> Array.zeroCreate
        collection.CopyTo(arr, 0)
        test <@ Enumerable.SequenceEqual(collection, arr.Take count) = true @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() same size array`` count =
        let collection = count |> x.GenericICollectionFactory
        let arr = count |> Array.zeroCreate
        collection.CopyTo(arr, 0)
        test <@ Enumerable.SequenceEqual(collection, arr.Take count) = true @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() index equal to array size throws ArgumentException`` count =
        let collection = count |> x.GenericICollectionFactory
        let arr = count |> Array.zeroCreate

        if count > 0 then
            raises<ArgumentException> <@ collection.CopyTo(arr, count) @>
        else
            collection.CopyTo(arr, count)

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() index larger than array size throws ArgumentException`` count =
        let collection = count |> x.GenericICollectionFactory
        let arr = count |> Array.zeroCreate
        raises<ArgumentException> <@ collection.CopyTo(arr, count + 1) @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() negative index throws ArgumentException`` count =
        let collection = count |> x.GenericICollectionFactory
        let arr = count |> Array.zeroCreate
        raises<ArgumentException> <@ collection.CopyTo(arr, -1) @>
        raises<ArgumentException> <@ collection.CopyTo(arr, Int32.MinValue) @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() with index and not enough space throws ArgumentException`` count =
        if count > 0 then
            let collection = count |> x.GenericICollectionFactory
            let arr = count |> Array.zeroCreate
            raises<ArgumentException> <@ collection.CopyTo(arr, 1) @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> CopyTo() null array throws ArgumentNullException`` count =
        let collection = count |> x.GenericICollectionFactory
        raises<ArgumentNullException> <@ collection.CopyTo(null, 0) @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Count`` count =
        let collection = count |> x.GenericICollectionFactory
        test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> IsReadOnly`` count =
        let collection = count |> x.GenericICollectionFactory
        let IsReadOnly = x.IsReadOnly
        test <@ collection.IsReadOnly = IsReadOnly @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() default value present`` count =
        if x.DefaultValueAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported``
           && Enumerable.Contains(x.InvalidValues, Unchecked.defaultof<_>)
              |> not then
            let collection = count |> x.GenericICollectionFactory
            let value = Unchecked.defaultof<_>
            let mutable count = count

            match value |> collection.Contains with
            | true -> count <- count - 1
            | _ -> collection.Add value

            test <@ collection.Remove value = true @>
            test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() default value not present`` count =
        if x.DefaultValueAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported``
           && Enumerable.Contains(x.InvalidValues, Unchecked.defaultof<_>)
              |> not then
            let collection = count |> x.GenericICollectionFactory
            let value = Unchecked.defaultof<_>
            let mutable count = count

            while collection.Contains value do
                count <- count - 1
                value |> collection.Remove |> ignore

            test <@ collection.Remove value = false @>
            test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() default value when not allowed`` count =
        if not x.DefaultValueAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            let value = Unchecked.defaultof<_>

            if x.``Default Value when not allowed throws`` then
                raises<InvalidOperationException> <@ value |> collection.Remove @>
            else
                test <@ collection.Remove value = true @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() all values`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory

            test
                <@ collection.ToArray()
                   |> Array.forall (fun value -> collection.Remove value) @>

            test <@ collection.Count = 0 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() invalid values throws ArgumentException`` count =
        let collection = count |> x.GenericICollectionFactory

        for invalidValue in x.InvalidValues do
            raises<NotSupportedException> <@ invalidValue |> collection.Remove @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() value present`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            let seed = count * 251
            let value = seed |> x.CreateT
            let mutable count = count

            match value |> collection.Contains with
            | true -> count <- count - 1
            | _ -> collection.Add value

            test <@ collection.Remove value = true @>
            test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() value not present`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            let mutable seed = count * 251
            let mutable value = seed |> x.CreateT

            while collection.Contains value
                  || Enumerable.Contains(x.InvalidValues, value) do
                value <- seed |> x.CreateT
                seed <- seed + 1

            test <@ collection.Remove value = false @>
            test <@ collection.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() on readonly collection throws NotSupportedException`` count =
        if x.IsReadOnly
           || x.``Add Remove and Clear throws NotSupported`` then
            raises<NotSupportedException>
                <@ 34543
                   |> x.CreateT
                   |> x.GenericICollectionFactory(count).Remove @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> Remove() value that is repeated in the collection`` count =
        if x.DuplicateValuesAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericICollectionFactory
            let mutable seed = count * 251
            let mutable value = seed |> x.CreateT
            collection.Add value
            collection.Add value
            test <@ collection.Remove value = true @>
            test <@ collection.Count = count + 1 @>
