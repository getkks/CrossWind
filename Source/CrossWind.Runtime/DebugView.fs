namespace CrossWind.Runtime

open System
open System.Collections.Generic
open System.Linq
open System.Diagnostics

module DebugView =

    type ICollectionDebugView<'T> (collection : 'T ICollection) =
        do ArgumentNullException.ThrowIfNull(collection)
        [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
        member _.Items = collection.ToArray()
