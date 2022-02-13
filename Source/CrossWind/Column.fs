namespace CrossWind

open CrossWind.Collections
open CrossWind.Runtime
open System
open System.Collections
open System.Collections.Generic
open System.Runtime.CompilerServices
open Microsoft.FSharp.Quotations

module Column =
    type IColumn =
        abstract AddDefault : unit -> unit
        abstract Count : int
        abstract Duplicate : unit -> IColumn
        abstract FirstN : n : int -> unit
        abstract IndexedCopy : indexedCounts : struct (int * int) [] -> unit
        abstract IndexedSelection : indices : int [] -> unit
        abstract NewEmptyColumn : unit -> IColumn
        abstract NewIndexedCopy : indexedCounts : struct (int * int) [] -> IColumn
        abstract SetItemDefault : index : int -> unit

    type Column<'T> (column : Column<'T>) =
        inherit PooledList<'T> (column)

        new () = new Column<'T>(Unchecked.defaultof<_>)

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member x.AddDefault () = Unchecked.defaultof<_> |> x.Add

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member x.Duplicate () = new Column<'T>(x)

        member x.FirstN (n) =
            let c = x.Count

            if n > 0 && c > n then
                x.RemoveRange(n, c - n)
                x.TrimExcess()

        member _.NewEmptyColumn () = new Column<'T>()

        member x.SetItemDefault index = x.[index] <- Unchecked.defaultof<_>

        interface IColumn with

            [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
            override x.AddDefault () = x.AddDefault()

            override x.Count = x.Count

            [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
            override x.Duplicate () = x.Duplicate().As()

            [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
            override x.FirstN (n) = n |> x.FirstN

            override x.IndexedCopy indexedCounts = raise (System.NotImplementedException())

            override x.IndexedSelection indices = raise (System.NotImplementedException())

            override x.NewEmptyColumn () = x.NewEmptyColumn()

            override x.NewIndexedCopy indexedCounts = raise (System.NotImplementedException())

            [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
            override x.SetItemDefault index = index |> x.SetItemDefault

    [<Extension>]
    type ColumnExtensions =
        static member inline IsColumnOfType<'T> (object : IColumn) = object.IsOfType<'T Column>()

        [<Extension>]
        static member inline AsColumn (x : #IColumn) : 'U Column = x.As()

    let inline add value (column : #IColumn) = column.AsColumn().Add value

    let inline count (column : #IColumn) = column.Count

    let inline getItem index (column : #IColumn) = column.AsColumn().[index]

    let inline setItem index value (column : #IColumn) = column.AsColumn().[index] <- value

type Table<'ColumnNameComparer when 'ColumnNameComparer :> IEqualityComparer<string>> =
    PooledDictionary<string, Column.IColumn, 'ColumnNameComparer>
