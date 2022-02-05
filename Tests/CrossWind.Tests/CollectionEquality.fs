namespace CrossWind.Collections.Tests

open System.Collections
open System.Collections.Generic
open System.Linq
open Swensen.Unquote
open CrossWind.Runtime

type CollectionEquality =
    static member Equal(expected: 'T IEnumerable, actual: 'T IEnumerable) =
        if expected.IsNull() then
            test <@ actual = expected @>
        else
            use expectedEnumerator = expected.GetEnumerator()
            use actualEnumerator = actual.GetEnumerator()

            while expectedEnumerator.MoveNext()
                  && actualEnumerator.MoveNext() do
                test <@ actualEnumerator.Current = expectedEnumerator.Current @>

    static member inline Equal(expected: IEnumerable, actual: IEnumerable) =
        CollectionEquality.Equal(expected.Cast<obj>(), actual.Cast<obj>())

    static member ToDictionary collection =
        let dictionary = Dictionary()

        for item in collection do
            match dictionary.TryGetValue(item) with
            | true, x -> dictionary.[item] <- x + 1
            | _ -> dictionary.[item] <- 1

        dictionary

    static member EqualUnOrdered(expected: 'T IEnumerable, actual: 'T IEnumerable) =
        if expected.IsNull() then
            test <@ actual = expected @>
        else
            let expected =
                expected |> CollectionEquality.ToDictionary

            let actual =
                actual |> CollectionEquality.ToDictionary

            for kv in expected do
                let key = kv.Key
                let value = kv.Value
                test <@ actual.[key] = value @>

    static member inline EqualUnOrdered(expected: IEnumerable, actual: IEnumerable) =
        CollectionEquality.EqualUnOrdered(expected.Cast<obj>(), actual.Cast<obj>())
