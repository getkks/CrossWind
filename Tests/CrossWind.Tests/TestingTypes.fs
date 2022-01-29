#nowarn "343" "345" "386"

namespace CrossWind.Collections.Tests

open System
open System.Collections
open System.Collections.Generic

[<Serializable>]
type BadIntEqualityComparer =
    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = x = y

        member _.GetHashCode (obj : int) = obj % 2

    override _.Equals (o : Object) = o :? BadIntEqualityComparer // Equal to all other instances of this type, not to anything else.

    override _.GetHashCode () = (int) 0xC001CAFE // Doesn't matter as long as its constant.

[<Serializable>]
type ComparerSameAsDefaultComparer =

    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = x = y
        member _.GetHashCode (x : int) = x.GetHashCode()

    interface IComparer<int> with

        member _.Compare (x : int, y) = x - y

[<Serializable>]
type ComparerHashCodeAlwaysReturnsZero =

    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = x = y
        member _.GetHashCode (x : int) = 0

    interface IComparer<int> with

        member _.Compare (x : int, y) = x - y

[<Serializable>]
type ComparerModOfInt (modulo : int) =
    new () = ComparerModOfInt(500)

    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = (x % modulo) = (y % modulo)
        member _.GetHashCode (x : int) = x % modulo

    interface IComparer<int> with

        member _.Compare (x : int, y) = ((x % modulo) - (y % modulo))

[<Serializable>]
type ComparerAbsOfInt =

    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = Math.Abs(x) = Math.Abs(y)
        member _.GetHashCode (x : int) = Math.Abs(x)

    interface IComparer<int> with

        member _.Compare (x : int, y) = Math.Abs(x) - Math.Abs(y)

[<Serializable ; Struct ; CustomComparison ; CustomEquality>]
type SimpleInt =
    val Val : int
    new (value : int) = { Val = value }

    interface IComparable<SimpleInt> with
        member x.CompareTo (other : SimpleInt) = other.Val - x.Val

    interface IComparable with

        member x.CompareTo (o : Object) =
            match o with
            | :? SimpleInt as other -> other.Val - x.Val
            | _ -> -1

    interface IStructuralComparable with
        member x.CompareTo (o : Object, comparer) =
            match o with
            | :? SimpleInt as other -> other.Val - x.Val
            | _ -> -1

    override x.GetHashCode () = x.Val

    override x.Equals (o : Object) =
        match o with
        | :? SimpleInt as other -> other.Val = x.Val
        | _ -> false

    interface IStructuralEquatable with
        member x.Equals (other, comparer) =
            match other with
            | :? SimpleInt as other -> other.Equals(x)
            | _ -> false

        member x.GetHashCode (comparer) = comparer.GetHashCode(x)

[<Serializable>]
type WrapStructuralInt =

    interface IComparer<int> with

        member _.Compare (x : int, y) = StructuralComparisons.StructuralComparer.Compare(x, y)

    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = StructuralComparisons.StructuralEqualityComparer.Equals(x, y)
        member _.GetHashCode (x : int) = StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj)

[<Serializable>]
type WrapStructuralSimpleInt =

    interface IComparer<int> with

        member _.Compare (x : int, y) = StructuralComparisons.StructuralComparer.Compare(x, y)

    interface IEqualityComparer<int> with
        member _.Equals (x : int, y) = StructuralComparisons.StructuralEqualityComparer.Equals(x, y)
        member _.GetHashCode (x : int) = StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj)

[<Serializable ; NoComparison ; NoEquality>]
type BadlyBehavingComparable =
    interface IComparable with

        member _.CompareTo (_ : Object) = 1

    interface IComparable<BadlyBehavingComparable> with
        member _.CompareTo (_) = -1

