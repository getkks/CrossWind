namespace CrossWind.Collections.Tests

open System
open System.Collections.Generic
open System.Reflection
open Fixie
open Shouldly
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.CompilerServices

[<Extension>]
type Extensions =
    [<Extension>]
    static member Slice (array : _ [], startIndex : int, [<Optional ; DefaultParameterValue(-1)>] length : int) =
        array.AsSpan(startIndex, (if (length = -1) then array.Length - startIndex else length)).ToArray()

    [<Extension>]
    static member Push (array : 'T [], [<ParamArray>] arguments : 'T []) =
        match arguments with
        | null when RuntimeHelpers.IsReferenceOrContainsReferences<'T>() -> array.Push(Unchecked.defaultof<'T>)
        | null ->
            nameof (arguments)
            |> ArgumentNullException
            |> raise
        | _ ->
            let ret = Array.zeroCreate (array.Length + arguments.Length)
            Array.Copy(array, ret, array.Length)
            Array.Copy(arguments, 0, ret, array.Length, arguments.Length)
            ret

    [<Extension>]
    static member RemoveAt (array : 'T [], removeIndex) =
        let ret = Array.zeroCreate (array.Length - 1)
        Array.Copy(array, 0, ret, 0, removeIndex)
        Array.Copy(array, removeIndex + 1, ret, removeIndex, array.Length - 1 - removeIndex)
        ret

    [<Extension>]
    static member Shuffle (rng : Random, list : 'T IList) =
        let mutable n = list.Count

        while (n > 1) do
            n <- n - 1
            let k = rng.Next(n + 1)
            let value = list.[k]
            list.[k] <- list.[n]
            list.[n] <- value

    [<Extension>]
    static member CreateNewObject<'T> (collectionType : Type, elementType : Type) =
        collectionType.GetGenericTypeDefinition().MakeGenericType([| elementType |]).GetConstructor([||]).Invoke([||])
        :?> 'T
