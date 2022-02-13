namespace CrossWind.Collections.Test

open Expecto
open CrossWind.Tests
open System
open Expecto.Expect
open System.Collections.Generic
open System.Runtime.CompilerServices

module ``ICollection<'T> Tests`` =

    let inline ICollectionPropertyTest<'T, 'TestType> testName : 'TestType -> Test =
        genericTypePropertyTest<'T, 'TestType> "ICollection" testName

open ``ICollection<'T> Tests``

[<AbstractClass>]
type ``ICollection<'T> Tests``<'T when 'T : equality> () =
    inherit ``IEnumerable<'T> Tests``<'T> ()

    abstract createCollection : items : 'T [] -> 'T ICollection

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    override x.createIEnumerable items = x.createCollection items

    member x.``Add(item)`` () =
        ICollectionPropertyTest<'T, _>(nameof (x.``Add(item)``))
        <| fun item (items : 'T []) ->
            let list = x.createCollection items
            item |> list.Add
            equal list.Count (items.Length + 1) $"Should be {items.Length + 1}"

    member x.``Clear()`` () =
        ICollectionPropertyTest<'T, _>(nameof (x.``Clear()``))
        <| fun (items : 'T []) ->
            let list = x.createCollection items
            list.Clear()
            list.Clear()
            equal list.Count 0 "Should be 0"

    member x.``Contains(item)`` () =
        ICollectionPropertyTest<'T, _>(nameof (x.``Contains(item)``))
        <| fun (items : 'T []) ->
            let list = x.createCollection items

            for item in items do
                isTrue (list.Contains item) "Should be true"

    member x.``CopyTo(arr,count)`` () =
        ICollectionPropertyTest<'T, _>(nameof (x.``CopyTo(arr,count)``))
        <| fun (items : 'T []) ->
            let list = x.createCollection items
            let arr = Array.zeroCreate items.Length
            throwsT<ArgumentNullException> (fun () -> list.CopyTo(null, 0)) "Should throw"
            throwsT<ArgumentOutOfRangeException> (fun () -> list.CopyTo(arr, 1)) "Should throw"
            list.CopyTo(arr, 0)

            for i = 0 to items.Length - 1 do
                equal arr[i] items[i] "Should be equal"

    member x.Count () =
        ICollectionPropertyTest<'T, _>(nameof (x.Count))
        <| fun (items : 'T []) ->
            let list = x.createCollection items
            equal list.Count items.Length "Should be equal"

    member x.IsReadOnly () =
        ICollectionPropertyTest<'T, _>(nameof (x.IsReadOnly))
        <| fun (items : 'T []) ->
            let collection = x.createCollection items
            isFalse collection.IsReadOnly "Should be false"

    member x.``Remove(item)`` () =
        ICollectionPropertyTest<'T, _>(nameof (x.``Remove(item)``))
        <| fun (items : 'T []) ->
            let list = x.createCollection items

            for item in items do
                isTrue (list.Remove item) "Should be true"

    member x.Tests () =
        testList
            "ICollection<'T>"
            [ x.``Add(item)`` ()
              x.``Clear()`` ()
              x.``Contains(item)`` ()
              x.``CopyTo(arr,count)`` ()
              x.Count()
              x.IsReadOnly()
              x.``Remove(item)`` () ]
