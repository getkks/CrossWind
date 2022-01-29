namespace CrossWind.Runtime

//#r "nuget: FParsec, 1.1.1"
open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open FParsec

module rec Tao =
    open System
    open System.Buffers

    [<NoComparison ; NoEquality>]
    type Tao =
        { Parts : List<Part> }

        member x.Push part = x.Parts.Add part

        override x.ToString () =
            let mutable ret = ""

            for part in x.Parts do
                ret <- ret + part.ToString()

            ret

    and [<NoComparison ; NoEquality>] Part =
        | Note of string
        | Op of char
        | Other
        | Tree of Tao

        override x.ToString () =
            match x with
            | Tree t -> "[" + t.ToString() + "]"
            | Op c -> "`" + c.ToString()
            | Note s -> s
            | Other -> ""

    [<NoComparison ; NoEquality>]
    type Bound = { Position : int ; Line : int ; Column : int ; Symbol : char }

    type Input (str : string) =
        let length = str.Length
        let mutable position = 0
        let mutable line = 0
        let mutable column = 0
        let bounds = Stack()

        member _.Done = position >= length
        member _.At symbol = str.[position] = symbol
        member _.Current = str.[position]

        member x.MoveNext () =
            position <- position + 1

            if not x.Done then
                let c = str.[position]

                if c = '\n' then
                    line <- line + 1
                    column <- 0
                else
                    column <- column + 1

            x.Done |> not

        member _.Peek = str.[position]

        member _.Error (name : string) : unit =
            let defaultInterpolatedStringHandler = DefaultInterpolatedStringHandler(43, 6)
            defaultInterpolatedStringHandler.AppendFormatted(line)
            defaultInterpolatedStringHandler.AppendLiteral(":")
            defaultInterpolatedStringHandler.AppendFormatted(column)
            defaultInterpolatedStringHandler.AppendLiteral(": malformed ")
            defaultInterpolatedStringHandler.AppendFormatted(name)
            defaultInterpolatedStringHandler.AppendLiteral(" at line ")
            defaultInterpolatedStringHandler.AppendFormatted(line)
            defaultInterpolatedStringHandler.AppendLiteral(" column ")
            defaultInterpolatedStringHandler.AppendFormatted(column)
            defaultInterpolatedStringHandler.AppendLiteral(" (position ")
            defaultInterpolatedStringHandler.AppendFormatted(position)
            defaultInterpolatedStringHandler.AppendLiteral(").")

            Exception(defaultInterpolatedStringHandler.ToStringAndClear())
            |> raise

        member _.Bound (symbol : char) =
            bounds.Push({ Position = position ; Line = line ; Column = column ; Symbol = symbol })

        member _.UnBound () = bounds.Pop() |> ignore

        member x.AtBound =
            if bounds.Count > 0 then
                let bound = bounds.Peek()

                if x.Done then
                    let defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(77, 6)
                    defaultInterpolatedStringHandler.AppendFormatted(line)
                    defaultInterpolatedStringHandler.AppendLiteral(":")
                    defaultInterpolatedStringHandler.AppendFormatted(column)
                    defaultInterpolatedStringHandler.AppendLiteral(": expected symbol ")
                    defaultInterpolatedStringHandler.AppendFormatted(bound.Symbol)
                    defaultInterpolatedStringHandler.AppendLiteral(" before the end of input since line ")
                    defaultInterpolatedStringHandler.AppendFormatted(bound.Line)
                    defaultInterpolatedStringHandler.AppendLiteral(", column ")
                    defaultInterpolatedStringHandler.AppendFormatted(bound.Column)
                    defaultInterpolatedStringHandler.AppendLiteral(" (position ")
                    defaultInterpolatedStringHandler.AppendFormatted(bound.Position)
                    defaultInterpolatedStringHandler.AppendLiteral(").")

                    Exception(defaultInterpolatedStringHandler.ToStringAndClear())
                    |> raise
                else
                    x.At(bound.Symbol)
            else
                x.Done

    let moveNext (str : _ ReadOnlySpan) = if str.IsEmpty then str else str.Slice(1)

    let bound symbol (stack : _ Stack) = stack.Push(symbol)

    let unBound (str : _ ReadOnlySpan) (stack : _ Stack) =
        match stack.TryPop() with
        | true, x when str.IsEmpty |> not -> x = str.[0]
        | _ -> false

    let atBound (str : _ ReadOnlySpan) (stack : _ Stack) =
        if str.IsEmpty then false
        elif stack.Count > 0 then stack.Peek() = str.[0]
        else true

    let meta (input : Input) =
        input.At('[')
        || input.At(']')
        || input.At('`')

    let note (input : Input) =
        if input.At(']') then input.Error("note (unexpected meta symbol)")
        let builder = StringBuilder()

        while (input.Current
               |> builder.Append
               |> ignore

               input.MoveNext() && input |> meta |> not) do
            ()

        builder.ToString() |> Note

    let op (input : Input) =
        if input.At('`') then
            if input.MoveNext() |> not then input.Error "op (unexpected end of input)"
            input.Current |> Op
        else
            input |> note

    let treeOld (input : Input) =
        if input.At('[') && input.MoveNext() then
            input.Bound(']')
            let tree = parseOld (input)
            input.UnBound()
            input.MoveNext() |> ignore
            Tree(tree)
        else
            op input

    let tree (str : _ ReadOnlySpan) (stack : _ Stack) =
        if str.[0] = '[' then
            let newStr = moveNext str
            if not str.IsEmpty then
                stack.Push(']')
                let tree = parse str
                if unBound str stack then
                    ()//moveNext str
                else ()//error case
                Tree(tree)
            else Other
        else
            Other//op input

    let parseOld (input : Input) =
        let tao = { Parts = List() }

        while not input.AtBound do
            let part = treeOld input
            tao.Push part

        tao

    let parse (str : _ ReadOnlySpan) =
        let tao = { Parts = List() }
        let stack = Stack()

        while not str.IsEmpty do
            let part = tree str stack
            tao.Push part

        tao
    (*
"[subValue1 [subValue2]]" |> Tao.Input |> Tao.parse
*)

    let opChar : Parser<_, unit> = pchar '`'
    let lBracket : Parser<_, unit> = pchar '['
    let rBracket = pchar ']'

    let primitive =
        spaces
        >>. (many1Satisfy (fun x ->
            x <> '['
            && x <> ']'
            && x <> '`'
            && x <> EOS
        ))
        .>> spaces

    let forward = createParserForwardedToRef ()
    let valueParser = fst forward

    let subValueParser = between lBracket rBracket valueParser

    (snd forward).Value <- choice [ attempt subValueParser ; attempt primitive ]
//run valueParser "[subValue1 [subValue2]]"
