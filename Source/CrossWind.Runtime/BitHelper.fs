namespace CrossWind.Runtime

open System
open System.Runtime.CompilerServices

module BitHelper =
    [<Literal>]
    let IntSize = 32

open BitHelper

[<Struct ; IsByRefLike>]
type BitHelper (span : int Span) =
    new (span : _ Span, clear) =
        if clear then span.Clear()
        BitHelper(span)
    /// <summary>How many ints must be allocated to represent n bits. Returns (n+31)/32, but avoids overflow.</summary>
    static member ToIntArrayLength n =
        if n > 0 then
            (n - 1) / IntSize + 1
        else
            0

    member _.IsMarked bitPosition =
        let bitArrayIndex = bitPosition / IntSize

        uint (bitArrayIndex) < uint (span.Length)
        && span.[bitArrayIndex]
           &&& (1 <<< (bitPosition % IntSize))
           <> 0

    member _.MarkBit bitPosition =
        let bitArrayIndex = bitPosition / IntSize

        if uint (bitArrayIndex) < uint (span.Length) then
            let s = &span.[bitArrayIndex]
            s <- s ||| (1 <<< (bitPosition % IntSize))
