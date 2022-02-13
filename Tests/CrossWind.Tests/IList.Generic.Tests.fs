namespace CrossWind.Collections.Test

open Expecto
open FsCheck
open System
open CrossWind.Tests
open Expecto.Expect
open System.Collections.Generic
open System.Runtime.CompilerServices

module ``IList<'T> Tests`` =

    let inline IListPropertyTest<'T, 'TestType> testName : 'TestType -> Test =
        genericTypePropertyTest<'T, 'TestType> "IList" testName

open ``IList<'T> Tests``

[<AbstractClass>]
type ``IList<'T> Tests``<'T when 'T : equality> () =
    inherit ``ICollection<'T> Tests``<'T> ()

    abstract createList : items : 'T [] -> 'T IList

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    override x.createCollection items = x.createList items

    member x.``IndexOf(item)`` () =
        IListPropertyTest<'T, _>(nameof (x.``IndexOf(item)``))
        <| fun (items : 'T []) ->
            let list = x.createList items
            all items (fun item -> list.[list.IndexOf(item)] = item) ""

    member x.``Insert(item,index)`` () =
        IListPropertyTest<'T, _>(nameof (x.``Insert(item,index)``))
        <| fun index (items : 'T []) item ->
            let list = x.createList items

            if uint (index) <= uint (list.Count) then
                "in range"
                @| Prop.ofTestable (lazy (list.Insert(index, item)))
            else
                "out of range"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (list.Insert(index, item)))

    member x.Item () =
        IListPropertyTest<'T, _>(nameof (x.Item))
        <| fun index (items : 'T []) ->
            let list = x.createList items

            if uint (index) < uint (list.Count) then
                "in range"
                @| Prop.ofTestable (lazy (list.[index] <- list.[index]))
            else
                "out of range"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (list.[index] |> ignore))

    member x.``RemoveAt(index)`` () =
        IListPropertyTest<'T, _>(nameof (x.``RemoveAt(index)``))
        <| fun index (items : 'T []) ->
            let list = x.createList items

            if uint (index) < uint (list.Count) then
                "in range"
                @| (lazy (list.RemoveAt index))
            else
                "out of range"
                @| Prop.throws<ArgumentOutOfRangeException, _> (lazy (list.RemoveAt index))

    member x.Tests () =
        testList
            "IList<'T>"
            [ x.``IndexOf(item)`` () ; x.``Insert(item,index)`` () ; x.Item() ; x.``RemoveAt(index)`` () ]
