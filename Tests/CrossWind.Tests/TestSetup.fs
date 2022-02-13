namespace CrossWind.Tests

[<AutoOpen>]
module TestSetup =

    open Expecto
    open System

    let genericTypePropertyTest<'T, 'TestType> typeName testName : 'TestType -> Test =
        let rec formattedName (t : Type) =
            match t.IsGenericType with
            | true ->
                t.Name.Split('`')[0]
                + "<"
                + String.Join(
                    ',',
                    t.GenericTypeArguments
                    |> Seq.map formattedName
                )
                + ">"
            | _ -> t.Name

        testProperty
        <| String.Concat(typeName, "<", formattedName typeof<'T>, ">.", testName)
