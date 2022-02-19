namespace CrossWind.Collections.Test

open Expecto
open CrossWind.Collections
open CrossWind.Tests
open System.Runtime.CompilerServices
open FsCheck
open System
open Expecto.Expect
open System.Collections.Generic

module ``PooledDictionary<'TKey, 'TValue,keyComparer> Tests`` =

    let inline PooledDictionaryPropertyTest<'T, 'TestType> testName : 'TestType -> Test =
        genericTypePropertyTest<'T, 'TestType> "PooledDictionary" testName

open ``PooledDictionary<'TKey, 'TValue,keyComparer> Tests``

type ``PooledDictionary<'TKey, 'TValue,keyComparer> Tests``<'TKey, 'TValue when 'TKey : equality> () =
    inherit ``IDictionary<'T> Tests``<'TKey, 'TValue> ()

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    override _.createDictionary items = new PooledDictionary<'TKey, 'TValue, EqualityComparer<'TKey>>(items) :> _

    member x.``PooledDictionary<'TKey, 'TValue,keyComparer>(capacity)`` () =
        PooledDictionaryPropertyTest<_, _>(nameof (x.``PooledDictionary<'TKey, 'TValue,keyComparer>(capacity)``))
        <| fun capacity ->
            if capacity < 0 then
                "out of range"
                @| Prop.throws<ArgumentOutOfRangeException, _> (
                    lazy (new PooledDictionary<'TKey, 'TValue, EqualityComparer<'TKey>>(capacity))
                )
            else
                "in range"
                @| Prop.ofTestable (lazy (new PooledDictionary<'TKey, 'TValue, EqualityComparer<'TKey>>(capacity)))

    member x.Tests () =
        testList
            "PooledList<'T> Type"
            [ (x :> ``IDictionary<'T> Tests``<'TKey, 'TValue>).Tests()
              (x :> ``ICollection<'T> Tests``<KeyValuePair<'TKey, 'TValue>>).Tests()
              (x :> ``IEnumerable<'T> Tests``<KeyValuePair<'TKey, 'TValue>>).Tests() ]
