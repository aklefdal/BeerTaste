module BeerTaste.Console.Workflow

open System
open Spectre.Console
open BeerTaste.Console.Beers
open BeerTaste.Console.BeerTaste
open BeerTaste.Console.Tasters
open BeerTaste.Console.Scores
open BeerTaste.Console.Configuration

let createScoreSchema (fileName: string) (beers: Beer list) (tasters: Taster list) =
    match fileName |> getScoresSchemaState with
    | DoesNotExist
    | ExistsWithoutScores -> deleteAndCreateScoreSchema fileName beers tasters
    | ExistsWithScores ->
        AnsiConsole.MarkupLine("[yellow]Warning: The existing ScoreSchema worksheet contains scores![/]")

        let confirm =
            AnsiConsole.Confirm("[red]Are you sure you want to delete it and create a new one?[/]", false)

        if not confirm then
            AnsiConsole.MarkupLine("[yellow]ScoreSchema creation cancelled. Keeping existing worksheet.[/]")
        else
            deleteAndCreateScoreSchema fileName beers tasters

// Prompt user if they are done editing beers
let promptDoneEditingBeers () : bool =
    AnsiConsole.Confirm("[yellow]Are you done editing the beers?[/]", false)

let verifyBeers (setup: ConsoleSetup) (beerTasteGuid: string) : Beer list option =
    try
        let beers = readBeers setup.ExcelFilePath
        AnsiConsole.MarkupLine($"[green]Found {beers.Length} beer(s) in the Excel file.[/]")

        // Only ask if the user is done editing when there are 2 or more beers
        if beers.Length <= 1 then
            AnsiConsole.MarkupLine("[yellow]Please continue editing the beers in the Excel file.[/]")
            None
        elif promptDoneEditingBeers () then
            AnsiConsole.MarkupLine("[cyan]Creating TastersSchema worksheet...[/]")
            createTastersSchema setup.ExcelFilePath beers
            AnsiConsole.MarkupLine("[green]TastersSchema worksheet created successfully![/]")

            try
                AnsiConsole.MarkupLine("[cyan]Saving beers to Azure Table Storage...[/]")
                deleteBeersForBeerTaste setup.TableStorage.BeersTableClient beerTasteGuid
                addBeers setup.TableStorage.BeersTableClient beerTasteGuid beers
                AnsiConsole.MarkupLine($"[green]Successfully saved {beers.Length} beer(s) to Azure Table Storage.[/]")
                Some beers
            with ex ->
                AnsiConsole.MarkupLine($"[red]Warning: Could not save beers to Azure Table Storage: {ex.Message}[/]")
                None
        else
            AnsiConsole.MarkupLine("[yellow]Please continue editing the beers in the Excel file.[/]")
            None
    with ex ->
        AnsiConsole.MarkupLine($"[red]Warning: Could not read beers from Excel file: {ex.Message}[/]")
        None

// Prompt user for description
let promptForDescription () : string =
    AnsiConsole.Prompt(
        TextPrompt<string>("Enter [green]description[/]:")
            .PromptStyle("yellow")
            .ValidationErrorMessage("[red]Description cannot be empty[/]")
            .Validate(fun input ->
                if String.IsNullOrWhiteSpace(input) then
                    ValidationResult.Error("[red]Description cannot be empty[/]")
                else
                    ValidationResult.Success())
    )

// Prompt user for date
let promptForDate () : DateTime =
    AnsiConsole.Prompt(
        TextPrompt<string>("Enter [green]date[/] (yyyy-MM-dd):")
            .PromptStyle("yellow")
            .ValidationErrorMessage("[red]Invalid date format. Use yyyy-MM-dd[/]")
            .Validate(fun input ->
                match DateTime.TryParse(input) with
                | true, _ -> ValidationResult.Success()
                | false, _ -> ValidationResult.Error("[red]Invalid date format. Use yyyy-MM-dd[/]"))
    )
    |> DateTime.Parse

let setupBeerTaste (setup: ConsoleSetup) : string =
    match getBeerTasteGuid setup.TableStorage.BeerTasteTableClient setup.ShortName with
    | None ->
        AnsiConsole.MarkupLine($"[cyan]BeerTaste '{setup.ShortName}' not found. Creating new entry...[/]")

        let description = promptForDescription ()
        let date = promptForDate ()

        let beerTasteGuid =
            addBeerTaste setup.TableStorage.BeerTasteTableClient setup.ShortName description date

        let dateStr = date.ToString("yyyy-MM-dd")
        AnsiConsole.MarkupLine($"[green]Successfully added BeerTaste '{setup.ShortName}'![/]")
        AnsiConsole.MarkupLine($"  Description: [yellow]{description}[/]")
        AnsiConsole.MarkupLine($"  Date: [yellow]{dateStr}[/]")
        beerTasteGuid
    | Some beerTasteGuid ->
        AnsiConsole.MarkupLine($"[green]BeerTaste with short name '{setup.ShortName}' found.[/]")
        beerTasteGuid

let promptDoneEditingTasters () : bool =
    AnsiConsole.Confirm("[yellow]Are you done editing the tasters?[/]", false)

let verifyTasters (setup: ConsoleSetup) (beers: Beer list) (beerTasteGuid: string) =
    try
        let tasters = readTasters setup.ExcelFilePath
        AnsiConsole.MarkupLine($"[green]Found {tasters.Length} taster(s) in the Excel file.[/]")

        // Only ask if the user is done editing when there are 2 or more tasters
        if tasters.Length > 1 then
            let doneTasters = promptDoneEditingTasters ()
            if doneTasters then
                AnsiConsole.MarkupLine("[cyan]Creating ScoreSchema worksheet...[/]")
                createScoreSchema setup.ExcelFilePath beers tasters
                AnsiConsole.MarkupLine("[green]ScoreSchema worksheet created successfully![/]")

                // Save tasters to Azure Table Storage
                if not (String.IsNullOrWhiteSpace(beerTasteGuid)) then
                    try
                        AnsiConsole.MarkupLine("[cyan]Saving tasters to Azure Table Storage...[/]")
                        deleteTastersForPartitionKey setup.TableStorage.TastersTableClient beerTasteGuid
                        addTasters setup.TableStorage.TastersTableClient beerTasteGuid tasters
                        AnsiConsole.MarkupLine($"[green]Successfully saved {tasters.Length} taster(s) to Azure Table Storage.[/]")
                    with ex ->
                        AnsiConsole.MarkupLine($"[red]Warning: Could not save tasters to Azure Table Storage: {ex.Message}[/]")
            else
                AnsiConsole.MarkupLine("[yellow]Please continue editing the tasters in the Excel file.[/]")
        elif tasters.Length = 1 then
            AnsiConsole.MarkupLine("[yellow]Only one taster found. Please add more tasters to the Excel file.[/]")
    with ex ->
        AnsiConsole.MarkupLine($"[red]Warning: Could not read tasters from Excel file: {ex.Message}[/]")
    
