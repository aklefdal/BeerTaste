module BeerTaste.Console.Storage

open Spectre.Console
open Azure.Data.Tables

type BeerTasteTableStorage (connectionString: string) =
    do AnsiConsole.MarkupLine("[grey]Connecting to Azure Table Storage...[/]")
    let service = TableServiceClient(connectionString)
    let beerTasteTableName = "beertaste"
    let beerTasteTableClient = service.GetTableClient(beerTasteTableName)
    do beerTasteTableClient.CreateIfNotExists() |> ignore

    let beersTableName = "beers"
    let beersTableClient = service.GetTableClient(beersTableName)
    do beersTableClient.CreateIfNotExists() |> ignore

    let tastersTableName = "tasters"
    let tastersTableClient = service.GetTableClient(tastersTableName)
    do tastersTableClient.CreateIfNotExists() |> ignore

    do AnsiConsole.MarkupLine($"[green]Connected to Table Storage.[/] Tables [bold]{beerTasteTableName}[/], [bold]{beersTableName}[/], and [bold]{tastersTableName}[/] ready.")

    member this.BeerTasteTableClient = beerTasteTableClient
    member this.BeersTableClient = beersTableClient
    member this.TastersTableClient = tastersTableClient