[<NoComparison ; NoEquality>]
type ValueComparable<'T when 'T :> IComparable<'T>> =
    val Value : 'T
    new (value : 'T) = { Value = value }

    interface IComparable<ValueComparable<'T>> with
        member x.CompareTo (other) = x.Value.CompareTo(other.Value)

module ValueComparable =
    let inline Create value = value |> ValueComparable

[<Struct ; NoComparison ; NoEquality>]
type NonEquatableValueType =
    val mutable Value : int
    new (value : int) = { Value = value }

[<Struct ; NoComparison ; NoEquality>]
type ValueDelegateEquatable =
    val EqualsWorker : ValueDelegateEquatable -> bool
    new (value : ValueDelegateEquatable -> bool) = { EqualsWorker = value }

    interface IEquatable<ValueDelegateEquatable> with
        member x.Equals (other) = x.EqualsWorker other

[<Serializable>]
type Comparer_AbsOfInt =
    interface IEqualityComparer<int> with
        member _.Equals (x, y) = Math.Abs(x) = Math.Abs(y)

        member _.GetHashCode x = Math.Abs(x)

    interface IComparer<int> with
        member _.Compare (x, y) = Math.Abs(x) - Math.Abs(y)

[<Serializable>]
type Comparer_HashCodeAlwaysReturnsZero =
    interface IEqualityComparer<int> with
        member _.Equals (x, y) = x = y

        member _.GetHashCode _ = 0

    interface IComparer<int> with
        member _.Compare (x, y) = x - y

[<Serializable>]
type Comparer_ModOfInt (m : int) =
    new () = Comparer_ModOfInt(500)

    interface IEqualityComparer<int> with
        member _.Equals (x, y) = x % m = y % m

        member _.GetHashCode x = x % m

    interface IComparer<int> with
        member _.Compare (x, y) = x % m - y % m

[<Serializable>]
type Comparer_SameAsDefaultComparer =
    interface IEqualityComparer<int> with
        member _.Equals (x, y) = x = y

        member _.GetHashCode x = x.GetHashCode()

    interface IComparer<int> with
        member _.Compare (x, y) = x - y

[<NoComparison ; NoEquality>]
type DelegateEquatable () =
    member val EqualsWorker : DelegateEquatable -> bool = fun _ -> false

    interface IEquatable<DelegateEquatable> with
        member x.Equals (other) = x.EqualsWorker other

[<NoComparison ; NoEquality>]
type Equatable (value : int) =
    member val Value = value

    interface IEquatable<Equatable> with
        member x.Equals (other) = x.Value = other.Value

    override x.GetHashCode () = x.Value

[<Serializable>]
type EquatableBackwardsOrder (v : int) =
    member val internal value = v

    interface IComparable<EquatableBackwardsOrder> with
        member x.CompareTo (other : EquatableBackwardsOrder) = //backwards from the usual integer ordering
            other.value - x.value

    override x.GetHashCode () = x.value

    override x.Equals (o : Object) =
        match o with
        | :? EquatableBackwardsOrder as other -> other.value = x.value
        | _ -> false

    interface IEquatable<EquatableBackwardsOrder> with
        member x.Equals (other : EquatableBackwardsOrder) = x.value = other.value

    interface IComparable with

        member x.CompareTo (o : Object) =
            match o with
            | :? EquatableBackwardsOrder as other -> other.value - x.value
            | _ -> -1

[<Serializable ; NoComparison ; NoEquality>]
type GenericComparable (v : int) =
    member val Val = v

    interface IComparable<GenericComparable> with
        member x.CompareTo (other : GenericComparable) = other.Val - x.Val

[<Serializable ; NoComparison ; NoEquality>]
type MutatingComparable (s : int) =
    member val State = s with get, set

    interface IComparable with

        member x.CompareTo (_ : Object) =
            let r = x.State
            x.State <- x.State + 1
            r

    interface IComparable<MutatingComparable> with
        member x.CompareTo (_) =
            let r = x.State
            x.State <- x.State + 1
            r

[<Serializable ; NoComparison ; NoEquality>]
type NonGenericComparable (i : GenericComparable) =
    member val Inner = i

    interface IComparable with

        member x.CompareTo (o : Object) =
            match o with
            | :? NonGenericComparable as o -> (x.Inner :> _ IComparable).CompareTo(o.Inner)
            | _ -> -1

[<Serializable>]
type WrapStructural_Int =
    interface IEqualityComparer<int> with
        member _.Equals (x, y) = StructuralComparisons.StructuralEqualityComparer.Equals(x, y)
        member _.GetHashCode obj = StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj)

    interface IComparer<int> with
        member _.Compare (x, y) = StructuralComparisons.StructuralComparer.Compare(x, y)

[<Serializable>]
type WrapStructural_SimpleInt =
    interface IEqualityComparer<SimpleInt> with
        member _.Equals (x, y) = StructuralComparisons.StructuralEqualityComparer.Equals(x, y)
        member _.GetHashCode obj = StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj)

    interface IComparer<SimpleInt> with
        member _.Compare (x, y) = StructuralComparisons.StructuralComparer.Compare(x, y)
