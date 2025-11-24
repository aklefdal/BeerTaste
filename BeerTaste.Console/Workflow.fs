module BeerTaste.Console.Workflow

open System
open System.Diagnostics
open Spectre.Console
open BeerTaste.Common
open BeerTaste.Common.Beers
open BeerTaste.Common.BeerTasteStorage
open BeerTaste.Common.Tasters
open BeerTaste.Common.Scores
open BeerTaste.Console.Beers
open BeerTaste.Console.Tasters
open BeerTaste.Console.Scores
open BeerTaste.Console.Configuration

let createScoreSchema
    (scoresTableClient: Azure.Data.Tables.TableClient)
    (beerTasteGuid: string)
    (fileName: string)
    (beers: Beer list)
    (tasters: Taster list)
    =
    match fileName |> getScoresSchemaState with
    | DoesNotExist
    | ExistsWithoutScores -> deleteAndCreateScoreSchema scoresTableClient beerTasteGuid fileName beers tasters
    | ExistsAndComplete
    | ExistsWithScores ->
        AnsiConsole.MarkupLine("[yellow]Warning: The existing ScoreSchema worksheet contains scores![/]")

        let confirm = AnsiConsole.Confirm("[red]Do you want to delete it and create a new one?[/]", false)

        if not confirm then
            AnsiConsole.MarkupLine("[yellow]ScoreSchema creation cancelled. Keeping existing worksheet.[/]")
        else
            deleteAndCreateScoreSchema scoresTableClient beerTasteGuid fileName beers tasters

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
let promptForDate () : DateOnly =
    AnsiConsole.Prompt(
        TextPrompt<string>("Enter [green]date[/] (yyyy-MM-dd):")
            .PromptStyle("yellow")
            .ValidationErrorMessage("[red]Invalid date format. Use yyyy-MM-dd[/]")
            .Validate(fun input ->
                match DateOnly.TryParse(input) with
                | true, _ -> ValidationResult.Success()
                | false, _ -> ValidationResult.Error("[red]Invalid date format. Use yyyy-MM-dd[/]"))
    )
    |> DateOnly.Parse

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

let verifyTasters (setup: ConsoleSetup) (beerTasteGuid: string) (beers: Beer list) : Taster list option =
    try
        let tasters = readTasters setup.ExcelFilePath
        AnsiConsole.MarkupLine($"[green]Found {tasters.Length} taster(s) in the Excel file.[/]")

        // Only ask if the user is done editing when there are 2 or more tasters
        if tasters.Length <= 1 then
            AnsiConsole.MarkupLine("[yellow]Please continue editing the tasters in the Excel file.[/]")
            None
        elif promptDoneEditingTasters () then
            AnsiConsole.MarkupLine("[cyan]Creating ScoreSchema worksheet...[/]")
            createScoreSchema setup.TableStorage.ScoresTableClient beerTasteGuid setup.ExcelFilePath beers tasters
            AnsiConsole.MarkupLine("[green]ScoreSchema worksheet created successfully![/]")

            try
                AnsiConsole.MarkupLine("[cyan]Saving tasters to Azure Table Storage...[/]")
                deleteTastersForPartitionKey setup.TableStorage.TastersTableClient beerTasteGuid
                addTasters setup.TableStorage.TastersTableClient beerTasteGuid tasters

                AnsiConsole.MarkupLine(
                    $"[green]Successfully saved {tasters.Length} taster(s) to Azure Table Storage.[/]"
                )

                Some tasters
            with ex ->
                AnsiConsole.MarkupLine($"[red]Warning: Could not save scores to Azure Table Storage: {ex.Message}[/]")
                None
        else
            Some tasters
    with ex ->
        AnsiConsole.MarkupLine($"[red]Warning: Could not read tasters from Excel file: {ex.Message}[/]")
        None


