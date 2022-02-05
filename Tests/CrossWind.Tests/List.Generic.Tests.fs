namespace CrossWind.Collections.Tests

open CrossWind.Collections

[<AbstractClass>]
type ``List Generic Tests``<'T when 'T: equality>() =
    inherit ``IList Generic Tests``<'T>()

    abstract GenericListFactory : unit -> 'T PooledList.PooledList
    default _.GenericListFactory() = new PooledList.PooledList<'T>()

    abstract GenericListFactory : count: int -> 'T PooledList.PooledList

    default x.GenericListFactory count =
        let toCreateFrom =
            x.CreateEnumerable(EnumerableType.List, null, count, 0, 0)

        let list =
            new PooledList.PooledList<'T>(toCreateFrom)

        list |> x.RegisterForDispose |> ignore
        list

    override x.GenericIListFactory count = x.GenericListFactory count
    override x.GenericIListFactory() = x.GenericListFactory()
