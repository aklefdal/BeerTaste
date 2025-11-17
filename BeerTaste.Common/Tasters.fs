namespace BeerTaste.Common

open Azure.Data.Tables

type Taster = {
    Name: string
    Email: string option
    BirthYear: int option
}

module Tasters =
    let tasterToEntity (beerTasteGuid: string) (taster: Taster) : TableEntity =
        let entity = TableEntity(beerTasteGuid, taster.Name)
        entity.Add("Email", taster.Email)
        entity.Add("BirthYear", taster.BirthYear)
        entity

    let entityToTaster (entity: TableEntity) : Taster = {
        Name = entity.RowKey
        Email = entity.GetString("Email") |> Option.ofObj
        BirthYear = entity.GetInt32("BirthYear") |> Option.ofNullable
    }

    let deleteTastersForPartitionKey (tastersTable: TableClient) (beerTasteGuid: string) : unit =
        try
            tastersTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.iter (tastersTable.DeleteEntity >> ignore)
        with _ ->
            ()

    let addTasters (tastersTable: TableClient) (beerTasteGuid: string) (tasters: Taster list) : unit =
        tasters
        |> List.iter (fun taster ->
            let entity = taster |> tasterToEntity beerTasteGuid
            tastersTable.AddEntity(entity) |> ignore)

    let fetchTasters (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Taster list =
        try
            storage.TastersTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.map entityToTaster
            |> Seq.toList
            |> List.sortBy _.Name
        with _ -> []
