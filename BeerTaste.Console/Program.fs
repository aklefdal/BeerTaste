open System
open Spectre.Console
open Microsoft.Extensions.Configuration
open Azure.Data.Tables
open Azure
open OfficeOpenXml

// Set EPPlus license for non-commercial use (EPPlus 8.x API)
do ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")

// Anchor type for UserSecrets lookup (the assembly contains the UserSecretsId via the project file)
type SecretsAnchor = class end

// Beer type definition
type Beer = {
    Id: int
    Name: string
    BeerType: string
    Origin: string
    Producer: string
    ABV: float
    Volume: float
    Price: float
    Packaging: string
}

// Taster type definition
type Taster = {
    Name: string
    Email: string
    BirthYear: int
}

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

// Beer entity type for Azure Table Storage
type BeerEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Name = "" with get, set
    member val BeerType = "" with get, set
    member val Origin = "" with get, set
    member val Producer = "" with get, set
    member val ABV = 0.0 with get, set
    member val Volume = 0.0 with get, set
    member val Price = 0.0 with get, set
    member val Packaging = "" with get, set

    new(partitionKey: string, rowKey: string, beer: Beer) as this =
        BeerEntity()
        then
            (this :> ITableEntity).PartitionKey <- partitionKey
            (this :> ITableEntity).RowKey <- rowKey
            this.Name <- beer.Name
            this.BeerType <- beer.BeerType
            this.Origin <- beer.Origin
            this.Producer <- beer.Producer
            this.ABV <- beer.ABV
            this.Volume <- beer.Volume
            this.Price <- beer.Price
            this.Packaging <- beer.Packaging

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

// Get the PartitionKey for a BeerTaste by short name
let getBeerTastePartitionKey (table: TableClient) (shortName: string) : string option =
    try
        let query = table.Query<BeerTasteEntity>(filter = $"RowKey eq '{shortName}'")
        let entity = query |> Seq.tryHead
        entity |> Option.map (fun e -> (e :> ITableEntity).PartitionKey)
    with
    | _ -> None

// Delete all beers for a given partition key
let deleteBeersForPartitionKey (beersTable: TableClient) (partitionKey: string) : unit =
    try
        let query = beersTable.Query<BeerEntity>(filter = $"PartitionKey eq '{partitionKey}'")
        for entity in query do
            beersTable.DeleteEntity(entity) |> ignore
    with
    | _ -> ()

// Add beers to the beers table
let addBeers (beersTable: TableClient) (partitionKey: string) (beers: Beer list) : unit =
    beers
    |> List.iter (fun beer ->
        let rowKey = beer.Id.ToString()
        let entity = BeerEntity(partitionKey, rowKey, beer)
        beersTable.AddEntity(entity) |> ignore)

// Helper function for Norwegian decimal format (comma to dot)
let norwegianToFloat (s: string) : float = s.Replace(",", ".") |> float

// Read a single beer from a worksheet row
let rowToBeer (worksheet: ExcelWorksheet) (row: int) : Beer = {
    Id = worksheet.Cells.[row, 1].Text |> int
    Name = worksheet.Cells.[row, 2].Text
    BeerType = worksheet.Cells.[row, 3].Text
    Origin = worksheet.Cells.[row, 4].Text
    Producer = worksheet.Cells.[row, 5].Text
    ABV = worksheet.Cells.[row, 6].Text |> norwegianToFloat
    Volume = worksheet.Cells.[row, 7].Text |> norwegianToFloat
    Price = worksheet.Cells.[row, 8].Text |> norwegianToFloat
    Packaging = worksheet.Cells.[row, 9].Text
}

// Read beers from Excel file
let readBeers (fileName: string) : Beer list =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets.["Beers"]

    if worksheet.Dimension = null then
        []
    else
        seq { 2 .. worksheet.Dimension.End.Row }
        |> Seq.map (rowToBeer worksheet)
        |> Seq.toList

// Read a single taster from a worksheet row
let rowToTaster (worksheet: ExcelWorksheet) (row: int) : Taster = {
    Name = worksheet.Cells.[row, 1].Text
    Email = worksheet.Cells.[row, 2].Text
    BirthYear = worksheet.Cells.[row, 3].Text |> int
}

