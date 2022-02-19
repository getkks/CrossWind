namespace CrossWind.Collections.Test

open Expecto
open CrossWind.Collections

module ``Collection Tests`` =

    [<Tests>]
    let Tests =
        testList
            "Collection Tests"
            [ (``PooledList<'T> Tests``<int> ()).Tests() ; (``PooledList<'T> Tests``<string> ()).Tests() ]
