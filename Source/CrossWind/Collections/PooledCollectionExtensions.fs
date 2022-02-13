namespace CrossWind.Collections

open System.Runtime.CompilerServices
open System.Collections.Generic
open System

[<Extension>]
type CollectionExtensions =

    [<Extension>]
    static member inline ToPooledList (collection : _ ICollection) = new PooledList<_>(collection)

    [<Extension>]
    static member inline ToPooledList (enumerable : _ seq) = new PooledList<_>(enumerable)

    [<Extension>]
    static member inline ToPooledList (arr : _ []) = new PooledList<_>(arr)

    [<Extension>]
    static member inline ToPooledList (span : _ Span) = new PooledList<_>(span)
