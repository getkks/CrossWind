namespace CrossWind.Collections.Tests

open System
open System.Collections.Generic
open System.Linq
open Swensen.Unquote
open CrossWind.Runtime
open CrossWind.Tests

[<AbstractClass>]
type ``IList Generic Tests``<'T when 'T: equality>() =
    inherit ``ICollection<'T> Tests``<'T>()

    override _.``Default Value when not allowed throws`` = false

    abstract GenericIListFactory : unit -> 'T IList
    abstract GenericIListFactory : count: int -> 'T IList

    default x.GenericIListFactory count =
        let collection = x.GenericIListFactory()
        x.AddToCollection(collection, count)
        collection

    override x.GenericICollectionFactory count = x.GenericIListFactory count
    override x.GenericICollectionFactory() = x.GenericIListFactory()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Current after enumeration and adding values to list`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let collection = count |> x.GenericIListFactory
            use enumerator = collection.GetEnumerator()

            while enumerator.MoveNext() do
                ()

            match x.``IEnumerator<'T>.Current throws Undefined Operation`` with
            | true -> raises<InvalidOperationException> <@ enumerator.Current @>
            | _ -> test <@ enumerator.Current = Unchecked.defaultof<_> @>

            for seed = 3538963 to 3538963 + 3 do
                seed |> x.CreateT |> collection.Add

            match x.``IEnumerator<'T>.Current throws Undefined Operation`` with
            | true -> raises<InvalidOperationException> <@ enumerator.Current @>
            | _ -> test <@ enumerator.Current = Unchecked.defaultof<_> @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() default value in the list`` count =
        if count > 0
           && x.DefaultValueAllowed
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            let value = Unchecked.defaultof<_>

            match not x.IsReadOnly && value |> list.Contains |> not with
            | true -> list.[0] <- value
            | _ -> ()

            test <@ list.IndexOf value = 0 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() default value not in the list`` count =
        let list = count |> x.GenericIListFactory
        let value = Unchecked.defaultof<_>

        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            match x.DefaultValueAllowed with
            | true ->

                match count > 0
                      && not x.IsReadOnly
                      && value |> list.Contains |> not
                    with
                | true ->
                    list.Remove value |> ignore
                    test <@ list.IndexOf value = -1 @>
                | _ -> ()
            | _ -> raises<ArgumentNullException> <@ list.IndexOf value @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() each value in the list`` count =
        let list = count |> x.GenericIListFactory

        for index = 0 to count - 1 do
            let value = list.[index]
            test <@ list.IndexOf value = index @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() invalid value`` count =
        let list = count |> x.GenericIListFactory

        for invalidValue in x.InvalidValues do
            raises<ArgumentException> <@ list.IndexOf invalidValue @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() returns first match`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            let expected = list.ToArray()

            for value in expected do
                list.Add value

            for index = 0 to count - 1 do
                test <@ list.IndexOf expected.[index] = index @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() valid value not in the list`` count =
        let list = count |> x.GenericIListFactory
        let mutable seed = 54321
        let mutable value = seed |> x.CreateT

        while list.Contains value do
            seed <- seed + 1
            value <- seed |> x.CreateT

        test <@ list.IndexOf value = -1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> IndexOf() repeated valid value in the list`` count =
        if count > 0
           && x.DuplicateValuesAllowed
           && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let mutable value = 12345 |> x.CreateT

            list.[0] <- value
            list.[count / 2] <- value

            test <@ list.IndexOf value = 0 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() duplicate values`` count =
        if x.DuplicateValuesAllowed && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT

            match x.``Add Remove and Clear throws NotSupported`` with
            | true -> raises<NotSupportedException> <@ list.Insert(0, value) @>
            | _ ->
                list.Insert(0, value)
                list.Insert(1, value)

            test
                <@ list.[0] = value
                   && list.[1] = value
                   && list.Count = count + 2 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() default value as first element`` count =
        if not x.``Add Remove and Clear throws NotSupported``
           && not x.IsReadOnly
           && x.DefaultValueAllowed then
            let list = count |> x.GenericIListFactory
            let value = Unchecked.defaultof<_>
            list.Insert(0, value)
            test <@ list.[0] = value && list.Count = count + 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() value as first element`` count =
        if not x.``Add Remove and Clear throws NotSupported``
           && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            list.Insert(0, value)
            test <@ list.[0] = value && list.Count = count + 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() at Count`` count =
        if not x.``Add Remove and Clear throws NotSupported``
           && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            list.Insert(count, value)
            test <@ list.[count] = value && list.Count = count + 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() invalid values`` count =
        if not x.``Add Remove and Clear throws NotSupported``
           && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory

            for invalidValue in x.InvalidValues do
                raises<ArgumentException> <@ list.Insert(count / 2, invalidValue) @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() default value as last element`` count =
        if not x.``Add Remove and Clear throws NotSupported``
           && not x.IsReadOnly
           && x.DefaultValueAllowed then
            let list = count |> x.GenericIListFactory
            let value = Unchecked.defaultof<_>
            let lastIndex = if count > 0 then count - 1 else 0
            list.Insert(lastIndex, value)
            test <@ list.[lastIndex] = value && list.Count = count + 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() value as last element`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            let lastIndex = if count > 0 then count - 1 else 0
            list.Insert(lastIndex, value)
            test <@ list.[lastIndex] = value && list.Count = count + 1 @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() at negative index throws ArgumentOutOfRange Exception`` count =
        if not x.``Add Remove and Clear throws NotSupported``
           && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            raises<ArgumentOutOfRangeException> <@ list.Insert(-1, value) @>
            raises<ArgumentOutOfRangeException> <@ list.Insert(Int32.MinValue, value) @>
            test <@ list.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Insert() on readonly list throws NotSupportedException`` count =
        if x.``Add Remove and Clear throws NotSupported``
           && x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            raises<ArgumentOutOfRangeException> <@ list.Insert(count / 2, value) @>
            test <@ list.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item index greater than or equal to list count throws ArgumentOutOfRangeException`` count =
        let list = count |> x.GenericIListFactory
        raises<ArgumentOutOfRangeException> <@ list.[count] @>
        raises<ArgumentOutOfRangeException> <@ list.[count + 1] @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item negative index throws ArgumentOutOfRangeException`` count =
        let list = count |> x.GenericIListFactory
        raises<ArgumentOutOfRangeException> <@ list.[-1] @>
        raises<ArgumentOutOfRangeException> <@ list.[Int32.MinValue] @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item within list bounds`` count =
        let list = count |> x.GenericIListFactory

        for i = 0 to count - 1 do
            list.[i] |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set duplicate values`` count =
        if count > 2
           && x.DuplicateValuesAllowed
           && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            list.[0] <- value
            list.[1] <- value
            test <@ list.[0] = value && list.[1] = value @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set first element to default value`` count =
        if count > 0 && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = Unchecked.defaultof<_>

            match x.DefaultValueAllowed with
            | true ->
                list.[0] <- value
                test <@ list.[0] = value @>
            | _ ->
                raises<InvalidOperationException> <@ list.[0] <- value @>
                test <@ list.[0] <> value @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set first element`` count =
        if count > 0 && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            list.[0] <- value
            test <@ list.[0] = value @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set index greater than or equal to list count throws ArgumentOutOfRangeException`` count =
        if not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            raises<ArgumentOutOfRangeException> <@ list.[count] <- value @>
            raises<ArgumentOutOfRangeException> <@ list.[count + 1] <- value @>
            test <@ list.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set invalid values`` count =
        if count > 0 && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory

            for invalidValue in x.InvalidValues do
                raises<ArgumentException> <@ list.[count / 2] <- invalidValue @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set default value as last element`` count =
        if count > 0 && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = Unchecked.defaultof<_>
            let lastIndex = if count > 0 then count - 1 else 0

            match x.DefaultValueAllowed with
            | true ->
                list.[lastIndex] <- value
                test <@ list.[lastIndex] = value @>
            | _ ->
                raises<ArgumentNullException> <@ list.[lastIndex] <- value @>
                test <@ list.[lastIndex] <> value @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set value as last element`` count =
        if count > 0 && not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            let lastIndex = if count > 0 then count - 1 else 0
            list.[lastIndex] <- value
            test <@ list.[lastIndex] = value @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set negative index throws ArgumentOutOfRangeException`` count =
        if not x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            raises<ArgumentOutOfRangeException> <@ list.[-1] <- value @>
            raises<ArgumentOutOfRangeException> <@ list.[Int32.MinValue] <- value @>
            test <@ list.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> Item set on readonly list`` count =
        if count > 0 && x.IsReadOnly then
            let list = count |> x.GenericIListFactory
            let value = 12345 |> x.CreateT
            let oldValue = list.[count / 2]
            raises<NotSupportedException> <@ list.[count / 2] <- value @>
            test <@ list.[count / 2] = oldValue @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> RemoveAt all indices`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory

            for i = count - 1 downto 0 do
                list.RemoveAt(i)
                test <@ list.Count = i @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> RemoveAt index greater than or equal to list count throws ArgumentOutOfRange`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            raises<ArgumentOutOfRangeException> <@ list.RemoveAt(count) @>
            raises<ArgumentOutOfRangeException> <@ list.RemoveAt(count + 1) @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> RemoveAt negative index throws ArgumentOutOfRange`` count =
        if not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            raises<ArgumentOutOfRangeException> <@ list.RemoveAt(-1) @>
            raises<ArgumentOutOfRangeException> <@ list.RemoveAt(Int32.MinValue) @>
            test <@ list.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> RemoveAt on readonly list throws NotSupported`` count =
        if x.IsReadOnly
           || x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            raises<NotSupportedException> <@ list.RemoveAt(count / 2) @>
            test <@ list.Count = count @>

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IList<'T> RemoveAt zero index multiple times`` count =
        if count = 1
           && not x.IsReadOnly
           && not x.``Add Remove and Clear throws NotSupported`` then
            let list = count |> x.GenericIListFactory
            let mutable i = 0

            while i < list.Count do
                list.RemoveAt(0)
                test <@ list.Count = count - i - 1 @>
                i <- i + 1
