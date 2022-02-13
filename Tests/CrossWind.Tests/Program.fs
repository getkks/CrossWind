namespace CrossWind.Tests

module Main =

    open Expecto
    open CrossWind.Collections.Test

    [<EntryPoint>]
    let main args =
        ``Collection Tests``.Tests
        |> Test.shuffle defaultConfig.joinWith.asString
        |> runTestsWithCLIArgs
            [ //Summary
              Colours(256)
              Verbosity(Logging.LogLevel.Debug)
              Log_Name("CrossWind.Tests")
              CLIArguments.Parallel ]
            args