// Read tasters from Excel file
let readTasters (fileName: string) : Taster list =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets.["Tasters"]

    if worksheet.Dimension = null then
        []
    else
        seq { 2 .. worksheet.Dimension.End.Row }
        |> Seq.map (rowToTaster worksheet)
        |> Seq.toList

// Create TastersSchema worksheet
let createTastersSchema (fileName: string) (beers: Beer list) : unit =
    use package = new ExcelPackage(fileName)

    let schemaName = "TastersSchema " + DateTime.Now.ToString("yyyy-MM-dd HHmmss")

    let worksheet = package.Workbook.Worksheets.Copy("TastersSchema", schemaName)
    let height = worksheet.Row(3).Height

    worksheet.InsertRow(3, beers.Length - 1, 3) |> ignore

    for i in 3 .. (beers.Length + 1) do
        worksheet.Row(i).Height <- height

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells.[i + 3, 1].Value <- beer.Id
        worksheet.Cells.[i + 3, 2].Value <- beer.Producer
        worksheet.Cells.[i + 3, 3].Value <- beer.Name
        worksheet.Cells.[i + 3, 4].Value <- beer.BeerType
        worksheet.Cells.[i + 3, 5].Value <- beer.Origin
        worksheet.Cells.[i + 3, 6].Value <- beer.ABV)

    package.Save()

// Create ScoreSchema worksheet
let createScoreSchema (fileName: string) (beers: Beer list) (tasters: Taster list) : unit =
    use package = new ExcelPackage(fileName)

    let sheetName = "ScoreSchema " + DateTime.Now.ToString("yyyy-MM-dd HHmmss")

    let worksheet = package.Workbook.Worksheets.Add(sheetName)
    worksheet.Cells.[1, 1].Value <- "Id"
    worksheet.Cells.[1, 2].Value <- "Producer"
    worksheet.Cells.[1, 3].Value <- "Name"

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells.[i + 2, 1].Value <- beer.Id
        worksheet.Cells.[i + 2, 2].Value <- beer.Producer
        worksheet.Cells.[i + 2, 3].Value <- beer.Name)

    tasters
    |> List.iteri (fun i taster -> worksheet.Cells.[1, i + 4].Value <- taster.Name)

    worksheet.Row(1).Style.Font.Bold <- true
    worksheet.Column(1).Style.Font.Bold <- true
    worksheet.Column(2).Style.Font.Bold <- true
    worksheet.Column(3).Style.Font.Bold <- true

    package.Save()

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

// Prompt user if they are done editing beers
let promptDoneEditingBeers () : bool =
    AnsiConsole.Confirm("[yellow]Are you done editing the beers?[/]", false)

// Prompt user if they are done editing tasters
let promptDoneEditingTasters () : bool =
    AnsiConsole.Confirm("[yellow]Are you done editing the tasters?[/]", false)

