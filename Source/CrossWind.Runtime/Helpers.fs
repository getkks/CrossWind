namespace CrossWind.Runtime

module Helpers =

    let inline fetchAndUpdate (left : _ byref) right =
        let t = left
        left <- right
        t
