namespace CrossWind.Collections.Test

open Expecto
open CrossWind.Collections
open CrossWind.Tests
open System.Runtime.CompilerServices
open FsCheck
open System
open Expecto.Expect

module ``PooledList<'T> Tests`` =

    let inline PooledListPropertyTest<'T, 'TestType> testName : 'TestType -> Test =
        genericTypePropertyTest<'T, 'TestType> "PooledList" testName

open ``PooledList<'T> Tests``

type ``PooledList<'T> Tests``<'T when 'T : equality> () =
    inherit ``IList<'T> Tests``<'T> ()

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    override _.createList items = new PooledList<'T>(items)

    member x.``PooledList<'T>(capacity)`` () =
        PooledListPropertyTest<'T, _>(nameof (x.``PooledList<'T>(capacity)``))
        <| fun (capacity : int) ->
            if capacity < 0 then
                "out of range"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (new PooledList<'T>(capacity)))
            else
                "in range"
                @| Prop.ofTestable (lazy ((new PooledList<'T>(capacity)) |> ignore))

    member x.``PooledList<'T>.Reverse()`` () =
        PooledListPropertyTest<'T, _>(nameof (x.``PooledList<'T>.Reverse()``))
        <| fun (items : _ []) ->
            let lst = new PooledList<'T>(items)
            lst.Reverse()
            Array.Reverse items
            sequenceEqual lst items

    member x.``PooledList<'T>.Reverse(index,count)`` () =
        PooledListPropertyTest<'T, _>(nameof (x.``PooledList<'T>.Reverse(index,count)``))
        <| fun (items : _ []) index count ->
            let lst = new PooledList<'T>(items)

            match index, count with
            | _ when index < 0 ->
                "index negative"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (lst.Reverse(index, count)))
            | _ when count < 0 ->
                "count negative"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (lst.Reverse(index, count)))
            | _ when lst.Count - index < count ->
                "count negative"
                @| Prop.throws<ArgumentException, _> (lazy (lst.Reverse(index, count)))
            | _ ->
                lst.Reverse(index, count)
                Array.Reverse(items, index, count)

                "in range"
                @| Prop.ofTestable (sequenceEqual lst items)

    member x.Tests () =
        testList
            "PooledList<'T> Type"
            [ (x :> ``IList<'T> Tests``<'T>).Tests()
              (x :> ``ICollection<'T> Tests``<'T>).Tests()
              (x :> ``IEnumerable<'T> Tests``<'T>).Tests()
              x.``PooledList<'T>(capacity)`` ()
              x.``PooledList<'T>.Reverse()``()
              x.``PooledList<'T>.Reverse(index,count)``() ]
