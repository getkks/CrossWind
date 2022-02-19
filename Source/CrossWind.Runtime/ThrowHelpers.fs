namespace CrossWind.Runtime

open System
open System.Collections.Generic
open System.Diagnostics.CodeAnalysis
open System.Runtime.CompilerServices
open System.Runtime.Serialization
/// <summary>
/// Argument names for throw helpers.
/// </summary>
[<NoEquality ; NoComparison ; RequireQualifiedAccess>]
type ExceptionArgument =
    | Obj
    | Dictionary
    | Array
    | Info
    | Key
    | Text
    | Values
    | Value
    | StartIndex
    | Task
    | Ch
    | S
    | Input
    | List
    | Index
    | Capacity
    | Collection
    | Item
    | Converter
    | Match
    | Count
    | Action
    | Comparison
    | Exceptions
    | Exception
    | Enumerable
    | Start
    | Format
    | Culture
    | Comparer
    | Comparable
    | Source
    | State
    | Length
    | ComparisonType
    | Manager
    | SourceBytesToCopy
    | CallBack
    | CreationOptions
    | Function
    | Delay
    | MillisecondsDelay
    | MillisecondsTimeout
    | Timeout
    | Type
    | SourceIndex
    | SourceArray
    | DestinationIndex
    | DestinationArray
    | Other
    | NewSize
    | LowerBounds
    | Lengths
    | Len
    | Keys
    | Indices
    | EndIndex
    | ElementType
    | ArrayIndex

    override x.ToString () =
        match x with
        | Obj -> "obj"
        | Dictionary -> "dictionary"
        | Array -> "array"
        | Info -> "info"
        | Key -> "key"
        | Text -> "text"
        | Values -> "values"
        | Value -> "value"
        | StartIndex -> "startIndex"
        | Task -> "task"
        | Ch -> "ch"
        | S -> "s"
        | Input -> "input"
        | List -> "list"
        | Index -> "index"
        | Capacity -> "capacity"
        | Collection -> "collection"
        | Item -> "item"
        | Converter -> "converter"
        | Match -> "match"
        | Count -> "count"
        | Action -> "action"
        | Comparison -> "comparison"
        | Exceptions -> "exceptions"
        | Exception -> "exception"
        | Enumerable -> "enumerable"
        | Start -> "start"
        | Format -> "format"
        | Culture -> "culture"
        | Comparer -> "comparer"
        | Comparable -> "comparable"
        | Source -> "source"
        | State -> "state"
        | Length -> "length"
        | ComparisonType -> "comparisonType"
        | Manager -> "manager"
        | SourceBytesToCopy -> "sourceBytesToCopy"
        | CallBack -> "callBack"
        | CreationOptions -> "creationOptions"
        | Function -> "function"
        | Delay -> "delay"
        | MillisecondsDelay -> "millisecondsDelay"
        | MillisecondsTimeout -> "millisecondsTimeout"
        | Timeout -> "timeout"
        | Type -> "type"
        | SourceIndex -> "sourceIndex"
        | SourceArray -> "sourceArray"
        | DestinationIndex -> "destinationIndex"
        | DestinationArray -> "destinationArray"
        | Other -> "other"
        | NewSize -> "newSize"
        | LowerBounds -> "lowerBounds"
        | Lengths -> "lengths"
        | Len -> "len"
        | Keys -> "keys"
        | Indices -> "indices"
        | EndIndex -> "endIndex"
        | ElementType -> "elementType"
        | ArrayIndex -> "arrayIndex"
