module BeerTaste.Console.Configuration

open Spectre.Console
open Microsoft.Extensions.Configuration
open BeerTaste.Common
open System
open System.IO

// Anchor type for UserSecrets lookup (the assembly contains the UserSecretsId via the project file)
type SecretsAnchor = class end

type ConsoleSetup = {
    ShortName: string
    ExcelFilePath: string
    TableStorage: BeerTasteTableStorage
    EmailConfig: EmailConfiguration option
    ResultsBaseUrl: string
}

// Setup folder and copy template file for a BeerTaste event
let setupBeerTasteFolder (filesFolder: string) (shortName: string) : unit =
    if not (Directory.Exists(filesFolder)) then
        AnsiConsole.MarkupLine($"[yellow]Warning: Files folder does not exist. Creating: {filesFolder}[/]")

        Directory.CreateDirectory(filesFolder) |> ignore
    // Display configured files folder
    AnsiConsole.MarkupLine($"[grey]Files folder: {filesFolder}[/]")


    // Create a subfolder for this BeerTaste
    let eventFolder = Path.Combine(filesFolder, shortName)

    if not (Directory.Exists(eventFolder)) then
        AnsiConsole.MarkupLine($"[cyan]Creating folder: {eventFolder}[/]")

        Directory.CreateDirectory(eventFolder) |> ignore
    else
        AnsiConsole.MarkupLine($"[grey]Folder already exists: {eventFolder}[/]")

    // Copy the BeerTaste.xlsx template if it doesn't exist
    let targetFile = Path.Combine(eventFolder, $"{shortName}.xlsx")

    if not (File.Exists(targetFile)) then
        // Template file is in the same directory as the executable
        let templateFile = "BeerTaste.xlsx"

        if File.Exists(templateFile) then
            AnsiConsole.MarkupLine($"[cyan]Copying template to: {targetFile}[/]")
            File.Copy(templateFile, targetFile)
            AnsiConsole.MarkupLine($"[green]Template file created successfully.[/]")
        else
            AnsiConsole.MarkupLine($"[red]Warning: Template file not found at {templateFile}[/]")
    else
        AnsiConsole.MarkupLine($"[grey]Excel file already exists: {targetFile}[/]")

let getEmailConfig (config: IConfigurationRoot) =
    let sendGridApiKey = config["BeerTaste:Email:SendGridApiKey"]
    let fromEmail = config["BeerTaste:Email:FromEmail"]
    let fromName = config["BeerTaste:Email:FromName"]

    let emailSetupMissing =
        String.IsNullOrWhiteSpace sendGridApiKey
        || String.IsNullOrWhiteSpace fromEmail

    if emailSetupMissing then
        AnsiConsole.MarkupLine("[grey]Email configuration not found or incomplete. Email sending will be disabled.[/]")

        None
    else
        AnsiConsole.MarkupLine("[grey]Email configuration loaded successfully.[/]")

        let actualFromName =
            if String.IsNullOrWhiteSpace fromName then
                fromEmail
            else
                fromName

        {
            SendGridApiKey = sendGridApiKey
            FromEmail = fromEmail
            FromName = actualFromName
        }
        |> Some

let getResultsBaseUrl (config: IConfigurationRoot) =
    let configUrl = config["BeerTaste:ResultsBaseUrl"]

    if String.IsNullOrWhiteSpace configUrl then
        "https://beertaste.azurewebsites.net"
    else
        configUrl

let getConsoleSetup (args: string[]) : ConsoleSetup option =
    let config = ConfigurationBuilder().AddUserSecrets<SecretsAnchor>().AddEnvironmentVariables().Build()
    let connStr = config["BeerTaste:TableStorageConnectionString"]
    // Get the folder path for Excel files, default to "../scripts" relative to executable
    let filesFolder =
        let configPath = config["BeerTaste:FilesFolder"]

        if String.IsNullOrWhiteSpace configPath then
            // Default to scripts directory relative to the project
            "./BeerTastes" |> Path.GetFullPath
        else
            configPath |> Path.GetFullPath

    match args.Length, connStr with
    | 0, _ ->
        AnsiConsole.MarkupLine("[red]Error: Short name parameter is required.[/]")
        AnsiConsole.MarkupLine("Usage: [yellow]dotnet run -- <short-name>[/]")
        None
    | _, s when String.IsNullOrWhiteSpace s ->
        AnsiConsole.MarkupLine(
            "[red]Missing connection string 'BeerTaste:TableStorageConnectionString' in user secrets or environment.[/]"
        )

        AnsiConsole.MarkupLine(
            "Use: [yellow]dotnet user-secrets set \"BeerTaste:TableStorageConnectionString\" \"<your-connection-string>\"[/]"
        )

        None
    | _, _ ->
        let shortName = args[0]
        setupBeerTasteFolder filesFolder shortName

        let excelFilePath = Path.Combine(filesFolder, shortName, $"{shortName}.xlsx")
        let emailConfig = getEmailConfig config
        let resultsBaseUrl = getResultsBaseUrl config

        {
            ShortName = shortName
            ExcelFilePath = excelFilePath
            TableStorage = connStr |> BeerTasteTableStorage
            EmailConfig = emailConfig
            ResultsBaseUrl = resultsBaseUrl
        }
        |> Some
