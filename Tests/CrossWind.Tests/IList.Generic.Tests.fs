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

[<AbstractClass>]
type ``IList Generic Tests``<'T> () =
    inherit ``ICollection Generic Tests``<'T> ()

    override _.``Default Value when not allowed throws`` = false

    abstract GenericIListFactory : unit -> 'T IList
    abstract GenericIListFactory : count : int -> 'T IList

    default x.GenericIListFactory count =
        let collection = x.GenericIListFactory()
        x.AddToCollection(collection, count)
        collection

    override x.GenericICollectionFactory count = x.GenericIListFactory count
    override x.GenericICollectionFactory () = x.GenericIListFactory()
