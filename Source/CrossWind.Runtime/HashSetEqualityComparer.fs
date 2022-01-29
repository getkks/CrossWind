namespace CrossWind.Runtime

open System.Runtime.InteropServices
open System.Collections.Generic
open System
open System.Collections

module HashSetEqualityComparer =
    let rec loop<'H, 'T when 'H :> 'T IReadOnlySet and 'H : null> (left : _ IEnumerator) (right : 'H) =
        if left.MoveNext() then
            if left.Current |> right.Contains |> not then
                false
            else
                loop left right
        else
            true

open HashSetEqualityComparer

[<NoComparison ; CustomEquality ; StructLayout(LayoutKind.Sequential, Size = 0)>]
type HashSetEqualityComparer<'H, 'T when 'H :> 'T IReadOnlySet and 'H : null> =
    struct
        member _.Equals (left : 'H, right : 'H) =
            if left.ReferenceEquals(right) then
                true
            else
                match left, right with
                | null, _
                | _, null -> false
                | _ when left.As<_, 'T IReadOnlySet>().Count = right.As<_, 'T IReadOnlySet>().Count ->
                    left.As<_, 'T IReadOnlySet>().IsSubsetOf(right.As())
                | _ -> loop (left.GetEnumerator()) right

        override _.Equals (o : Object) = o.IsOfType<HashSetEqualityComparer<'H, 'T>>()
        override _.GetHashCode () = EqualityComparer<'T>.Default.GetHashCode ()

        member _.GetHashCode (obj : 'H) =
            obj |> ArgumentNullException.ThrowIfNull
            let hashCode = HashCode()

            for item in obj do
                hashCode.Add(item)

            hashCode.ToHashCode()
    end
