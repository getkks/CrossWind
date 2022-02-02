namespace CrossWind.Runtime

open System
open System.Collections.Generic
open System.Linq.Expressions
open System.Reflection
open LocalsInit

module ExpressionHelpers =
    [<LocalsInit(false)>]
    let CreateStaticCallLambda (typeOfObject : Type) methodName flags : Func<_, _> =
        let param = Expression.Parameter(typeof<IEqualityComparer<string>>)

        Expression
            .Lambda<_>(
                Expression.Call(null, typeOfObject.GetMethod(methodName, flags ||| BindingFlags.Static), param),
                param
            )
            .Compile()


    [<LocalsInit(false)>]
    let CreateInterfaceCallLambda (typeOfInterface : Type) typeOfObject methodName flags : Func<_, _> =
        let param = Expression.Parameter(typeof<IEqualityComparer<string>>)

        Expression
            .Lambda<_>(
                Expression.Call(
                    Expression.TypeAs(param, typeOfObject),
                    typeOfInterface.GetMethod(methodName, flags ||| BindingFlags.Instance),
                    null
                ),
                param
            )

            .Compile()

    [<LocalsInit(false)>]
    let CreateInstanceAsCallLambda typeOfObject methodName flags : Func<_, _> =
        let param = Expression.Parameter(typeof<IEqualityComparer<string>>)

        Expression
            .Lambda<_>(
                Expression.Call(
                    Expression.TypeAs(param, typeOfObject),
                    typeOfObject.GetMethod(methodName, flags ||| BindingFlags.Instance),
                    null
                ),
                param
            )
            .Compile()
