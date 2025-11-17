namespace BeerTaste.Common

open System
open Azure.Data.Tables
open Azure

type Beer = {
    Id: int
    Name: string
    BeerType: string
    Origin: string
    Producer: string
    ABV: float
    Volume: float
    Price: float
    Packaging: string
} with
    member this.PricePerLiter = this.Price / this.Volume
    member this.PricePerAbv = this.PricePerLiter / (this.ABV / 100.0)

module Beers =
    let beerToEntity (beerTasteGuid: string) (beer: Beer) =
        let entity = TableEntity(beerTasteGuid, beer.Id.ToString())
        entity.Add("Name", beer.Name)
        entity.Add("BeerType", beer.BeerType)
        entity.Add("Origin", beer.Origin)
        entity.Add("Producer", beer.Producer)
        entity.Add("ABV", beer.ABV)
        entity.Add("Volume", beer.Volume)
        entity.Add("Price", beer.Price)
        entity.Add("Packaging", beer.Packaging)
        entity

    let entityToBeer (entity: TableEntity) : Beer = {
        Id = int (entity :> ITableEntity).RowKey
        Name =
            entity.GetString("Name")
            |> Option.ofObj
            |> Option.get
        BeerType =
            entity.GetString("BeerType")
            |> Option.ofObj
            |> Option.get
        Origin =
            entity.GetString("Origin")
            |> Option.ofObj
            |> Option.get
        Producer =
            entity.GetString("Producer")
            |> Option.ofObj
            |> Option.get
        ABV =
            entity.GetDouble("ABV")
            |> Option.ofNullable
            |> Option.get
        Volume =
            entity.GetDouble("Volume")
            |> Option.ofNullable
            |> Option.get
        Price =
            entity.GetDouble("Price")
            |> Option.ofNullable
            |> Option.get
        Packaging =
            entity.GetString("Packaging")
            |> Option.ofObj
            |> Option.get
    }

    let fetchBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Beer list =
        try
            storage.BeersTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.map entityToBeer
            |> Seq.toList
        with _ -> []

    let deleteBeersForBeerTaste (beersTable: TableClient) (beerTasteGuid: string) : unit =
        try
            beersTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.iter (beersTable.DeleteEntity >> ignore)
        with _ ->
            ()

    let addBeers (beersTable: TableClient) (beerTasteGuid: string) (beers: Beer list) : unit =
        beers
        |> List.map (beerToEntity beerTasteGuid)
        |> List.iter (beersTable.AddEntity >> ignore)
