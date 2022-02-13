namespace CrossWind.Collections.Test

open Expecto
open CrossWind.Collections
open CrossWind.Tests
open System.Runtime.CompilerServices
open FsCheck
open System
open Expecto.Expect
open System.Collections.Generic

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
        <| fun capacity ->
            if capacity < 0 then
                "out of range"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (new PooledList<'T>(capacity)))
            else
                "in range"
                @| Prop.ofTestable (lazy (new PooledList<'T>(capacity)))

    member x.Tests () =
        testList
            "PooledList<'T> Type"
            [ (x :> ``IList<'T> Tests``<'T>).Tests()
              (x :> ``ICollection<'T> Tests``<'T>).Tests()
              (x :> ``IEnumerable<'T> Tests``<'T>).Tests() ]
