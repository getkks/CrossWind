namespace CrossWind.Collections.Test

open Expecto
open Expecto.Expect
open CrossWind.Tests

module ``IEnumerable<'T> Tests`` =

    let inline IEnumerablePropertyTest<'T, 'TestType> testName : 'TestType -> Test =
        genericTypePropertyTest<'T, 'TestType> "IEnumerable" testName

open ``IEnumerable<'T> Tests``

[<AbstractClass>]
type ``IEnumerable<'T> Tests``<'T when 'T : equality> () =

    abstract createIEnumerable : items : 'T [] -> 'T seq

    member x.``IEnumerable<'T>.GetEnumerator()`` () =
        IEnumerablePropertyTest<'T, _>(nameof (x.``IEnumerable<'T>.GetEnumerator()``))
        <| fun (items : 'T []) ->
            //let list = (new PooledList.PooledList<'T>(items))
            let list = x.createIEnumerable items
            use mutable enumerator = list.GetEnumerator()

            while enumerator.MoveNext() do
                isTrue (Array.contains enumerator.Current items) "Should contain"

            enumerator.Reset()

            while enumerator.MoveNext() do
                isTrue (Array.contains enumerator.Current items) "Should contain"

    member x.Tests () = testList "IEnumerable<'T>" [ x.``IEnumerable<'T>.GetEnumerator()``() ]
