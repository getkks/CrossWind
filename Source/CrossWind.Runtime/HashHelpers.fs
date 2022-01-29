namespace CrossWind.Runtime

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open LocalsInit
open TypeHelpers
open ExpressionHelpers
open CollectionHelpers

#nowarn "9"

module HashHelpers =

    [<IsReadOnly>]
    let PrimesDoublingInSize =
        [|
            11
            29
            59
            107
            239
            431
            919
            1931
            4049
            7013
            14591
            30293
            62851
            130363
            225307
            467237
            968897
            2009191
            4166287
            8293349
            16605227
            33554393
            67108859
            134217649
            268435399
            536870909
            1073741789
            2147483587
        |]

    [<IsReadOnly>]
    let PrimeMultiplier =
        [|
            1676976733973595602UL
            636094623231363849UL
            312656679215416130UL
            172399477324388333UL
            77183029597111095UL
            42799870240625410UL
            20072626848432592UL
            9552948769399043UL
            4555876530923575UL
            2630364191317490UL
            1264254956734258UL
            608944114934459UL
            293499611361945UL
            141502911667495UL
            81873816941816UL
            39480486506227UL
            19038911332897UL
            9181179924512UL
            4427622022609UL
            2224281659160UL
            1110899843388UL
            549756452865UL
            274877927425UL
            137439034369UL
            68719491329UL
            34359738561UL
            17179869745UL
            8589934837UL
        |]

    let NonRandomizedStringEqualityComparerType =
        GetType<Dictionary<int, int>>("System.Collections.Generic.NonRandomizedStringEqualityComparer")

    let GetStringComparerMethodCall : Func<IEqualityComparer<string>, IEqualityComparer<string>> =
        CreateStaticCallLambda
            NonRandomizedStringEqualityComparerType
            "GetStringComparer"
            Reflection.BindingFlags.Public

    let GetUnderlyingEqualityComparerMethodCall : Func<IEqualityComparer<string>, IEqualityComparer<string>> =
        CreateStaticCallLambda
            (GetType<Dictionary<int, int>> "System.Collections.Generic.IInternalStringEqualityComparer")
            "GetUnderlyingEqualityComparer"
            Reflection.BindingFlags.NonPublic

    let GetRandomizedEqualityComparerMethodCall : Func<IEqualityComparer<string>, IEqualityComparer<string>> =
        CreateInstanceAsCallLambda
            NonRandomizedStringEqualityComparerType
            "GetRandomizedEqualityComparer"
            Reflection.BindingFlags.NonPublic

    let inline GetRandomizedEqualityComparer comparer = GetRandomizedEqualityComparerMethodCall.Invoke(comparer)

    let inline GetStringComparer comparer = GetStringComparerMethodCall.Invoke(comparer)

    let inline GetUnderlyingEqualityComparer comparer = GetUnderlyingEqualityComparerMethodCall.Invoke(comparer)

    [<LocalsInit(false)>]
    let inline ApplyComparer<'T, 'E when 'E :> IEqualityComparer<'T>> (keyComparer : 'E) : 'E =
        let tType = typeof<'T>

        if not tType.IsValueType then
            if tType.Equals(typeof<string>) then
                (keyComparer.As() |> GetStringComparer).As()
            else
                match keyComparer.IsNull() with
                | true -> EqualityComparer<'T>.Default.As ()
                | _ -> keyComparer
        else if keyComparer.IsOfType<EqualityComparer<'T>>() then
            Unchecked.defaultof<_>
        else
            keyComparer

    [<LocalsInit(false)>]
    let inline ApplyComparer1<'E, 'T when 'E :> IEqualityComparer<'T> and 'E : null> (keyComparer : 'E) : 'E =
        let tType = typeof<'T>

        if not tType.IsValueType then
            if tType.Equals(typeof<string>) then
                (keyComparer.As() |> GetStringComparer).As()
            else
                match keyComparer with
                | null -> EqualityComparer<'T>.Default.As ()
                | _ -> keyComparer.As()
        else if keyComparer.IsOfType<EqualityComparer<'T>>() then
            Unchecked.defaultof<_>
        else
            keyComparer.As()

    let inline FastMod (value : uint) (divisor : int64) (multiplier : uint64) =
        (int)
            (
                ((multiplier * (uint64) value >>> 32)
                 + 1UL)
                * (uint64) divisor
                >>> 32
            )

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetFastModMultiplier divisor = PrimeMultiplier.[divisor |> SizeToIndex]

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetNextPrime oldSize = PrimesDoublingInSize.[SizeToIndex(oldSize) + 1]

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetNextPrimeAndMultiplier (oldSize, multiplier : _ outref) =
        let index = SizeToIndex(oldSize) + 1
        multiplier <- PrimeMultiplier.[index]
        PrimesDoublingInSize.[index]

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetPrime size = PrimesDoublingInSize.[size |> SizeToIndex]

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetPrimeAndBucket (size, bucket : _ outref, multiplier : _ outref) =
        bucket <- SizeToIndex(size)
        multiplier <- PrimeMultiplier.[bucket]
        PrimesDoublingInSize.[bucket]

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetNextBucketAndMultiplier (size, multiplier : _ outref) =
        let bucket = SizeToIndex(size) + 1
        multiplier <- PrimeMultiplier.[bucket]
        bucket

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetBucketAndMultiplier (size, multiplier : _ outref) =
        let bucket = SizeToIndex(size)
        multiplier <- PrimeMultiplier.[bucket]
        bucket

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline GetPrimeAndMultiplier (size, multiplier : _ outref) =
        let index = SizeToIndex(size)
        multiplier <- PrimeMultiplier.[index]
        PrimesDoublingInSize.[index]

    [<Sealed>]
    type HashBucket =
        val mutable internal size : int
        val mutable internal ptr : int nativeptr
        new (hashBucketSize : int) = { size = hashBucketSize ; ptr = alignedAllocFill (hashBucketSize * 4) 4 -1 }

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member inline internal x.Release () = x.ptr |> alignedFree

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member inline internal x.Update () = x.ptr <- alignedAllocFill (x.size * 4) 4 -1

        member x.Span = asSpan x.ptr x.size

        member x.Item
            with get (index : int) = x.ptr.[index]
            and set (index : int) value = x.ptr.[index] <- value

        member x.Length = x.size

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        [<LocalsInit(false)>]
        member x.GetBucketReference index = &x.ptr.Reference index

        member x.Resize (newSize) =
            x.size <- newSize
            x.Release()
            x.Update() |> ignore

        member inline internal x.Dispose (_ : bool) = x.Release()

        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member x.Dispose () =
            x.Dispose true
            GC.SuppressFinalize(x)

        override x.Finalize () = x.Dispose false

        interface IDisposable with
            [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
            member x.Dispose () = x.Dispose()
