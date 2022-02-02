namespace CrossWind.Collections.Tests

open System
open CrossWind.Tests

[<TestClass>]
type ``PooledList<'T> Tests for Integer Type`` () =
    inherit ``List Generic Tests``<int> ()

    override _.CreateT seed =
        let rand = new Random(seed)
        rand.Next()

[<TestClass>]
type ``PooledList<'T> Tests for String Type`` () =
    inherit ``List Generic Tests``<string> ()

    override _.CreateT seed =
        let stringLength = seed % 10 + 5
        let rand = new Random(seed)
        let bytes = Array.zeroCreate stringLength
        rand.NextBytes(bytes)
        Convert.ToBase64String(bytes)
