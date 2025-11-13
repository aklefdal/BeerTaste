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
}

type BeerEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Name = "" with get, set
    member val BeerType = "" with get, set
    member val Origin = "" with get, set
    member val Producer = "" with get, set
    member val ABV = 0.0 with get, set
    member val Volume = 0.0 with get, set
    member val Price = 0.0 with get, set
    member val Packaging = "" with get, set

    new(beerTasteGuid: string, beer: Beer) as this =
        BeerEntity()
        then
            (this :> ITableEntity).PartitionKey <- beerTasteGuid
            (this :> ITableEntity).RowKey <- beer.Id.ToString()
            this.Name <- beer.Name
            this.BeerType <- beer.BeerType
            this.Origin <- beer.Origin
            this.Producer <- beer.Producer
            this.ABV <- beer.ABV
            this.Volume <- beer.Volume
            this.Price <- beer.Price
            this.Packaging <- beer.Packaging

module BeersStorage =
    let deleteBeersForBeerTaste (beersTable: TableClient) (beerTasteGuid: string) : unit =
        try
            beersTable.Query<BeerEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.iter (beersTable.DeleteEntity >> ignore)
        with
        | _ -> ()

    let addBeers (beersTable: TableClient) (beerTasteGuid: string) (beers: Beer list) : unit =
        beers
        |> List.iter (fun beer ->
            let entity = BeerEntity(beerTasteGuid, beer)
            beersTable.AddEntity(entity) |> ignore)
