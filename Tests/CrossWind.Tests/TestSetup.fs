namespace CrossWind.Tests

open System
open System.Linq
open Fixie
open System.Reflection
//Adopted from https://github.com/UglyToad/Fixie.DataDriven/blob/0f122a666572a6a93f491a19a3c65f82d9672397/src/UglyToad.Fixie.DataDriven/MemberDataAttribute.cs#L18
/// <summary>
/// Get the test data from a member of this class. One of:
/// Property
/// Field
/// Parameterless Method
/// The member must be static and have type equivalent to <see cref="IEnumerable{Object}"/>
/// The member can have any access modifier (public, internal, protected, private).
/// The member can be on a base class and will automatically be located without specifying the type.
/// </summary>
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type MemberDataAttribute (memberName : string) =
    inherit Attribute ()

    [<DefaultValue>]
    val mutable Type : Type

    member val MemberName = memberName with get, set

    static member GetData (methodInfo : MethodInfo) =
        let attributes = methodInfo.GetCustomAttributes<MemberDataAttribute>(true).ToList()

        if attributes.Count <> 0 then
            attributes.SelectMany(fun x -> MemberDataAttribute.GetSingleAttributeData(methodInfo, x))
        else
            Array.Empty<obj []>()

    static member GetMemberInfoByName (ty, getAllMemberInfos, filterStatement) =
        let rec loop ty getAllMemberInfos filterStatement =
            if ty = null || ty = typeof<obj> then
                null
            else
                match ty
                      |> getAllMemberInfos
                      |> Seq.tryFind filterStatement
                    with
                | Some info -> info
                | _ -> loop ty.BaseType getAllMemberInfos filterStatement

        loop ty getAllMemberInfos filterStatement

    static member GetFromProperty (memberDataAttribute : MemberDataAttribute, ty) =
        let propInfo =
            MemberDataAttribute.GetMemberInfoByName(
                ty,
                (fun t -> t.GetRuntimeProperties()),
                (fun p -> p.Name.Equals(memberDataAttribute.MemberName, StringComparison.InvariantCultureIgnoreCase))
            )

        if propInfo = null
           || propInfo.GetMethod = null
           || not propInfo.GetMethod.IsStatic then
            None
        else
            Some(fun () -> propInfo.GetValue(null, null))

    static member GetFromMethod (memberDataAttribute : MemberDataAttribute, ty) =
        let methodInfo =
            MemberDataAttribute.GetMemberInfoByName(
                ty,
                (fun t -> t.GetRuntimeMethods()),
                (fun p -> p.Name.Equals(memberDataAttribute.MemberName, StringComparison.InvariantCultureIgnoreCase))
            )

        if methodInfo = null
           || not methodInfo.IsStatic then
            None
        else
            Some(fun () -> methodInfo.Invoke(null, null))

    static member GetFromField (memberDataAttribute : MemberDataAttribute, ty) =
        let fieldInfo =
            MemberDataAttribute.GetMemberInfoByName(
                ty,
                (fun t -> t.GetRuntimeFields()),
                (fun p -> p.Name.Equals(memberDataAttribute.MemberName, StringComparison.InvariantCultureIgnoreCase))
            )

        if fieldInfo = null
           || not fieldInfo.IsStatic then
            None
        else
            Some(fun () -> fieldInfo.GetValue(null))

    static member GetSingleAttributeData (methodInfo : MethodInfo, attribute : MemberDataAttribute) =
        let targetType = if attribute.Type <> null then attribute.Type else methodInfo.DeclaringType

        (match MemberDataAttribute.GetFromMethod(attribute, targetType) with
         | Some f -> f ()
         | _ ->
             match MemberDataAttribute.GetFromProperty(attribute, targetType) with
             | Some f -> f ()
             | _ ->
                 match MemberDataAttribute.GetFromField(attribute, targetType) with
                 | Some f -> f ()
                 | _ -> null)
        |> unbox<obj array seq>

[<AttributeUsage(AttributeTargets.Method)>]
type TestAttribute () =
    inherit Attribute ()

[<AttributeUsage(AttributeTargets.Class)>]
type TestClassAttribute () =
    inherit Attribute ()

type AttributeDiscovery () =
    interface IDiscovery with
        member _.TestClasses concreteClasses =
            concreteClasses
            |> Seq.filter (fun (c : Type) -> c.Has<TestClassAttribute>())

        member _.TestMethods publicMethods =
            publicMethods
            |> Seq.filter (fun (method : MethodInfo) -> method.Has<TestAttribute>())

type ParameterizedExecution () =
    interface IExecution with
        member _.Run (testSuite : TestSuite) =
            task {
                for test in testSuite.Tests do
                    if test.Has<MemberDataAttribute>() then
                        for parameters in MemberDataAttribute.GetData(test.Method) do
                            test.Run(parameters).Wait()
                    else
                        test.Run().Wait()
            }

type TestProject () =
    interface ITestProject with
        member _.Configure (configuration : TestConfiguration, environment : TestEnvironment) =
            configuration.Conventions.Add<AttributeDiscovery, ParameterizedExecution>()
