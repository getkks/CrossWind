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
type ``ICollection Generic Tests``<'T> () =
    inherit ``IEnumerable<'T> Tests``<'T> ()
    abstract ``Add Remove and Clear throws NotSupported`` : bool
    default _.``Add Remove and Clear throws NotSupported`` = false

    abstract DefaultValueAllowed : bool
    default _.DefaultValueAllowed = true

    abstract ``Default Value when not allowed throws`` : bool
    default _.``Default Value when not allowed throws`` = true

    abstract DuplicateValuesAllowed : bool
    default _.DuplicateValuesAllowed = true

    abstract ``ICollection generic CopyTo index larger than array count throw type`` : Type
    default _.``ICollection generic CopyTo index larger than array count throw type`` = typeof<ArgumentException>

    abstract InvalidValues : 'T seq
    default _.InvalidValues : _ seq = Array.Empty<'T>()

    abstract IsReadOnly : bool
    default _.IsReadOnly = false

    abstract AddToCollection : collection : 'T ICollection * numberOfItemsToAdd : int -> unit

    default x.AddToCollection (collection, numberOfItemsToAdd) =
        let mutable seed = 9600
        let comparer = x.GetIEqualityComparer()

        while collection.Count < numberOfItemsToAdd do
            let toAdd = x.CreateT(seed)
            seed <- seed + 1

            while collection.Contains(toAdd, comparer)
                  || x.InvalidValues.Contains(toAdd, comparer) do
                let toAdd = x.CreateT(seed)
                seed <- seed + 1

            collection.Add toAdd

    /// <summary>
    /// Creates an instance of an ICollection{T} that can be used for testing.
    /// </summary>
    /// <returns>An instance of an ICollection{T} that can be used for testing.</returns>
    abstract GenericICollectionFactory : unit -> 'T ICollection

    /// <summary>
    /// Creates an instance of an ICollection{T} that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned ICollection{T} contains.</param>
    /// <returns>An instance of an ICollection{T} that can be used for testing.</returns>
    abstract GenericICollectionFactory : count : int -> 'T ICollection

    default x.GenericICollectionFactory count =
        let collection = x.GenericICollectionFactory()
        x.AddToCollection(collection, count)
        collection

    override x.GenericIEnumerableFactory count = x.GenericICollectionFactory count

    override x.GetModifyEnumerables operations =
        seq {
            if x.``Add Remove and Clear throws NotSupported`` then
                match operations with
                | Add ->
                    ModifyEnumerable<'T>(fun enumerable ->
                        let casted = enumerable.As<_, 'T ICollection>()
                        2344 |> x.CreateT |> casted.Add
                        true
                    )
                | Remove ->
                    ModifyEnumerable<'T>(fun enumerable ->
                        let casted = enumerable.As<_, 'T ICollection>()

                        if casted.Count > 0 then
                            0
                            |> casted.ElementAt
                            |> casted.Remove
                            |> ignore

                            true
                        else
                            false
                    )
                | Clear ->
                    ModifyEnumerable<'T>(fun enumerable ->
                        let casted = enumerable.As<_, 'T ICollection>()

                        if casted.Count > 0 then
                            casted.Clear()
                            true
                        else
                            false
                    )
                | _ -> ()
        }

    [<Test ; MemberData(nameof (TestBase.ValidCollectionSizes))>]
    member x.``ICollection<'T> calling Add method after Clear method`` count =
        if
            not
                (
                    x.IsReadOnly
                    || x.``Add Remove and Clear throws NotSupported``
                )
        then
            let collection = count |> x.GenericICollectionFactory
            collection.Clear()
            x.AddToCollection(collection, 5)
            collection.Count.ShouldBe(5)
