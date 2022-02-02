namespace CrossWind.Collections.Tests

open Shouldly
open System
open System.Linq
open CrossWind.Collections

type TestBase () =
    let disposables = new PooledList.PooledList<IDisposable>()

    static member ValidCollectionSizes () =
        seq {
            yield [| 0 |> box |]
            yield [| 1 |]
            yield [| 5 |]
            yield [| 75 |]
        }

    member _.RegisterForDispose o =
        match o |> box with
        | :? IDisposable as d -> disposables.Add d
        | _ -> ()

        o

    member _.RegisterForDispose ([<ParamArray>] o : _ []) =
        if o.Length <> 0 then
            o.OfType<IDisposable>()
            |> disposables.AddRange

            o.OfType<IDisposable>()
            |> disposables.AddRange

    interface IDisposable with
        member _.Dispose () =
            for x in disposables.AsSpan() do
                x.Dispose()

            disposables.Dispose()

type EnumerableType =
    | HashSet = 0
    | SortedSet = 1
    | List = 2
    | Queue = 3
    | Lazy = 4

[<Flags>]
type ModifyOperation =
    | None
    | Add
    | Insert
    | Remove
    | Clear
