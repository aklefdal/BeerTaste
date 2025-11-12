module BeerTaste.Console.Program

open Spectre.Console
open OfficeOpenXml
open BeerTaste.Console.Configuration
open BeerTaste.Console.Workflow

// Set EPPlus license for non-commercial use (EPPlus 8.x API)
do ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")

[<EntryPoint>]
let main args =
    try    
        match args |> getConsoleSetup with
        | None -> 1
        | Some setup ->
            let beerTasteGuid = setupBeerTaste setup
            
            match verifyBeers setup beerTasteGuid with
            | None -> 1
            | Some beers ->
                verifyTasters setup beers beerTasteGuid
                0
    with ex ->
        AnsiConsole.MarkupLineInterpolated($"[red]Error: {ex.Message}[/]")
        1
