open System
open Spectre.Console
open Microsoft.Extensions.Configuration
open Azure.Data.Tables
open Azure

// Anchor type for UserSecrets lookup (the assembly contains the UserSecretsId via the project file)
type SecretsAnchor = class end

// BeerTaste entity type for Azure Table Storage
type BeerTasteEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Description = "" with get, set
    member val Date = DateTime.MinValue with get, set

    new(partitionKey: string, rowKey: string, description: string, date: DateTime) as this =
        BeerTasteEntity()
        then
            (this :> ITableEntity).PartitionKey <- partitionKey
            (this :> ITableEntity).RowKey <- rowKey
            this.Description <- description
            this.Date <- date

// Check if a BeerTaste with the given short name exists
let beerTasteExists (table: TableClient) (shortName: string) : bool =
    try
        // Query for any entity with the given RowKey
        let query = table.Query<BeerTasteEntity>(filter = $"RowKey eq '{shortName}'")
        query |> Seq.isEmpty |> not
    with
    | _ -> false

// Add a new BeerTaste entity to the table
let addBeerTaste (table: TableClient) (shortName: string) (description: string) (date: DateTime) =
    let partitionKey = Guid.NewGuid().ToString()
    // Azure Table Storage requires UTC dates
    let utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc)
    let entity = BeerTasteEntity(partitionKey, shortName, description, utcDate)
    table.AddEntity(entity) |> ignore

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
                    ValidationResult.Success()))

// Prompt user for date
let promptForDate () : DateTime =
    AnsiConsole.Prompt(
        TextPrompt<string>("Enter [green]date[/] (yyyy-MM-dd):")
            .PromptStyle("yellow")
            .ValidationErrorMessage("[red]Invalid date format. Use yyyy-MM-dd[/]")
            .Validate(fun input ->
                match DateTime.TryParse(input) with
                | true, _ -> ValidationResult.Success()
                | false, _ -> ValidationResult.Error("[red]Invalid date format. Use yyyy-MM-dd[/]")))
    |> DateTime.Parse

[<EntryPoint>]
let main args =
    // Check if short name argument was provided
    if args.Length = 0 then
        AnsiConsole.MarkupLine("[red]Error: Short name parameter is required.[/]")
        AnsiConsole.MarkupLine("Usage: [yellow]dotnet run -- <short-name>[/]")
        1
    else
        let shortName = args.[0]

        // Load configuration
        let config =
            ConfigurationBuilder()
                .AddUserSecrets<SecretsAnchor>()
                .AddEnvironmentVariables()
                .Build()

        let connStr = config.["BeerTaste:TableStorageConnectionString"]
        // Get the folder path for Excel files, default to "../scripts" relative to executable
        let filesFolder =
            let configPath = config.["BeerTaste:FilesFolder"]
            if String.IsNullOrWhiteSpace configPath then
                // Default to scripts directory relative to the project
                "./BeerTastes" |> System.IO.Path.GetFullPath
            else
                configPath |> System.IO.Path.GetFullPath

        if String.IsNullOrWhiteSpace connStr then
            AnsiConsole.MarkupLine("[red]Missing connection string 'BeerTaste:TableStorageConnectionString' in user secrets or environment.[/]")
            AnsiConsole.MarkupLine("Use: [yellow]dotnet user-secrets set \"BeerTaste:TableStorageConnectionString\" \"<your-connection-string>\"[/]")
            1
        else
            try
                // Display configured files folder
                AnsiConsole.MarkupLine($"[grey]Files folder: {filesFolder}[/]")

                // Ensure the folder exists
                if not (System.IO.Directory.Exists(filesFolder)) then
                    AnsiConsole.MarkupLine($"[yellow]Warning: Files folder does not exist. Creating: {filesFolder}[/]")
                    System.IO.Directory.CreateDirectory(filesFolder) |> ignore

                AnsiConsole.MarkupLine("[grey]Connecting to Azure Table Storage...[/]")
                let service = TableServiceClient(connStr)
                let tableName = "beertaste"
                let table = service.GetTableClient(tableName)
                table.CreateIfNotExists() |> ignore
                AnsiConsole.MarkupLine($"[green]Connected to Table Storage.[/] Table [bold]{tableName}[/] ready.")

                // Check if BeerTaste exists
                if beerTasteExists table shortName then
                    AnsiConsole.MarkupLine($"[yellow]BeerTaste with short name '{shortName}' already exists.[/]")
                    0
                else
                    AnsiConsole.MarkupLine($"[cyan]BeerTaste '{shortName}' not found. Creating new entry...[/]")

                    // Prompt for description and date
                    let description = promptForDescription()
                    let date = promptForDate()

                    // Add to table
                    addBeerTaste table shortName description date

                    let dateStr = date.ToString("yyyy-MM-dd")
                    AnsiConsole.MarkupLine($"[green]Successfully added BeerTaste '{shortName}'![/]")
                    AnsiConsole.MarkupLine($"  Description: [yellow]{description}[/]")
                    AnsiConsole.MarkupLine($"  Date: [yellow]{dateStr}[/]")
                    0
            with ex ->
                AnsiConsole.MarkupLineInterpolated($"[red]Error: {ex.Message}[/]")
                1
