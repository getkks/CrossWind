namespace CrossWind

open System
open System.Collections.Generic
open CrossWind.Runtime

module Record =
    type Row<'FieldNameComparer when 'FieldNameComparer :> IEqualityComparer<string>> =
        { Table : 'FieldNameComparer Table
          RowNumber : int }

    let inline getField fieldName (row : _ Row) : 'T =
        Column.getItem row.RowNumber row.Table.[fieldName]

    let inline setField fieldName value (row : _ Row) =
        Column.setItem row.RowNumber value row.Table.[fieldName]

    let inline nextRow (row : _ Row) (nextRow : _ outref) =
        nextRow <- { Table = row.Table ; RowNumber = row.RowNumber + 1 }
        true

    type IReadOnlyRecord =
        abstract NextRecord : IReadOnlyRecord

    type IRecord =
        inherit IReadOnlyRecord
        abstract NextRecord : IRecord

    type Record<'FieldNameComparer when 'FieldNameComparer :> IEqualityComparer<string>>
        (
            table : 'FieldNameComparer Table,
            rowNumber : int
        ) =
        struct
            interface IReadOnlyRecord with
                member x.NextRecord : IReadOnlyRecord = raise (System.NotImplementedException())

            interface IRecord with
                member x.NextRecord : IRecord = raise (System.NotImplementedException())
        end
