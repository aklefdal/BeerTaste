namespace BeerTaste.Common

open System.Threading.Tasks
open Azure.Data.Tables

type Taster = {
    Name: string
    Email: string option
    BirthYear: int option
}

module Tasters =
    let tasterToEntity (beerTasteGuid: string) (taster: Taster) : TableEntity =
        let entity = TableEntity(beerTasteGuid, taster.Name)
        entity.Add("Email", taster.Email |> Option.toObj)
        entity.Add("BirthYear", taster.BirthYear |> Option.toNullable)
        entity

    let entityToTaster (entity: TableEntity) : Taster = {
        Name = entity.RowKey
        Email = entity.GetString("Email") |> Option.ofObj
        BirthYear = entity.GetInt32("BirthYear") |> Option.ofNullable
    }

    let deleteTastersForPartitionKeyAsync (tastersTable: TableClient) (beerTasteGuid: string) : Task =
        task {
            let deleteTasks =
                tastersTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
                |> Seq.map (fun e -> tastersTable.DeleteEntityAsync(e.PartitionKey, e.RowKey) :> Task)
                |> Seq.toArray

            do! Task.WhenAll(deleteTasks)
        }

    let deleteTastersForPartitionKey (tastersTable: TableClient) (beerTasteGuid: string) : unit =
        (deleteTastersForPartitionKeyAsync tastersTable beerTasteGuid).GetAwaiter().GetResult()

    let addTastersAsync (tastersTable: TableClient) (beerTasteGuid: string) (tasters: Taster list) : Task =
        task {
            let entities = tasters |> List.map (tasterToEntity beerTasteGuid)

            // Azure Table Storage supports up to 100 entities per batch transaction
            let batches = entities |> List.chunkBySize 100

            for batch in batches do
                let actions =
                    batch
                    |> List.map (fun entity -> TableTransactionAction(TableTransactionActionType.Add, entity))

                let! _ = tastersTable.SubmitTransactionAsync(actions)
                ()
        }

    let addTasters (tastersTable: TableClient) (beerTasteGuid: string) (tasters: Taster list) : unit =
        (addTastersAsync tastersTable beerTasteGuid tasters).GetAwaiter().GetResult()

    let fetchTasters (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Taster list =
        try
            storage.TastersTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.map entityToTaster
            |> Seq.toList
            |> List.sortBy _.Name
        with _ -> []
