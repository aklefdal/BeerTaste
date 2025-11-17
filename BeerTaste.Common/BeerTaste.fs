namespace BeerTaste.Common

open System
open Azure.Data.Tables

type BeerTaste = {
    BeerTasteGuid: string
    ShortName: string
    Description: string
    Date: DateOnly
}

module BeerTasteStorage =
    let beertasteToEntity (beerTaste: BeerTaste) : TableEntity =
        let entity = TableEntity(beerTaste.BeerTasteGuid, beerTaste.ShortName)
        entity.Add("Description", beerTaste.Description)
        entity.Add("Date", beerTaste.Date.ToString("yyyy-MM-dd"))
        entity

    let getBeerTasteGuid (table: TableClient) (shortName: string) : string option =
        table.Query<TableEntity>(filter = $"RowKey eq '{shortName}'")
        |> Seq.map _.PartitionKey
        |> Seq.tryHead

    let addBeerTaste (table: TableClient) (shortName: string) (description: string) (date: DateOnly) =
        let beerTasteGuid = Guid.NewGuid().ToString()

        let beerTaste = {
            BeerTasteGuid = beerTasteGuid
            ShortName = shortName
            Description = description
            Date = date
        }

        let entity = beertasteToEntity beerTaste
        table.AddEntity(entity) |> ignore
        beerTasteGuid

    let getBeerTastePartitionKey (table: TableClient) (shortName: string) : string option =
        table.Query<TableEntity>(filter = $"RowKey eq '{shortName}'")
        |> Seq.tryHead
        |> Option.map _.PartitionKey
