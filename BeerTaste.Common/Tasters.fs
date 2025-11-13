namespace BeerTaste.Common

open System
open Azure.Data.Tables
open Azure

type Taster = {
    Name: string
    Email: string
    BirthYear: int
}

type TasterEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Name = "" with get, set
    member val Email = "" with get, set
    member val BirthYear = 0 with get, set

    new(partitionKey: string, rowKey: string, taster: Taster) as this =
        TasterEntity()
        then
            (this :> ITableEntity).PartitionKey <- partitionKey
            (this :> ITableEntity).RowKey <- rowKey
            this.Name <- taster.Name
            this.Email <- taster.Email
            this.BirthYear <- taster.BirthYear

module TastersStorage =
    let deleteTastersForPartitionKey (tastersTable: TableClient) (partitionKey: string) : unit =
        try
            tastersTable.Query<TasterEntity>(filter = $"PartitionKey eq '{partitionKey}'")
            |> Seq.map tastersTable.DeleteEntity
            |> ignore
        with
        | _ -> ()

    let addTasters (tastersTable: TableClient) (partitionKey: string) (tasters: Taster list) : unit =
        tasters
        |> List.iter (fun taster ->
            let rowKey = taster.Name
            let entity = TasterEntity(partitionKey, rowKey, taster)
            tastersTable.AddEntity(entity) |> ignore)
