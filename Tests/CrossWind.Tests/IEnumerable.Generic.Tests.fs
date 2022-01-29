namespace CrossWind.Collections.Tests

open Shouldly
open System
open System.Collections.Generic
open System.Linq
open CrossWind.Collections
open System.Collections.Generic
open System.Diagnostics
open System.Runtime.InteropServices
open System.Collections
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
type ModifyEnumerable<'T> = delegate of enumerable : 'T seq -> bool

/// <summary>
/// Contains tests that ensure the correctness of any class that implements the generic
/// IEnumerable interface.
/// </summary>
[<AbstractClass>]
type ``IEnumerable<'T> Tests``<'T> () =
    inherit TestBase<'T> ()
    /// <summary>
    /// Creates an instance of an IEnumerable{T} that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned IEnumerable{T} contains.</param>
    /// <returns>An instance of an IEnumerable{T} that can be used for testing.</returns>
    abstract GenericIEnumerableFactory : count : int -> 'T seq
    /// <summary>
    /// To be implemented in the concrete collections test classes. Returns a set of ModifyEnumerable delegates
    /// that modify the enumerable passed to them.
    /// </summary>
    abstract GetModifyEnumerables : operations : ModifyOperation -> 'T ModifyEnumerable seq

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
    member _.``Enumerator Current throws Undefined Operation`` = false
    /// <summary>
    /// When calling MoveNext or Reset after modification of the enumeration, the resulting behavior is
    /// undefined. Tests are included to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Execute MoveNext or Reset.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// true.
    /// </summary>
    member _.``Enumerator modified during enumeration throws InvalidOperationException`` = true
    /// <summary>
    /// Specifies whether this IEnumerable follows some sort of ordering pattern.
    /// </summary>
    member _.Order = Sequential

    member private x.RepeatTest (testCode, [<Optional ; DefaultParameterValue(3)>] iters) =
        let enumerable = x.GenericIEnumerableFactory(32)
        let items = enumerable.ToArray()
        let mutable enumerator = enumerable.GetEnumerator()

        for i = 0 to iters - 1 do
            testCode (enumerator, items, i)

            if not x.ResetImplemented then
                enumerator <- enumerable.GetEnumerator()
            else
                enumerator.Reset()

    member private x.RepeatTest (testCode, [<Optional ; DefaultParameterValue(3)>] iters : int) =
        x.RepeatTest((fun (e, i, it) -> testCode (e, i)), iters)

    member private x.VerifyEnumerator
        (
            enumerator : 'T IEnumerator,
            expectedItems : _ [],
            startIndex,
            count,
            validateStart,
            validateEnd
        ) =
        let needToMatchAllExpectedItems = count - startIndex = expectedItems.Length

        if validateStart then
            for _ = 0 to 2 do
                if x.``Enumerator Current throws Undefined Operation`` then
                    (fun () -> enumerator.Current :> obj)
                    |> ShouldThrowExtensions.ShouldThrow<InvalidOperationException>
                    |> ignore
                else
                    enumerator.Current |> ignore

        let mutable iterations = 0

        match x.Order with
        | Unspecified ->
            let itemsVisited = BitArray((if needToMatchAllExpectedItems then count else expectedItems.Length), false)

            while iterations < count do
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
                                               + if needToMatchAllExpectedItems then startIndex else 0]
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
                    if itemsVisited.[i] then visitedItemCount <- visitedItemCount + 1

                count.ShouldBe(visitedItemCount)
        | Sequential ->
            while iterations < count do
                let currentItem = enumerator.Current
                expectedItems.[iterations].ShouldBe(currentItem)

                for _ = 0 to 2 do
                    currentItem.ShouldBe(enumerator.Current)

                iterations <- iterations + 1

        count.ShouldBe(iterations)

        if validateEnd then
            for _ = 0 to 2 do
                enumerator.MoveNext().ShouldBeFalse("enumerator.MoveNext() returned true past the expected end.")

                if x.``Enumerator Current throws Undefined Operation`` then
                    (fun () -> enumerator.Current :> obj)
                    |> ShouldThrowExtensions.ShouldThrow<InvalidOperationException>
                    |> ignore
                else
                    enumerator.Current |> ignore

    member x.VerifyModifiedEnumerator (enumerator : 'T IEnumerator, expectedCurrent, expectCurrentThrow, atEnd) =
        if expectCurrentThrow then
            (fun () -> enumerator.Current :> obj)
            |> ShouldThrowExtensions.ShouldThrow<InvalidOperationException>
            |> ignore
        else
            let mutable current = enumerator.Current

            for _ = 0 to 2 do
                current.ShouldBe(expectedCurrent)
                current <- enumerator.Current

        (fun () -> enumerator.MoveNext() |> ignore)
        |> ShouldThrowExtensions.ShouldThrow<InvalidOperationException>
        |> ignore

        if not x.ResetImplemented then
            (fun () -> enumerator.Reset())
            |> ShouldThrowExtensions.ShouldThrow<InvalidOperationException>
            |> ignore

    [<Test ; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``IEnumerator<'T> calling MoveNext after Dispose`` count =
        let enumerable = x.GenericIEnumerableFactory(count)
        let enumerator = enumerable.GetEnumerator()

        for i = 0 to count - 1 do
            enumerator.MoveNext() |> ignore

        enumerator.Dispose()
        enumerator.MoveNext().ShouldBeFalse()