// Setup folder and copy template file for a BeerTaste event
let setupBeerTasteFolder (filesFolder: string) (shortName: string) : unit =
    // Create subfolder for this BeerTaste
    let eventFolder = System.IO.Path.Combine(filesFolder, shortName)
    if not (System.IO.Directory.Exists(eventFolder)) then
        AnsiConsole.MarkupLine($"[cyan]Creating folder: {eventFolder}[/]")
        System.IO.Directory.CreateDirectory(eventFolder) |> ignore
    else
        AnsiConsole.MarkupLine($"[grey]Folder already exists: {eventFolder}[/]")

    // Copy BeerTaste.xlsx template if it doesn't exist
    let targetFile = System.IO.Path.Combine(eventFolder, $"{shortName}.xlsx")
    if not (System.IO.File.Exists(targetFile)) then
        // Template file is in the same directory as the executable
        let templateFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BeerTaste.xlsx")
        if System.IO.File.Exists(templateFile) then
            AnsiConsole.MarkupLine($"[cyan]Copying template to: {targetFile}[/]")
            System.IO.File.Copy(templateFile, targetFile)
            AnsiConsole.MarkupLine($"[green]Template file created successfully.[/]")
        else
            AnsiConsole.MarkupLine($"[red]Warning: Template file not found at {templateFile}[/]")
    else
        AnsiConsole.MarkupLine($"[grey]Excel file already exists: {targetFile}[/]")

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

                let beersTableName = "beers"
                let beersTable = service.GetTableClient(beersTableName)
                beersTable.CreateIfNotExists() |> ignore

                AnsiConsole.MarkupLine($"[green]Connected to Table Storage.[/] Tables [bold]{tableName}[/] and [bold]{beersTableName}[/] ready.")

                // Check if BeerTaste exists
                let exists = beerTasteExists table shortName

                if not exists then
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
                else
                    AnsiConsole.MarkupLine($"[green]BeerTaste with short name '{shortName}' found.[/]")

                // Setup folder and template file (for both new and existing entries)
                setupBeerTasteFolder filesFolder shortName

                // Get the partition key for this BeerTaste
                let partitionKeyOpt = getBeerTastePartitionKey table shortName
                let partitionKey =
                    match partitionKeyOpt with
                    | Some pk -> pk
                    | None ->
                        AnsiConsole.MarkupLine("[red]Error: Could not retrieve BeerTaste partition key.[/]")
                        ""

                // Get the Excel file path
                let excelFilePath = System.IO.Path.Combine(filesFolder, shortName, $"{shortName}.xlsx")

                // Read beers from the Excel file
                try
                    let beers = readBeers excelFilePath
                    AnsiConsole.MarkupLine($"[green]Found {beers.Length} beer(s) in the Excel file.[/]")

                    // Only ask if user is done editing when there are 2 or more beers
                    if beers.Length > 1 then
                        let doneEditing = promptDoneEditingBeers ()
                        if doneEditing then
                            AnsiConsole.MarkupLine("[cyan]Creating TastersSchema worksheet...[/]")
                            createTastersSchema excelFilePath beers
                            AnsiConsole.MarkupLine("[green]TastersSchema worksheet created successfully![/]")

                            // Save beers to Azure Table Storage
                            if not (String.IsNullOrWhiteSpace(partitionKey)) then
                                try
                                    AnsiConsole.MarkupLine("[cyan]Saving beers to Azure Table Storage...[/]")
                                    deleteBeersForPartitionKey beersTable partitionKey
                                    addBeers beersTable partitionKey beers
                                    AnsiConsole.MarkupLine($"[green]Successfully saved {beers.Length} beer(s) to Azure Table Storage.[/]")
                                with ex ->
                                    AnsiConsole.MarkupLine($"[red]Warning: Could not save beers to Azure Table Storage: {ex.Message}[/]")

                            // Now read tasters from the Excel file
                            try
                                let tasters = readTasters excelFilePath
                                AnsiConsole.MarkupLine($"[green]Found {tasters.Length} taster(s) in the Excel file.[/]")

                                // Only ask if user is done editing when there are 2 or more tasters
                                if tasters.Length > 1 then
                                    let doneTasters = promptDoneEditingTasters ()
                                    if doneTasters then
                                        AnsiConsole.MarkupLine("[cyan]Creating ScoreSchema worksheet...[/]")
                                        createScoreSchema excelFilePath beers tasters
                                        AnsiConsole.MarkupLine("[green]ScoreSchema worksheet created successfully![/]")
                                    else
                                        AnsiConsole.MarkupLine("[yellow]Please continue editing the tasters in the Excel file.[/]")
                                elif tasters.Length = 1 then
                                    AnsiConsole.MarkupLine("[yellow]Only one taster found. Please add more tasters to the Excel file.[/]")
                            with ex ->
                                AnsiConsole.MarkupLine($"[red]Warning: Could not read tasters from Excel file: {ex.Message}[/]")
                        else
                            AnsiConsole.MarkupLine("[yellow]Please continue editing the beers in the Excel file.[/]")
                    elif beers.Length = 1 then
                        AnsiConsole.MarkupLine("[yellow]Only one beer found. Please add more beers to the Excel file.[/]")
                with ex ->
                    AnsiConsole.MarkupLine($"[red]Warning: Could not read beers from Excel file: {ex.Message}[/]")

                0
            with ex ->
                AnsiConsole.MarkupLineInterpolated($"[red]Error: {ex.Message}[/]")
                1