/// <summary>
/// Exception message for throw helpers.
/// </summary>
[<NoEquality ; NoComparison>]
type ExceptionResource =
    | ArgumentOutOfRange_Index
    | ArgumentOutOfRange_Count
    | Argument_ArrayPlusOffTooSmall
    | NotSupported_ReadOnlyCollection
    | Argument_RankMultiDimNotSupported
    | Argument_NonZeroLowerBound
    | ArgumentOutOfRange_ListInsert
    | ArgumentOutOfRange_NeedNonNegNum
    | ArgumentOutOfRange_SmallCapacity
    | Argument_InvalidOffLen
    | ArgumentOutOfRange_BiggerThanCollection
    | Serialization_MissingKeys
    | Serialization_NullKey
    | NotSupported_KeyCollectionSet
    | NotSupported_ValueCollectionSet
    | InvalidOperation_NullArray
    | InvalidOperation_HSCapacityOverflow
    | NotSupported_StringComparison
    | ConcurrentCollection_SyncRoot_NotSupported
    | ArgumentException_OtherNotArrayOfCorrectLength
    | ArgumentOutOfRange_EndIndexStartIndex
    | ArgumentOutOfRange_HugeArrayNotSupported
    | Argument_AddingDuplicate
    | Argument_InvalidArgumentForComparison
    | Argument_LowerBoundsMustMatch
    | Argument_MustBeType
    | InvalidOperation_IComparerFailed
    | NotSupported_FixedSizeCollection
    | Rank_MultiDimNotSupported
    | Argument_TypeNotSupported

    override x.ToString () =
        match x with
        | ArgumentOutOfRange_Index -> "Argument 'index' was out of the range of valid values."
        | ArgumentOutOfRange_Count -> "Argument 'count' was out of the range of valid values."
        | Argument_ArrayPlusOffTooSmall -> "Array plus offset too small."
        | NotSupported_ReadOnlyCollection -> "This operation is not supported on a read-only collection."
        | Argument_RankMultiDimNotSupported -> "Multi-dimensional arrays are not supported."
        | Argument_NonZeroLowerBound -> "Arrays with a non-zero lower bound are not supported."
        | ArgumentOutOfRange_ListInsert -> "Insertion index was out of the range of valid values."
        | ArgumentOutOfRange_NeedNonNegNum -> "The number must be non-negative."
        | ArgumentOutOfRange_SmallCapacity -> "The capacity cannot be set below the current Count."
        | Argument_InvalidOffLen -> "Invalid offset length."
        | ArgumentOutOfRange_BiggerThanCollection -> "The given value was larger than the size of the collection."
        | Serialization_MissingKeys -> "Serialization error: missing keys."
        | Serialization_NullKey -> "Serialization error: null key."
        | NotSupported_KeyCollectionSet -> "The KeyCollection does not support modification."
        | NotSupported_ValueCollectionSet -> "The ValueCollection does not support modification."
        | InvalidOperation_NullArray -> "Null arrays are not supported."
        | InvalidOperation_HSCapacityOverflow -> "Set hash capacity overflow. Cannot increase size."
        | NotSupported_StringComparison -> "String comparison not supported."
        | ConcurrentCollection_SyncRoot_NotSupported -> "SyncRoot not supported."
        | ArgumentException_OtherNotArrayOfCorrectLength -> "The other array is not of the correct length."
        | ArgumentOutOfRange_EndIndexStartIndex -> "The end index does not come after the start index."
        | ArgumentOutOfRange_HugeArrayNotSupported -> "Huge arrays are not supported."
        | Argument_AddingDuplicate -> "Duplicate item added."
        | Argument_InvalidArgumentForComparison -> "Invalid argument for comparison."
        | Argument_LowerBoundsMustMatch -> "Array lower bounds must match."
        | Argument_MustBeType -> "Argument must be of type: "
        | InvalidOperation_IComparerFailed -> "IComparer failed."
        | NotSupported_FixedSizeCollection -> "This operation is not suppored on a fixed-size collection."
        | Rank_MultiDimNotSupported -> "Multi-dimensional arrays are not supported."
        | Argument_TypeNotSupported -> "Type not supported."

