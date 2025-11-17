module BeerTaste.Console.Configuration

open Spectre.Console
open Microsoft.Extensions.Configuration
open BeerTaste.Common
open System

// Anchor type for UserSecrets lookup (the assembly contains the UserSecretsId via the project file)
type SecretsAnchor = class end

type ConsoleSetup = {
    ShortName: string
    ExcelFilePath: string
    TableStorage: BeerTasteTableStorage
}

// Setup folder and copy template file for a BeerTaste event
let setupBeerTasteFolder (filesFolder: string) (shortName: string) : unit =
    // Create a subfolder for this BeerTaste
    let eventFolder = System.IO.Path.Combine(filesFolder, shortName)

    if not (System.IO.Directory.Exists(eventFolder)) then
        AnsiConsole.MarkupLine($"[cyan]Creating folder: {eventFolder}[/]")

        System.IO.Directory.CreateDirectory(eventFolder)
        |> ignore
    else
        AnsiConsole.MarkupLine($"[grey]Folder already exists: {eventFolder}[/]")

    // Copy the BeerTaste.xlsx template if it doesn't exist
    let targetFile = System.IO.Path.Combine(eventFolder, $"{shortName}.xlsx")

    if not (System.IO.File.Exists(targetFile)) then
        // Template file is in the same directory as the executable
        let templateFile = "BeerTaste.xlsx"

        if System.IO.File.Exists(templateFile) then
            AnsiConsole.MarkupLine($"[cyan]Copying template to: {targetFile}[/]")
            System.IO.File.Copy(templateFile, targetFile)
            AnsiConsole.MarkupLine($"[green]Template file created successfully.[/]")
        else
            AnsiConsole.MarkupLine($"[red]Warning: Template file not found at {templateFile}[/]")
    else
        AnsiConsole.MarkupLine($"[grey]Excel file already exists: {targetFile}[/]")

let getConsoleSetup (args: string[]) : ConsoleSetup option =
    // Check if a short name argument was provided
    if args.Length = 0 then
        AnsiConsole.MarkupLine("[red]Error: Short name parameter is required.[/]")
        AnsiConsole.MarkupLine("Usage: [yellow]dotnet run -- <short-name>[/]")
        None
    else
        let shortName = args[0]

        // Load configuration
        let config = ConfigurationBuilder().AddUserSecrets<SecretsAnchor>().AddEnvironmentVariables().Build()

        let connStr = config["BeerTaste:TableStorageConnectionString"]
        // Get the folder path for Excel files, default to "../scripts" relative to executable
        let filesFolder =
            let configPath = config["BeerTaste:FilesFolder"]

            if String.IsNullOrWhiteSpace configPath then
                // Default to scripts directory relative to the project
                "./BeerTastes" |> System.IO.Path.GetFullPath
            else
                configPath |> System.IO.Path.GetFullPath

        setupBeerTasteFolder filesFolder shortName

        let excelFilePath = System.IO.Path.Combine(filesFolder, shortName, $"{shortName}.xlsx")

        if String.IsNullOrWhiteSpace connStr then
            AnsiConsole.MarkupLine(
                "[red]Missing connection string 'BeerTaste:TableStorageConnectionString' in user secrets or environment.[/]"
            )

            AnsiConsole.MarkupLine(
                "Use: [yellow]dotnet user-secrets set \"BeerTaste:TableStorageConnectionString\" \"<your-connection-string>\"[/]"
            )

            None
        else
            // Display configured files folder
            AnsiConsole.MarkupLine($"[grey]Files folder: {filesFolder}[/]")

            // Ensure the folder exists
            if not (System.IO.Directory.Exists(filesFolder)) then
                AnsiConsole.MarkupLine($"[yellow]Warning: Files folder does not exist. Creating: {filesFolder}[/]")

                System.IO.Directory.CreateDirectory(filesFolder)
                |> ignore

                None
            else
                {
                    ShortName = shortName
                    ExcelFilePath = excelFilePath
                    TableStorage = connStr |> BeerTasteTableStorage
                }
                |> Some
