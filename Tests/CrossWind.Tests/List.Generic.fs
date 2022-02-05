namespace CrossWind.Collections.Tests

open System
open CrossWind.Tests

module ``PooledList<'T> Test Data Generator`` =
    let stringData seed =
        let stringLength = seed % 10 + 5
        let rand = new Random(seed)
        let bytes = Array.zeroCreate stringLength
        rand.NextBytes(bytes)
        Convert.ToBase64String(bytes)

    let intData seed =
        let rand = new Random(seed)
        rand.Next()

open ``PooledList<'T> Test Data Generator``

[<TestClass>]
type ``PooledList<'T> Tests for Integer type``() =
    inherit ``List Generic Tests``<int>()

    override _.CreateT seed = seed |> intData

[<TestClass>]
type ``PooledList<'T> Tests for String type``() =
    inherit ``List Generic Tests``<string>()

    override _.CreateT seed = seed |> stringData

[<TestClass>]
type ``PooledList<'T> Tests as ReadOnly for Integer type``() =
    inherit ``List Generic Tests``<int>()

    override _.IsReadOnly = true

    override _.CreateT seed = seed |> intData

    override x.GenericIListFactory() = x.GenericListFactory().AsReadOnly()

    override x.GenericIListFactory count =
        x.GenericListFactory(count).AsReadOnly()

[<TestClass>]
type ``PooledList<'T> Tests as ReadOnly for String type``() =
    inherit ``List Generic Tests``<string>()

    override _.IsReadOnly = true

    override _.CreateT seed = seed |> stringData

    override x.GenericIListFactory() = x.GenericListFactory().AsReadOnly()

    override x.GenericIListFactory count =
        x.GenericListFactory(count).AsReadOnly()
