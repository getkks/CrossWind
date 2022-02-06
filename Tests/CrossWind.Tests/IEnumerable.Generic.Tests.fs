namespace CrossWind.Collections.Tests

open Shouldly
open System
open System.Collections
open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.Runtime.InteropServices
open CrossWind.Collections
open CrossWind.Tests

/// <summary>
/// An discrete union to allow specification of the order of the Enumerable. Used in validation for enumerables.
/// </summary>
type EnumerableOrder =
    | Unspecified
    | Sequential

/// <summary>
/// Modifies the given IEnumerable such that any enumerators for that IEnumerable will be
/// invalidated.
/// </summary>
/// <param name="enumerable">An IEnumerable to modify</param>
/// <returns>true if the enumerable was successfully modified. Else false.</returns>
type ModifyEnumerable<'T> = delegate of enumerable: 'T seq -> bool

/// <summary>
/// Contains tests that ensure the correctness of any class that implements the generic
/// IEnumerable interface.
/// </summary>
[<AbstractClass>]
type ``IEnumerable<'T> Tests``<'T when 'T: equality>() =
    inherit TestBase<'T>()
    /// <summary>
    /// Creates an instance of an IEnumerable{T} that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned IEnumerable{T} contains.</param>
    /// <returns>An instance of an IEnumerable{T} that can be used for testing.</returns>
    abstract GenericIEnumerableFactory : count: int -> 'T seq
    /// <summary>
    /// To be implemented in the concrete collections test classes. Returns a set of ModifyEnumerable delegates
    /// that modify the enumerable passed to them.
    /// </summary>
    abstract GetModifyEnumerables : operations: ModifyOperation -> 'T ModifyEnumerable seq

    member _.ModifyEnumeratorThrows operations =
        match operations with
        | Add
        | Insert
        | Remove
        | Clear -> true
        | _ -> false

    member _.ModifyEnumeratorAllowed = ModifyOperation.None
    /// <summary>
    /// The Reset method is provided for COM interoperability. It does not necessarily need to be
    /// implemented; instead, the implementer can simply throw a NotSupportedException.
    ///
    /// If Reset is not implemented, this property must return False. The default value is true.
    /// </summary>
    member _.ResetImplemented = true
    /// <summary>
    /// When calling Current of the enumerator before the first MoveNext, after the end of the collection,
    /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
    /// to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Returning an undefined value.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// false.
    /// </summary>
    member _.``IEnumerator<'T>.Current throws Undefined Operation`` = false
    /// <summary>
    /// When calling MoveNext or Reset after modification of the enumeration, the resulting behavior is
    /// undefined. Tests are included to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Execute MoveNext or Reset.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// true.
    /// </summary>
    member _.``IEnumerable<'T> modified during enumeration throws InvalidOperationException`` = true
    /// <summary>
    /// Specifies whether this IEnumerable follows some sort of ordering pattern.
    /// </summary>
    member _.Order = Sequential

    member private x.RepeatTest(testCode, [<Optional; DefaultParameterValue(3)>] iters) =
        let enumerable = x.GenericIEnumerableFactory(32)
        let items = enumerable.ToArray()
        let mutable enumerator = enumerable.GetEnumerator()

        for i = 0 to iters - 1 do
            testCode (enumerator, items, i)

            match x.ResetImplemented with
            | true -> enumerator.Reset()
            | _ -> enumerator <- enumerable.GetEnumerator()

    member private x.RepeatTest(testCode, [<Optional; DefaultParameterValue(3)>] iters: int) =
        x.RepeatTest((fun (e, i, it) -> testCode (e, i)), iters)

    member private x.VerifyEnumerator
        (
            enumerator: 'T IEnumerator,
            expectedItems: _ [],
            startIndex,
            count,
            validateStart,
            validateEnd
        ) =
        let needToMatchAllExpectedItems =
            count - startIndex = expectedItems.Length

        if validateStart then
            for _ = 0 to 2 do
                if x.``IEnumerator<'T>.Current throws Undefined Operation`` then
                    (fun () -> enumerator.Current :> obj)
                    |> Should.Throw<InvalidOperationException>
                    |> ignore
                else
                    enumerator.Current |> ignore

        let mutable iterations = 0

        match x.Order with
        | Unspecified ->
            let itemsVisited =
                BitArray(
                    (if needToMatchAllExpectedItems then
                         count
                     else
                         expectedItems.Length),
                    false
                )

            while iterations < count && enumerator.MoveNext() do
                let currentItem = enumerator.Current
                let mutable itemFound = false
                let mutable i = 0

                while not itemFound && i < itemsVisited.Length do
                    if
                        not itemsVisited.[i]
                        && obj.Equals
                            (
                                currentItem,
                                expectedItems.[i
                                               + if needToMatchAllExpectedItems then
                                                     0
                                                 else
                                                     startIndex]
                            )
                    then
                        itemsVisited.[i] <- true
                        itemFound <- true

                    i <- i + 1

                itemFound.ShouldBeTrue()

                for _ = 0 to 2 do
                    currentItem.ShouldBe(enumerator.Current)

                iterations <- iterations + 1

            if needToMatchAllExpectedItems then
                for i = 0 to itemsVisited.Length - 1 do
                    itemsVisited.[i].ShouldBeTrue()
            else
                let mutable visitedItemCount = 0

                for i = 0 to itemsVisited.Length - 1 do
                    if itemsVisited.[i] then
                        visitedItemCount <- visitedItemCount + 1

                count.ShouldBe(visitedItemCount)
        | Sequential ->
            while iterations < count && enumerator.MoveNext() do
                let currentItem = enumerator.Current

                expectedItems.[iterations
                               + if needToMatchAllExpectedItems then
                                     0
                                 else
                                     startIndex]
                    .ShouldBe(currentItem)

                for _ = 0 to 2 do
                    currentItem.ShouldBe(enumerator.Current)

                iterations <- iterations + 1

        count.ShouldBe(iterations)

        if validateEnd then
            for _ = 0 to 2 do
                enumerator
                    .MoveNext()
                    .ShouldBeFalse("enumerator.MoveNext() returned true past the expected end.")

                if x.``IEnumerator<'T>.Current throws Undefined Operation`` then
                    (fun () -> enumerator.Current :> obj)
                    |> Should.Throw<InvalidOperationException>
                    |> ignore
                else
                    enumerator.Current |> ignore

    member x.VerifyEnumerator(enumerator, expectedItems) =
        x.VerifyEnumerator(enumerator, expectedItems, 0, expectedItems.Length, true, true)

    member x.VerifyModifiedEnumerator(enumerator: 'T IEnumerator, expectedCurrent, expectCurrentThrow, _) =
        if expectCurrentThrow then
            (fun () -> enumerator.Current :> obj)
            |> Should.Throw<InvalidOperationException>
            |> ignore
        else
            let mutable current = enumerator.Current

            for _ = 0 to 2 do
                current.ShouldBe(expectedCurrent)
                current <- enumerator.Current

        (fun () -> enumerator.MoveNext() |> ignore)
        |> Should.Throw<InvalidOperationException>
        |> ignore

        if not x.ResetImplemented then
            (fun () -> enumerator.Reset())
            |> Should.Throw<InvalidOperationException>
            |> ignore


    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> MoveNext() after Dispose()`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        let enumerator = enumerable.GetEnumerator()

        for _ = 0 to count - 1 do
            enumerator.MoveNext() |> ignore

        enumerator.ShouldSatisfyAllConditions(
            (fun () -> enumerator.Dispose()),
            (fun () -> enumerator.MoveNext() |> ignore)
        )

    [<Test>]
    member x.``IEnumerator<'T> Current returns proper result``() =
        x.RepeatTest (fun (enumerator, items, iteration) ->
            if iteration = 1 then
                x.VerifyEnumerator(enumerator, items, 0, items.Length / 2, true, false)
            else
                x.VerifyEnumerator(enumerator, items))

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Current when MoveNext() = false has UndefinedBehavior`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        use enumerator = enumerable.GetEnumerator()

        while enumerator.MoveNext() do
            ()

        if x.``IEnumerator<'T>.Current throws Undefined Operation`` then
            (fun () -> enumerator.Current |> ignore)
            |> Should.Throw<InvalidOperationException>
            |> ignore
        else
            enumerator.Current |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Current has UndefinedBehavior before MoveNext()`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        use enumerator = enumerable.GetEnumerator()

        if x.``IEnumerator<'T>.Current throws Undefined Operation`` then
            (fun () -> enumerator.Current |> ignore)
            |> Should.Throw<InvalidOperationException>
            |> ignore
        else
            enumerator.Current |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification succeeds during enumeration`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            if modifyEnumberable.Invoke(enumerable) then
                Should.NotThrow(fun () -> enumerator.Current |> ignore)

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification during enumeration has UndefinedBehavior`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            if modifyEnumberable.Invoke(enumerable) then
                if x.``IEnumerator<'T>.Current throws Undefined Operation`` then
                    (fun () -> enumerator.Current |> ignore)
                    |> Should.Throw<InvalidOperationException>
                    |> ignore
                else
                    enumerator.Current |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> returns same object on different IEnumerator<'T>`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        let firstValues = Dictionary(count)
        let secondValues = Dictionary(count)

        for item in enumerable do
            match firstValues.TryGetValue(item) with
            | true, value -> firstValues.[item] <- value + 1
            | _ -> firstValues.[item] <- 1

        for item in enumerable do
            match secondValues.TryGetValue(item) with
            | true, value -> secondValues.[item] <- value + 1
            | _ -> secondValues.[item] <- 1

        firstValues.ShouldBe(secondValues)

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Current returns same value on repeated calls`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        use enumerator = enumerable.GetEnumerator()

        while enumerator.MoveNext() do
            let current = enumerator.Current

            enumerator.ShouldSatisfyAllConditions(
                (fun () -> enumerator.Current.ShouldBe(current)),
                (fun () -> enumerator.Current.ShouldBe(current)),
                (fun () -> enumerator.Current.ShouldBe(current))
            )

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> MoveNext() should stay false after enumeration`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        use enumerator = enumerable.GetEnumerator()

        for _ = 0 to count - 1 do
            enumerator.MoveNext() |> ignore

        enumerator.ShouldSatisfyAllConditions(
            (fun () -> enumerator.MoveNext().ShouldBeFalse()),
            (fun () -> enumerator.MoveNext().ShouldBeFalse())
        )

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> MoveNext() should cover all values during enumeration`` count =
        let mutable iterations = 0
        let enumerable = x.GenericIEnumerableFactory(count)
        use enumerator = enumerable.GetEnumerator()

        while enumerator.MoveNext() do
            iterations <- iterations + 1

        iterations.ShouldBe(count)

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification after enumeration should succeed`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            while enumerator.MoveNext() do
                ()

            (modifyEnumberable.Invoke(enumerable)
             && Should.NotThrow(fun () -> enumerator.MoveNext()))
                .ShouldBeTrue()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification after enumeration throws InvalidOperationException`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            while enumerator.MoveNext() do
                ()

            if
                modifyEnumberable.Invoke(enumerable)
                && x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException``
            then
                (fun () -> enumerator.MoveNext() |> ignore)
                |> Should.Throw<InvalidOperationException>
                |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification before enumeration should succeed`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            if
                modifyEnumberable.Invoke(enumerable)
                && x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException``
            then
                (fun () -> enumerator.MoveNext() |> ignore)
                |> Should.Throw<InvalidOperationException>
                |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification before enumeration throws InvalidOperationException`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            if modifyEnumberable.Invoke(enumerable) then
                if x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException`` then
                    (fun () -> enumerator.MoveNext() |> ignore)
                    |> Should.Throw<InvalidOperationException>
                    |> ignore
                else
                    enumerator.MoveNext() |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification during enumeration should succeed`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            for _ = 0 to count / 2 - 1 do
                enumerator.MoveNext() |> ignore

            (modifyEnumberable.Invoke(enumerable)
             && Should.NotThrow(fun () -> enumerator.MoveNext()))
                .ShouldBeTrue()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable<'T> modification during enumeration throws InvalidOperationException`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            for _ = 0 to count / 2 - 1 do
                enumerator.MoveNext() |> ignore

            if modifyEnumberable.Invoke(enumerable) then
                if x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException`` then
                    (fun () -> enumerator.MoveNext() |> ignore)
                    |> Should.Throw<InvalidOperationException>
                    |> ignore
                else
                    enumerator.MoveNext() |> ignore

    [<Test>]
    member x.``IEnumerator<'T> MoveNext() should be false at collection end``() =
        x.RepeatTest (fun (enumerator: _ IEnumerator, _) ->
            while enumerator.MoveNext() do
                ()

            enumerator.MoveNext().ShouldBeFalse())

    [<Test>]
    member x.``IEnumerator<'T> MoveNext() reaches all items``() =
        x.RepeatTest (fun (enumerator: _ IEnumerator, items: _ []) ->
            let mutable iterations = 0

            while enumerator.MoveNext() do
                iterations <- iterations + 1

            items.Length.ShouldBe(iterations))

    [<Test>]
    member x.``IEnumerator<'T> Reset()``() =
        match x.ResetImplemented with
        | true ->
            x.RepeatTest (fun (enumerator: _ IEnumerator, items: _ [], iteration) ->
                match iteration with
                | 1 ->
                    x.VerifyEnumerator(enumerator, items, 0, items.Length / 2, true, false)
                    enumerator.Reset()
                    enumerator.Reset()
                | 3 ->
                    x.VerifyEnumerator(enumerator, items)
                    enumerator.Reset()
                    enumerator.Reset()
                | _ -> x.VerifyEnumerator(enumerator, items))
        | _ ->
            x.RepeatTest (fun (enumerator: _ IEnumerator, items: _ []) ->
                Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
                |> ignore)

            x.RepeatTest (fun (enumerator: _ IEnumerator, items: _ [], iteration) ->
                match iteration with
                | 1 ->
                    let halfLength = items.Length / 2
                    x.VerifyEnumerator(enumerator, items, 0, halfLength, true, false)

                    for _ = 0 to 2 do
                        Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
                        |> ignore

                    x.VerifyEnumerator(enumerator, items, halfLength, halfLength, false, true)
                | 2 ->
                    x.VerifyEnumerator(enumerator, items)

                    for _ = 0 to 2 do
                        Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
                        |> ignore

                    x.VerifyEnumerator(enumerator, items, 0, 0, false, true)
                | _ -> x.VerifyEnumerator(enumerator, items))

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() before enumeration`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        use enumerator = enumerable.GetEnumerator()

        match x.ResetImplemented with
        | true ->
            Should.NotThrow(fun () -> enumerator.Reset())
            |> ignore
        | _ ->
            Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
            |> ignore

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() after enumeration and modification should succeed`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            while enumerator.MoveNext() do
                ()

            match modifyEnumberable.Invoke enumerable with
            | true -> enumerator.Reset()
            | _ -> ()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() after enumeration and modification throws InvalidOperationException`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            while enumerator.MoveNext() do
                ()

            if modifyEnumberable.Invoke enumerable then
                match x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException`` with
                | true ->
                    Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
                    |> ignore
                | _ -> enumerator.Reset()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() modification before enumeration succeeds`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            match modifyEnumberable.Invoke enumerable with
            | true -> enumerator.Reset()
            | _ -> ()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() modification before enumeration throws InvalidOperationException`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            if modifyEnumberable.Invoke enumerable then
                match x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException`` with
                | true ->
                    Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
                    |> ignore
                | _ -> enumerator.Reset()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() modification during enumeration succeeds`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            for _ = 0 to count / 2 - 1 do
                enumerator.MoveNext() |> ignore

            match modifyEnumberable.Invoke enumerable with
            | true -> enumerator.Reset()
            | _ -> ()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> Reset() modification during enumeration throws InvalidOperationException`` count =
        for modifyEnumberable in x.GetModifyEnumerables(x.ModifyEnumeratorAllowed) do
            let enumerable = x.GenericIEnumerableFactory(count)
            use enumerator = enumerable.GetEnumerator()

            for _ = 0 to count / 2 - 1 do
                enumerator.MoveNext() |> ignore

            if modifyEnumberable.Invoke enumerable then
                match x.``IEnumerable<'T> modified during enumeration throws InvalidOperationException`` with
                | true ->
                    Should.Throw<NotSupportedException>(fun () -> enumerator.Reset())
                    |> ignore
                | _ -> enumerator.Reset()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable GetEnumerator() succeeds`` count =
        (x.GenericIEnumerableFactory(count))
            .GetEnumerator()
            .Dispose()

    [<Test; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerable GetEnumerator() returns unique enumerator`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        let mutable iterations = 0

        for _ in enumerable do
            for _ in enumerable do
                for _ in enumerable do
                    iterations <- iterations + 1

        iterations.ShouldBe(count * count * count)
