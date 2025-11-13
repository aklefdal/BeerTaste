namespace BeerTaste.Common

open System
open Azure.Data.Tables
open Azure

type BeerTasteEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Description = "" with get, set
    member val Date = DateTime.MinValue with get, set

    new(beerTasteGuid: string, rowKey: string, description: string, date: DateTime) as this =
        BeerTasteEntity()

        then
            (this :> ITableEntity).PartitionKey <- beerTasteGuid
            (this :> ITableEntity).RowKey <- rowKey
            this.Description <- description
            this.Date <- date

module BeerTasteStorage =
    let getBeerTasteGuid (table: TableClient) (shortName: string) : string option =
        table.Query<TableEntity>(filter = $"RowKey eq '{shortName}'")
        |> Seq.map _.PartitionKey
        |> Seq.tryHead

    let addBeerTaste (table: TableClient) (shortName: string) (description: string) (date: DateTime) =
        let beerTasteGuid = Guid.NewGuid().ToString()
        // Azure Table Storage requires UTC dates
        let utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc)
        let entity = BeerTasteEntity(beerTasteGuid, shortName, description, utcDate)
        table.AddEntity(entity) |> ignore
        beerTasteGuid

    let getBeerTastePartitionKey (table: TableClient) (shortName: string) : string option =
        try
            let query = table.Query<BeerTasteEntity>(filter = $"RowKey eq '{shortName}'")
            let entity = query |> Seq.tryHead

            entity
            |> Option.map (fun e -> (e :> ITableEntity).PartitionKey)
        with _ ->
            None
