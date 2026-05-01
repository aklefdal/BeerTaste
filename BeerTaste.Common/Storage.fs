namespace BeerTaste.Common

open System.Threading.Tasks
open Azure.Data.Tables

/// <summary>
/// Manages Azure Table Storage clients for BeerTaste application.
/// Creates and provides access to six tables: beertaste, beers, tasters, scores, users, sessions.
/// All tables are initialised concurrently to reduce startup latency.
/// </summary>
type BeerTasteTableStorage(connectionString: string) =
    let service = TableServiceClient(connectionString)
    let beerTasteTableClient = service.GetTableClient("beertaste")
    let beersTableClient = service.GetTableClient("beers")
    let tastersTableClient = service.GetTableClient("tasters")
    let scoresTableClient = service.GetTableClient("scores")
    let usersTableClient = service.GetTableClient("users")
    let sessionsTableClient = service.GetTableClient("sessions")

    // Run all six CreateIfNotExists calls concurrently instead of sequentially.
    // With ~50 ms per Azure round-trip this cuts startup overhead by ~5×.
    do
        [|
            beerTasteTableClient.CreateIfNotExistsAsync() :> Task
            beersTableClient.CreateIfNotExistsAsync() :> Task
            tastersTableClient.CreateIfNotExistsAsync() :> Task
            scoresTableClient.CreateIfNotExistsAsync() :> Task
            usersTableClient.CreateIfNotExistsAsync() :> Task
            sessionsTableClient.CreateIfNotExistsAsync() :> Task
        |]
        |> Task.WhenAll
        |> fun t -> t.GetAwaiter().GetResult()

    member this.BeerTasteTableClient = beerTasteTableClient
    member this.BeersTableClient = beersTableClient
    member this.TastersTableClient = tastersTableClient
    member this.ScoresTableClient = scoresTableClient
    member this.UsersTableClient = usersTableClient
    member this.SessionsTableClient = sessionsTableClient

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
