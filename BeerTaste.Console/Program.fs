module BeerTaste.Console.Program

open Spectre.Console
open OfficeOpenXml
open BeerTaste.Console.Configuration
open BeerTaste.Console.Workflow
open FsToolkit.ErrorHandling

// Set EPPlus license for non-commercial use (EPPlus 8.x API)
do ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")

let workflow (args: string[]) =
    option {
        let! setup = args |> getConsoleSetup
        let beerTasteGuid = setupBeerTaste setup
        let! beers = verifyBeers setup beerTasteGuid
        let! tasters = verifyTasters setup beerTasteGuid beers
        let! scores = verifyScores setup beerTasteGuid
        showResults setup beerTasteGuid scores tasters
    }

[<EntryPoint>]
let main args =
    try
        match args |> workflow with
        | Some() -> 0
        | None -> 1

    with ex ->
        AnsiConsole.MarkupLineInterpolated($"[red]Error: {ex.Message}[/]")
        1
