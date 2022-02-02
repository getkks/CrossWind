namespace CrossWind.Runtime

open System.Numerics

module CollectionHelpers =
    
    let MaximumArraySize = 2146435071

    let inline IndexToSize index = 16 <<< index

    let inline SizeToIndex size =
        if size < 1 then
            0
        else
            BitOperations.Log2((uint) size - 1u ||| 15u)
            - 3
