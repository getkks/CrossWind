namespace CrossWind.Runtime

open System.Runtime.CompilerServices
open Microsoft.FSharp.NativeInterop
open System.Runtime.InteropServices

#nowarn "1204" "42" "9"

[<Extension>]
type ObjectExtensions =
    //https://github.com/stephan-tolksdorf/fparsec/blob/fdd990ad5abe32fd65d926005b4c7bd71dd2384f/FParsec/Internals.fs#L14
    [<Extension>]
    static member inline ReferenceEquals (x : 'T, y : 'T) = (# "ceq" x y : bool #) //obj.ReferenceEquals(x, y)

    //https://github.com/stephan-tolksdorf/fparsec/blob/fdd990ad5abe32fd65d926005b4c7bd71dd2384f/FParsec/Internals.fs#L16
    [<Extension>]
    static member inline IsNull x = (# "ldnull ceq" x : bool #) //ObjectExtensions.ReferenceEquals(x, null)

    //https://github.com/stephan-tolksdorf/fparsec/blob/fdd990ad5abe32fd65d926005b4c7bd71dd2384f/FParsec/Internals.fs#L18
    [<Extension>]
    static member inline IsNotNull x = (# "ldnull cgt.un" x : bool #) //ObjectExtensions.ReferenceEquals(x, null)|> not

    [<Extension>]
    static member inline IsOfType<'T> object = LanguagePrimitives.IntrinsicFunctions.TypeTestFast<'T>(object)

    [<Extension>]
    static member inline As (x : 'T) =

#if INTERACTIVE
        unbox x
#else
        (# "" x: 'U #)
#endif

module TypeHelpers =
    type nativeptr<'T when 'T : unmanaged> with

        member inline x.Reference index =
            &
                Unsafe.AsRef<'T>(
                    index
                    |> NativePtr.add x
                    |> NativePtr.toVoidPtr
                )

        member inline x.LessThan right = (NativePtr.toNativeInt x) < (NativePtr.toNativeInt right)

        member inline x.LessThanOrEqual right =
            (NativePtr.toNativeInt x)
            <= (NativePtr.toNativeInt right)

        member inline x.GreaterThan right = (NativePtr.toNativeInt x) > (NativePtr.toNativeInt right)

        member inline x.GreaterThanOrEqual right =
            (NativePtr.toNativeInt x)
            >= (NativePtr.toNativeInt right)

        member inline x.Equal right = (NativePtr.toNativeInt x) = (NativePtr.toNativeInt right)

        member inline x.NotEqual right =
            (NativePtr.toNativeInt x)
            <> (NativePtr.toNativeInt right)

        member inline x.Item
            with get (index) = index |> NativePtr.get x
            and set index value = value |> NativePtr.set x index

        member inline x.Item
            with get (index : uint32) = index |> int32 |> NativePtr.get x
            and set (index : uint32) value =
                value
                |> NativePtr.set x (index |> int32)

        member inline x.Add (index : int32) = index |> NativePtr.add x

        member inline x.Subtract (index : _ nativeptr) =
            (NativePtr.toNativeInt x)
            - (NativePtr.toNativeInt index)

        member inline x.Add (index : uint32) = index |> int32 |> NativePtr.add x

        member inline x.Add (index : uint8) = index |> int32 |> NativePtr.add x

        member inline x.Value
            with get () = NativePtr.read x
            and set value = NativePtr.write x value

    let inline zeroCreateUncheckedArray<'T> (count : int) = (# "newarr !0" count : 'T array #)

    let inline GetType<'TypeForChoosingAssembly> typeName = typeof<'TypeForChoosingAssembly>.Assembly.GetType (typeName)

    let inline asSpan (ptr : 'T nativeptr) size =
        MemoryMarshal.CreateSpan(&Unsafe.AsRef<'T>(ptr |> NativePtr.toVoidPtr), size)

    let inline alignedAlloc size alignment =
        (size |> unativeint, alignment |> unativeint)
        |> NativeMemory.AlignedAlloc
        |> NativePtr.ofVoidPtr

    let inline alignedReAlloc (ptr : 'T nativeptr) size alignment : 'T nativeptr =
        (ptr |> NativePtr.toVoidPtr, size |> unativeint, alignment |> unativeint)
        |> NativeMemory.AlignedRealloc
        |> NativePtr.ofVoidPtr

    let inline alignedFree ptr =
        ptr
        |> NativePtr.toVoidPtr
        |> NativeMemory.AlignedFree

    let inline ptrFill ptr size value = (asSpan ptr size).Fill(value)

    let inline alignedAllocFill size alignment value =
        let ptr = alignedAlloc size alignment
        ptrFill ptr size value
        ptr
