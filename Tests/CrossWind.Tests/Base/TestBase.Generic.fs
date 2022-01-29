(*
Unlicense

This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or distribute this
software, either in source code form or as a compiled binary, for any purpose,
commercial or non-commercial, and by any means.

In jurisdictions that recognize copyright laws, the author or authors of this
software dedicate any and all copyright interest in the software to the public
domain. We make this dedication for the benefit of the public at large and to
the detriment of our heirs and
successors. We intend this dedication to be an overt act of relinquishment in
perpetuity of all present and future rights to this software under copyright
law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>
*)

namespace CrossWind.Collections.Tests

open Shouldly
open System
open System.Collections.Generic
open System.Linq
open CrossWind.Collections
open System.Collections.Generic
open System.Diagnostics

type IntArrayComparer () =
    interface IEqualityComparer<int []> with
        member _.Equals (x : _ [], y) =
            obj.ReferenceEquals(x, y)
            || (x |> isNull |> not
                && y |> isNull |> not
                && x.Length = y.Length
                && x.AsSpan().SequenceEqual(ReadOnlySpan(y)))

        member _.GetHashCode (o : _ []) =
            let mutable hash = 17

            for x in o do
                hash <- hash ^^^ x.GetHashCode()

            hash

[<AbstractClass>]
type TestBase<'T> () =
    inherit TestBase ()
    /// <summary>
    /// To be implemented in the concrete collections test classes. Creates an instance of T that
    /// is dependent only on the seed passed as input and will return the same value on repeated
    /// calls with the same seed.
    /// </summary>
    abstract CreateT : int -> 'T
    /// <summary>
    /// The EqualityComparer that can be used in the overriding class when creating test enumerables
    /// or test collections. Default if not overridden is the default comparator.
    /// </summary>
    member _.GetIEqualityComparer () = EqualityComparer<'T>.Default
    /// <summary>
    /// The Comparer that can be used in the overriding class when creating test enumerables
    /// or test collections. Default if not overridden is the default comparator.
    member _.GetIComparer () = Comparer<'T>.Default

    static member GetTestData () =
        seq {
            for collectionSizeArray in TestBase.ValidCollectionSizes() do
                let count = collectionSizeArray.[0] |> unbox

                for et in Enum.GetValues(typeof<EnumerableType>) do
                    let enumerableType = et :?> int
                    yield [| enumerableType ; count ; 0 ; 0 ; 0 |] // Empty Enumerable

                    yield [| enumerableType ; count ; count + 1 ; 0 ; 0 |] // Enumerable that is 1 larger

                    if count >= 1 then
                        yield [| enumerableType ; count ; count ; 0 ; 0 |] // Enumerable of the same size
                        yield [| enumerableType ; count ; count - 1 ; 0 ; 0 |] // Enumerable that is 1 smaller
                        yield [| enumerableType ; count ; count ; 1 ; 0 |] // Enumerable of the same size with 1 matching element
                        yield [| enumerableType ; count ; count + 1 ; 1 ; 0 |] // Enumerable that is 1 longer with 1 matching element
                        yield [| enumerableType ; count ; count ; count ; 0 |] // Enumerable with all elements matching
                        yield [| enumerableType ; count ; count + 1 ; count ; 0 |] // Enumerable with all elements matching plus one extra

                    if count >= 2 then
                        yield [| enumerableType ; count ; count - 1 ; 1 ; 0 |] // Enumerable that is 1 smaller with 1 matching element
                        yield [| enumerableType ; count ; count + 2 ; 2 ; 0 |] // Enumerable that is 2 longer with 2 matching element
                        yield [| enumerableType ; count ; count - 1 ; count - 1 ; 0 |] // Enumerable with all elements matching minus one
                        yield [| enumerableType ; count ; count ; 2 ; 0 |] // Enumerable of the same size with 2 matching element

                        if enumerableType = int EnumerableType.List
                           || enumerableType = int EnumerableType.Queue then
                            yield [| enumerableType ; count ; count ; 0 ; 1 |] // Enumerable with 1 element duplicated

                    if count >= 3 then
                        if enumerableType = int EnumerableType.List
                           || enumerableType = int EnumerableType.Queue then
                            yield [| enumerableType ; count ; count ; 0 ; 1 |] // Enumerable with all elements duplicated
                            yield [| enumerableType ; count ; count - 1 ; 2 ; 0 |] // Enumerable that is 1 smaller with 2 matching elements
        }

    static member TestData =
        Lazy<obj [] []>.Create
            (fun () ->
                (TestBase<'T>.GetTestData ())
                    .Distinct(IntArrayComparer())
                    .Select(fun ints ->
                        [| ints.[0] :> obj ; ints.[1] ; ints.[2] :> obj ; ints.[3] :> obj ; ints.[4] :> obj |]
                    )
                    .ToArray()
            )

    /// <summary>
    /// MemberData to be passed to tests that take an IEnumerable{T}. This method returns every permutation of
    /// EnumerableType to test on (e.g. HashSet, Queue), and size of set to test with (e.g. 0, 1, etc.).
    /// </summary>
    static member EnumerableTestData () = TestBase<'T>.TestData.Value

    /// <summary>
    /// Helper function to create an enumerable fulfilling the given specific parameters. The function will
    /// create an enumerable of the desired type using the Default constructor for that type and then add values
    /// to it until it is full. It will begin by adding the desired number of matching and duplicate elements,
    /// followed by random (deterministic) elements until the desired count is reached.
    /// </summary>
    member x.CreateEnumerable (eType, enumerableToMatchTo, count, numberOfMatchingElements, numberOfDuplicateElements) =
        match eType with
        | EnumerableType.List ->
            x.CreateList(enumerableToMatchTo, count, numberOfMatchingElements, numberOfDuplicateElements)
        | _ ->
            Debug.Assert(
                false,
                "Check that the 'EnumerableType' Enum returns only types that are special-cased in the CreateEnumerable function within the Iset_Generic_Tests class"
            )

            null
    /// <summary>
    /// Helper function to create an List fulfilling the given specific parameters. The function will
    /// create an List and then add values
    /// to it until it is full. It will begin by adding the desired number of matching,
    /// followed by random (deterministic) elements until the desired count is reached.
    /// </summary>
    member x.CreateList
        (
            enumerableToMatchTo : _ IEnumerable,
            count : int,
            numberOfMatchingElements,
            numberOfDuplicateElements
        ) =
        let lst = List<'T>(count)
        let mutable seed = 528
        let mutable duplicateAdded = 0

        let matchList =
            (match enumerableToMatchTo with
             | null -> null
             | _ ->
                 let mutable i = 0
                 let t = enumerableToMatchTo.ToList()

                 while i < numberOfMatchingElements do
                     lst.Add(t.[i])

                 t)

        while lst.Count < count do
            let mutable toAdd = seed |> x.CreateT
            seed <- seed + 1

            while (lst.Contains(toAdd)
                   || (matchList |> isNull |> not
                       && matchList.Contains(toAdd))) do // Don't want any unexpectedly duplicate values
                toAdd <- x.CreateT(seed)
                seed <- seed + 1

            lst.Add(toAdd)

            while (duplicateAdded < numberOfDuplicateElements) do
                lst.Add(toAdd)
                duplicateAdded <- duplicateAdded + 1

        // Validate that the Enumerable fits the guidelines as expected
        lst.Count.ShouldBe(count)

        if matchList |> isNull |> not then
            let mutable actualMatchingCount = 0

            for lookingFor in matchList do
                actualMatchingCount <-
                    actualMatchingCount
                    + (if lst.Contains(lookingFor) then
                           1
                       else
                           0)

            numberOfMatchingElements.ShouldBe(actualMatchingCount)

        lst