type ThrowHelpers =
    [<DoesNotReturn>]
    static member ThrowArrayTypeMismatchException () : unit = ArrayTypeMismatchException() |> raise

    [<DoesNotReturn>]
    static member ThrowIndexOutOfRangeException () : unit = IndexOutOfRangeException() |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentOutOfRangeException () : unit = ArgumentOutOfRangeException() |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentException_DestinationTooShort () : unit =
        ArgumentException("Destination too short.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentException_OverlapAlignmentMismatch () : unit =
        ArgumentException("Overlap alignment mismatch.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentOutOfRange_IndexException () : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(ExceptionArgument.Index, ArgumentOutOfRange_Index)
        |> raise

    [<DoesNotReturn>]
    static member ThrowIndexArgumentOutOfRange_NeedNonNegNumException () : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(ExceptionArgument.Index, ArgumentOutOfRange_NeedNonNegNum)
        |> raise

    [<DoesNotReturn>]
    static member ThrowValueArgumentOutOfRange_NeedNonNegNumException () : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(ExceptionArgument.Value, ArgumentOutOfRange_NeedNonNegNum)
        |> raise

    [<DoesNotReturn>]
    static member ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum () : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(ExceptionArgument.Length, ArgumentOutOfRange_NeedNonNegNum)
        |> raise

    [<DoesNotReturn>]
    static member ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index () : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(ExceptionArgument.StartIndex, ArgumentOutOfRange_Index)
        |> raise

    [<DoesNotReturn>]
    static member ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count () : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(ExceptionArgument.Count, ArgumentOutOfRange_Count)
        |> raise

    [<DoesNotReturn>]
    static member ThrowWrongKeyTypeArgumentException<'T> (key : 'T, targetType : Type) : unit =
        // Generic key to move the boxing to the right hand side of throw
        ThrowHelpers.GetWrongKeyTypeArgumentException(key, targetType)
        |> raise

    [<DoesNotReturn>]
    static member ThrowWrongValueTypeArgumentException<'T> (value : 'T, targetType : Type) : unit =
        // Generic key to move the boxing to the right hand side of throw
        ThrowHelpers.GetWrongValueTypeArgumentException(value, targetType)
        |> raise

    [<DoesNotReturn>]
    static member GetAddingDuplicateWithKeyArgumentException (key : 'T) =
        let defaultInterpolatedStringHandler = DefaultInterpolatedStringHandler(34, 1)
        defaultInterpolatedStringHandler.AppendLiteral("Error adding duplicate with key: ")
        defaultInterpolatedStringHandler.AppendFormatted(key)
        defaultInterpolatedStringHandler.AppendLiteral(".")
        ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear())

    [<DoesNotReturn>]
    static member ThrowAddingDuplicateWithKeyArgumentException<'T> (key : 'T) : unit =
        // Generic key to move the boxing to the right hand side of throw
        ThrowHelpers.GetAddingDuplicateWithKeyArgumentException(key)
        |> raise

    [<DoesNotReturn>]
    static member ThrowKeyNotFoundException (key : 'T) : unit =
        // Generic key to move the boxing to the right hand side of throw
        ThrowHelpers.GetKeyNotFoundException(key)
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentException (resource : ExceptionResource) : unit =
        ThrowHelpers.GetArgumentException(resource)
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentException (resource : ExceptionResource, argument : ExceptionArgument) : unit =
        ThrowHelpers.GetArgumentExceptionWithArgument(resource, argument)
        |> raise

    [<DoesNotReturn>]
    static member GetArgumentNullException (argument : ExceptionArgument) = ArgumentNullException(argument.ToString())

    [<DoesNotReturn>]
    static member ThrowArgumentNullException (argument : ExceptionArgument) : unit =
        ThrowHelpers.GetArgumentNullException(argument)
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentNullException (resource : ExceptionResource) : unit =
        ArgumentNullException(resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentNullException (argument : ExceptionArgument, resource : ExceptionResource) : unit =
        ArgumentNullException(argument.ToString(), resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentOutOfRangeException (argument : ExceptionArgument) : unit =
        ArgumentOutOfRangeException(argument.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentOutOfRangeException (argument : ExceptionArgument, resource : ExceptionResource) : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(argument, resource)
        |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentOutOfRangeException
        (
            argument : ExceptionArgument,
            paramNumber : int,
            resource : ExceptionResource
        ) : unit =
        ThrowHelpers.GetArgumentOutOfRangeException(argument, paramNumber, resource)
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException (resource : ExceptionResource) : unit =
        ThrowHelpers.GetInvalidOperationException(resource)
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException (resource : ExceptionResource, e) : unit =
        InvalidOperationException(resource.ToString(), e)
        |> raise

    [<DoesNotReturn>]
    static member ThrowSerializationException (resource : ExceptionResource) : unit =
        SerializationException(resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowSecurityException (resource : ExceptionResource) : unit =
        Security.SecurityException(resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowRankException (resource : ExceptionResource) : unit =
        RankException(resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowNotSupportedException (resource : ExceptionResource) : unit =
        NotSupportedException(resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowUnauthorizedAccessException (resource : ExceptionResource) : unit =
        UnauthorizedAccessException(resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowObjectDisposedException (objectName, resource : ExceptionResource) : unit =
        ObjectDisposedException(objectName, resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowObjectDisposedException (resource : ExceptionResource) : unit =
        ObjectDisposedException(null, resource.ToString())
        |> raise

    [<DoesNotReturn>]
    static member ThrowNotSupportedException () : unit = NotSupportedException() |> raise

    [<DoesNotReturn>]
    static member ThrowAggregateException (exceptions : List<Exception>) : unit =
        AggregateException(exceptions) |> raise

    [<DoesNotReturn>]
    static member ThrowOutOfMemoryException () : unit = OutOfMemoryException() |> raise

    [<DoesNotReturn>]
    static member ThrowArgumentException_Argument_InvalidArrayType () : unit =
        ArgumentException("Invalid array type.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_InvalidOperation_EnumNotStarted () : unit =
        InvalidOperationException("Enumeration has not started.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_InvalidOperation_EnumEnded () : unit =
        InvalidOperationException("Enumeration has ended.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_EnumCurrent (index : int) : unit =
        ThrowHelpers.GetInvalidOperationException_EnumCurrent(index)
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion () : unit =
        InvalidOperationException("Collection was modified during enumeration.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen () : unit =
        InvalidOperationException("Invalid enumerator state: enumeration cannot proceed.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_InvalidOperation_NoValue () : unit =
        InvalidOperationException("No value provided.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_ConcurrentOperationsNotSupported () : unit =
        InvalidOperationException("Concurrent operations are not supported.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowInvalidOperationException_HandleIsNotInitialized () : unit =
        InvalidOperationException("Handle is not initialized.")
        |> raise

    [<DoesNotReturn>]
    static member ThrowFormatException_BadFormatSpecifier () : unit =
        FormatException("Bad format specifier.")
        |> raise

    [<DoesNotReturn>]
    static member GetArgumentException (resource : ExceptionResource) = ArgumentException(resource.ToString())

    [<DoesNotReturn>]
    static member GetInvalidOperationException (resource : ExceptionResource) =
        InvalidOperationException(resource.ToString())

    static member GetWrongKeyTypeArgumentException (key : 'T, targetType : Type) =
        let defaultInterpolatedStringHandler = DefaultInterpolatedStringHandler(35, 2)
        defaultInterpolatedStringHandler.AppendLiteral("Wrong key type. Expected ")
        defaultInterpolatedStringHandler.AppendFormatted(targetType)
        defaultInterpolatedStringHandler.AppendLiteral(", got: '")
        defaultInterpolatedStringHandler.AppendFormatted(key)
        defaultInterpolatedStringHandler.AppendLiteral("'.")
        ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "key")

    static member GetWrongValueTypeArgumentException (value : 'T, targetType : Type) =
        let defaultInterpolatedStringHandler = DefaultInterpolatedStringHandler(37, 2)
        defaultInterpolatedStringHandler.AppendLiteral("Wrong value type. Expected ")
        defaultInterpolatedStringHandler.AppendFormatted(targetType)
        defaultInterpolatedStringHandler.AppendLiteral(", got: '")
        defaultInterpolatedStringHandler.AppendFormatted(value)
        defaultInterpolatedStringHandler.AppendLiteral("'.")
        ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "value")

    static member GetKeyNotFoundException (key : 'T) =
        let defaultInterpolatedStringHandler = DefaultInterpolatedStringHandler(15, 1)
        defaultInterpolatedStringHandler.AppendLiteral("Key not found: ")
        defaultInterpolatedStringHandler.AppendFormatted(key)
        KeyNotFoundException(defaultInterpolatedStringHandler.ToStringAndClear())

    static member GetArgumentOutOfRangeException (argument : ExceptionArgument, resource : ExceptionResource) =
        ArgumentOutOfRangeException(argument.ToString(), resource.ToString())

    static member GetArgumentExceptionWithArgument (resource : ExceptionResource, argument : ExceptionArgument) =
        ArgumentException(resource.ToString(), argument.ToString())

    static member GetArgumentOutOfRangeException
        (
            argument : ExceptionArgument,
            paramNumber : int,
            resource : ExceptionResource
        ) =
        ArgumentOutOfRangeException(
            argument.ToString()
            + "["
            + paramNumber.ToString()
            + "]",
            resource.ToString()
        )

    static member GetInvalidOperationException_EnumCurrent (index : int) =
        InvalidOperationException(if index < 0 then "Enumeration has not started" else "Enumeration has ended")

    // Allow nulls for reference types and Nullable<U>, but not for value types.
    // Aggressively inline so the jit evaluates the if in place and either drops the call altogether
    // Or just leaves null test and call to the Non-returning ThrowHelper.ThrowArgumentNullException
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member inline IfNullAndNullsAreIllegalThenThrow<'T> (value : obj, argName : ExceptionArgument) : unit =
        // Note that default<'T> is not equal to null for value types except when T is Nullable<U>.
        if Object.ReferenceEquals(Unchecked.defaultof<'T>, null)
           |> not
           && value |> isNull then
            ThrowHelpers.ThrowArgumentNullException(argName)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member inline ThrowForUnsupportedVectorBaseType<'T when 'T : struct> () : unit =
        //To work around F# Equality. Generated code is closer to C#.
        if
            System.Type.op_Inequality (typeof<'T>, typeof<byte>)
            && System.Type.op_Inequality (typeof<'T>, typeof<sbyte>)
            && System.Type.op_Inequality (typeof<'T>, typeof<int16>)
            && System.Type.op_Inequality (typeof<'T>, typeof<uint16>)
            && System.Type.op_Inequality (typeof<'T>, typeof<int>)
            && System.Type.op_Inequality (typeof<'T>, typeof<uint>)
            && System.Type.op_Inequality (typeof<'T>, typeof<int64>)
            && System.Type.op_Inequality (typeof<'T>, typeof<uint64>)
            && System.Type.op_Inequality (typeof<'T>, typeof<float>)
            && System.Type.op_Inequality (typeof<'T>, typeof<double>)
        then
            ThrowHelpers.ThrowNotSupportedException(Argument_TypeNotSupported)
