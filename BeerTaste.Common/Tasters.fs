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

    member this.Name = (this :> ITableEntity).RowKey
    member val Email = "" with get, set
    member val BirthYear = 0 with get, set

    new(beerTasteGuid: string, taster: Taster) as this =
        TasterEntity()
        then
            (this :> ITableEntity).PartitionKey <- beerTasteGuid
            (this :> ITableEntity).RowKey <- taster.Name
            this.Email <- taster.Email
            this.BirthYear <- taster.BirthYear

module TastersStorage =
    let deleteTastersForPartitionKey (tastersTable: TableClient) (beerTasteGuid: string) : unit =
        try
            tastersTable.Query<TasterEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.iter (tastersTable.DeleteEntity >> ignore)
        with
        | _ -> ()

    let addTasters (tastersTable: TableClient) (beerTasteGuid: string) (tasters: Taster list) : unit =
        tasters
        |> List.iter (fun taster ->
            let entity = TasterEntity(beerTasteGuid, taster)
            tastersTable.AddEntity(entity) |> ignore)
