namespace CrossWind.Collections.Tests

open Shouldly
open System
open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.Runtime.InteropServices
open CrossWind.Collections
open CrossWind.Runtime
open CrossWind.Tests

[<TestClass>]
type ``List Generic Tests for Integer Type`` () =
    inherit ``List Generic Tests``<int> ()

    override _.CreateT seed =
        let rand = new Random(seed)
        rand.Next()
