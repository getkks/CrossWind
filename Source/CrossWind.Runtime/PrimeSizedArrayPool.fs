namespace CrossWind.Runtime

open System
open System.Buffers
open System.Threading
open System.Diagnostics
open Helpers
open TypeHelpers
open CollectionHelpers

/// <summary>Provides a thread-safe bucket containing buffers that can be Rent'd and Return'd.</summary>
type Bucket<'T> (bufferLength : int, numberOfBuffers : int) =
    let lock = new SpinLock(Debugger.IsAttached) // only enable thread tracking if debugger is attached; it adds non-trivial overheads to Enter/Exit
    let buffers : 'T [] [] = zeroCreateUncheckedArray<'T []> numberOfBuffers
    let mutable index = 0
    member _.BufferLength : int = bufferLength
    /// <summary>Takes an array from the bucket.</summary>
    member _.Rent () =
        let mutable lockTaken = false
        let mutable buffer = null
        // While holding the lock, grab whatever is at the next available index and
        // update the index.  We do as little work as possible while holding the spin
        // lock to minimize contention with other threads.  The try/finally is
        // necessary to properly handle thread aborts on platforms which have them.
        try
            lock.Enter(&lockTaken)

            if index < buffers.Length then
                buffer <- fetchAndUpdate &buffers.[index] null
                index <- index + 1
        finally
            if lockTaken then lock.Exit()

        // While we were holding the lock, we grabbed whatever was at the next available index, if
        // there was one.  If we tried and if we got back null, that means we hadn't yet allocated
        // for that slot, in which case we should do so now.
        if buffer |> isNull then zeroCreateUncheckedArray bufferLength else buffer
    /// <summary>
    /// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
    /// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
    /// will be returned.
    /// </summary>
    member _.Return (arr : _ []) =
        // While holding the spin lock, if there's room available in the bucket,
        // put the buffer into the next available slot.  Otherwise, we just drop it.
        // The try/finally is necessary to properly handle thread aborts on platforms
        // which have them.
        let mutable lockTaken = false

        try
            if index <> 0 then
                lock.Enter(&lockTaken)
                index <- index - 1
                buffers.[index] <- arr
        finally
            if lockTaken then lock.Exit()

type PrimeSizedArrayPool<'T> (maxArrayLength : int, maxArraysPerBucket : int) =
    inherit ArrayPool<'T> ()

    [<Literal>]
    let MinimumArrayLength = 0x10

    [<Literal>]
    let MaximumArrayLength = 0x40000000

    [<Literal>]
    let MaxBucketsToTry = 2

    let maxBuckets =
        (match maxArrayLength with
         | x when x < 0 ->
             nameof (maxArrayLength)
             |> ArgumentOutOfRangeException
             |> raise
         // Our bucketing algorithm has a min length of 2^4 and a max length of 2^30.
         // Constrain the actual max used to those values.
         | x when x > MaximumArrayLength -> MaximumArrayLength
         | x when x < MinimumArrayLength -> MinimumArrayLength
         | _ -> maxArrayLength)
        |> SizeToIndex

    let buckets = Array.zeroCreate (maxBuckets + 1)

    do
        for i = 0 to buckets.Length - 1 do
            buckets.[i] <- Bucket(HashHelpers.PrimesDoublingInSize.[i], maxArraysPerBucket)

    new () = PrimeSizedArrayPool<_>(1024 * 1024, 50)

    override x.Rent (minimumLength : int) =
        match minimumLength with
        // No need for events with the empty array.  Our pool is effectively infinite
        // and we'll never allocate for rents and never store for returns.
        | 0 -> Array.Empty()
        // Arrays can't be smaller than zero.  We allow requesting zero-length arrays (even though
        // pooling such an array isn't valuable) as it's a valid length array, and we want the pool
        // to be usable in general instead of using `new`, even for computed lengths.
        | _ when minimumLength < 0 ->
            nameof (minimumLength)
            |> ArgumentOutOfRangeException
            |> raise
        | _ ->
            minimumLength
            |> SizeToIndex
            |> x.RentFromBucket

    member _.RentFromBucket bucketIndex =
        if bucketIndex < buckets.Length then
            // Search for an array starting at the 'index' bucket. If the bucket is empty, bump up to the
            // next higher bucket and try that one, but only try at most a few buckets.
            let mutable i = bucketIndex
            let mutable buffer = null

            while (buffer <- buckets.[i].Rent() // Attempt to rent from the bucket.  If we get a buffer from it, return it.
                   i <- i + 1

                   isNull buffer
                   && i < buckets.Length
                   && i < (bucketIndex + MaxBucketsToTry)) do
                ()

            buffer
        else
            // The request was for a size too large for the pool.  Allocate an array of exactly the requested length.
            // When it's returned to the pool, we'll simply throw it away.
            zeroCreateUncheckedArray buckets.[bucketIndex].BufferLength

    member x.ReturnAndRentFromBucket (arr, bucketIndex, clearArray) =
        x.Return(arr, clearArray)
        bucketIndex |> x.RentFromBucket

    override _.Return (arr, clearArray) =
        arr |> ArgumentNullException.ThrowIfNull

        match arr.Length with
        // Ignore empty arrays.  When a zero-length array is rented, we return a singleton
        // rather than actually taking a buffer out of the lowest bucket.
        | 0 -> ()
        | x ->
            // Determine with what bucket this array length is associated
            let bucket = x |> SizeToIndex
            // If we can tell that the buffer was allocated, drop it. Otherwise, check if we have space in the pool
            if bucket < buckets.Length then
                if clearArray then arr |> Array.Clear
                // Return the buffer to its bucket.  In the future, we might consider having Return return false
                // instead of dropping a bucket, in which case we could try to return to a lower-sized bucket,
                // just as how in Rent we allow renting from a higher-sized bucket.
                arr |> buckets.[bucket].Return

    static member val Shared = PrimeSizedArrayPool<'T>()