let verifyScores (setup: ConsoleSetup) (beerTasteGuid: string) =
    try
        let scores = readScores setup.ExcelFilePath

        if scores.Length > 0 then
            AnsiConsole.MarkupLine("[cyan]Saving scores to Azure Table Storage...[/]")
            deleteScoresForBeerTaste setup.TableStorage.ScoresTableClient beerTasteGuid
            addScores setup.TableStorage.ScoresTableClient beerTasteGuid scores

            AnsiConsole.MarkupLine($"[green]Successfully saved {scores.Length} score(s) to Azure Table Storage.[/]")
            Some scores
        else
            None

    with ex ->
        AnsiConsole.MarkupLine($"[red]Warning: Could not read tasters from Excel file: {ex.Message}[/]")
        None

let sendEmailsToTasters (setup: ConsoleSetup) (beerTasteGuid: string) (tasters: Taster list) =
    match setup.EmailConfig with
    | None -> AnsiConsole.MarkupLine("[grey]Email configuration not available. Skipping email sending.[/]")
    | Some emailConfig ->
        let confirm =
            AnsiConsole.Confirm("[yellow]Do you want to send results emails to all tasters?[/]", false)

        if confirm then
            AnsiConsole.MarkupLine("[cyan]Sending emails to tasters...[/]")

            let resultsUrl = $"{setup.ResultsBaseUrl}/{beerTasteGuid}/results"

            // Filter tasters who have email addresses
            let tastersWithEmail =
                tasters
                |> List.choose (fun t ->
                    match t.Email with
                    | Some email when not (String.IsNullOrWhiteSpace(email)) -> Some(t, email)
                    | _ -> None)

            if tastersWithEmail.IsEmpty then
                AnsiConsole.MarkupLine("[yellow]No tasters with email addresses found.[/]")
            else
                // Create email messages for each taster
                let messages =
                    tastersWithEmail
                    |> List.map (fun (taster, email) ->
                        let baseMessage = Email.createBeerTasteResultsEmail taster.Name setup.ShortName resultsUrl
                        { baseMessage with To = email })

                // Send emails (F# idiomatic way to block on async in console apps)
                let results =
                    Email.sendEmails emailConfig messages
                    |> Async.RunSynchronously

                // Partition results into successes and failures
                let (successes, failures) =
                    results
                    |> List.partition (fun (_, result) ->
                        match result with
                        | Ok _ -> true
                        | Error _ -> false)

                let successCount = successes |> List.length
                let failCount = failures |> List.length

                if successCount > 0 then
                    AnsiConsole.MarkupLine($"[green]Successfully sent {successCount} email(s).[/]")

                if failCount > 0 then
                    AnsiConsole.MarkupLine($"[red]Failed to send {failCount} email(s).[/]")

                    // Show details of failures
                    failures
                    |> List.choose (fun (msg, result) ->
                        match result with
                        | Error err -> Some(msg.To, err)
                        | _ -> None)
                    |> List.iter (fun (email, err) -> AnsiConsole.MarkupLine($"[red]  - {email}: {err}[/]"))
        else
            AnsiConsole.MarkupLine("[yellow]Email sending cancelled.[/]")

let showResults (setup: ConsoleSetup) (beerTasteGuid: string) (scores: Score list) (tasters: Taster list) : unit =
    if scores |> Scores.isComplete then
        // Open results page in browser
        try
            let url = $"{setup.ResultsBaseUrl}/{beerTasteGuid}/results"
            AnsiConsole.MarkupLine($"[cyan]Opening results page in browser: {url}[/]")

            let psi = ProcessStartInfo(url, UseShellExecute = true)
            Process.Start(psi) |> ignore
        with ex ->
            AnsiConsole.MarkupLine($"[yellow]Could not open browser: {ex.Message}[/]")

        // Ask if user wants to send emails
        sendEmailsToTasters setup beerTasteGuid tasters
    else
        AnsiConsole.MarkupLine("[yellow]Scores are not complete.[/]")
