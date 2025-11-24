namespace BeerTaste.Common

open Azure.Data.Tables

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

    let getNonNullString (e: TableEntity) (propertyName: string) : string =
        match e.GetString(propertyName) |> Option.ofObj with
        | Some value -> value
        | None -> failwithf $"Property %s{propertyName} is null"

    let getNonNullFloat (e: TableEntity) (propertyName: string) : float =
        match e.GetDouble(propertyName) |> Option.ofNullable with
        | Some value -> value
        | None -> failwithf $"Property %s{propertyName} is null"

    let entityToBeer (entity: TableEntity) : Beer = {
        Id = int (entity :> ITableEntity).RowKey
        Name = getNonNullString entity "Name"
        BeerType = getNonNullString entity "BeerType"
        Origin = getNonNullString entity "Origin"
        Producer = getNonNullString entity "Producer"
        ABV = getNonNullFloat entity "ABV"
        Volume = getNonNullFloat entity "Volume"
        Price = getNonNullFloat entity "Price"
        Packaging = getNonNullString entity "Packaging"
    }

    let fetchBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Beer list =
        try
            storage.BeersTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.map entityToBeer
            |> Seq.toList
            |> List.sortBy _.Id
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
