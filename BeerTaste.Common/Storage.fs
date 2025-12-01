namespace BeerTaste.Common

open System.Threading.Tasks
open Azure.Data.Tables

/// <summary>
/// Manages Azure Table Storage clients for BeerTaste application.
/// Creates and provides access to four tables: beertaste, beers, tasters, and scores.
/// All tables are created automatically if they don't exist.
/// </summary>
type BeerTasteTableStorage(connectionString: string) =
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

    let scoresTableName = "scores"
    let scoresTableClient = service.GetTableClient(scoresTableName)
    do scoresTableClient.CreateIfNotExists() |> ignore

    member this.BeerTasteTableClient = beerTasteTableClient
    member this.BeersTableClient = beersTableClient
    member this.TastersTableClient = tastersTableClient
    member this.ScoresTableClient = scoresTableClient

module Storage =
    let addEntitiesBatch (table: TableClient) (entities: TableEntity list) : Task =
        task {
            let batches = entities |> List.chunkBySize 100

            for batch in batches do
                let actions =
                    batch
                    |> List.map (fun e -> TableTransactionAction(TableTransactionActionType.Add, e))

                let! _ = table.SubmitTransactionAsync(actions)
                ()
        }
