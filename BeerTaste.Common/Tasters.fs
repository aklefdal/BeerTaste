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

    let deleteTastersForPartitionKey (tastersTable: TableClient) (beerTasteGuid: string) : Task =
        task {
            let deleteTasks =
                tastersTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
                |> Seq.map (fun e -> tastersTable.DeleteEntityAsync(e.PartitionKey, e.RowKey) :> Task)
                |> Seq.toArray

            do! Task.WhenAll(deleteTasks)
        }

    let addTasters (tastersTable: TableClient) (beerTasteGuid: string) (tasters: Taster list) : Task =
        tasters
        |> List.map (tasterToEntity beerTasteGuid)
        |> Storage.addEntitiesBatch tastersTable

    let fetchTasters (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Taster list =
        storage.TastersTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
        |> Seq.map entityToTaster
        |> Seq.toList
        |> List.sortBy _.Name
