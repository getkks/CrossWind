namespace CrossWind.Collections.Test

open Expecto
open FsCheck
open System
open CrossWind.Tests
open Expecto.Expect
open System.Collections.Generic
open System.Runtime.CompilerServices

module ``IDictionary<'T> Tests`` =

    let inline IDictionaryPropertyTest<'T, 'TestType> testName : 'TestType -> Test =
        genericTypePropertyTest<'T, 'TestType> "IEnumerable" testName

open ``IDictionary<'T> Tests``

[<AbstractClass>]
type ``IDictionary<'T> Tests``<'TKey, 'TValue when 'TKey : equality> () =
    inherit ``ICollection<'T> Tests``<KeyValuePair<'TKey, 'TValue>> ()

    abstract createDictionary : items : KeyValuePair<'TKey, 'TValue> [] -> IDictionary<'TKey, 'TValue>

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    override x.createCollection items = x.createDictionary items

    member x.``IndexOf(item)`` () =
        IDictionaryPropertyTest<_, _>(nameof (x.``IndexOf(item)``))
        <| fun key (items : _ []) ->
            let dictionary = Array.empty |> x.createDictionary
            Prop.throws<KeyNotFoundException, _> (lazy (dictionary.[key] |> ignore))
