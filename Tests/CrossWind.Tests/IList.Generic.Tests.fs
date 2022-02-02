namespace CrossWind.Collections.Tests

open Shouldly
open System.Collections.Generic

[<AbstractClass>]
type ``IList Generic Tests``<'T when 'T : equality> () =
    inherit ``ICollection<'T> Tests``<'T> ()

    override _.``Default Value when not allowed throws`` = false

    abstract GenericIListFactory : unit -> 'T IList
    abstract GenericIListFactory : count : int -> 'T IList

    default x.GenericIListFactory count =
        let collection = x.GenericIListFactory()
        x.AddToCollection(collection, count)
        collection

    override x.GenericICollectionFactory count = x.GenericIListFactory count
    override x.GenericICollectionFactory () = x.GenericIListFactory()
