namespace BeerTaste.Common

open System.Threading.Tasks
open Azure.Data.Tables

/// <summary>
/// Represents a beer with all its properties including pricing and alcohol content.
/// Includes computed properties for price per liter and price per ABV unit.
/// </summary>
type Beer = {
    /// Unique identifier for the beer within a tasting event
    Id: int
    /// Name of the beer
    Name: string
    /// Type/style of beer (e.g., IPA, Stout, Lager)
    BeerType: string
    /// Country or region of origin
    Origin: string
    /// Brewery or producer name
    Producer: string
    /// Alcohol by volume as a percentage (e.g., 5.5 for 5.5%)
    ABV: float
    /// Volume in liters
    Volume: float
    /// Price in local currency
    Price: float
    /// Packaging type (e.g., Bottle, Can)
    Packaging: string
} with
    /// Calculated price per liter
    member this.PricePerLiter = this.Price / this.Volume
    /// Calculated price per ABV unit (value for money metric)
    member this.PricePerAbv = this.PricePerLiter / (this.ABV / 100.0)

/// <summary>
/// Azure Table Storage operations for beer data.
/// Handles conversion between Beer domain type and TableEntity, and provides CRUD operations.
/// </summary>
module Beers =
    /// <summary>Converts a Beer domain object to an Azure TableEntity for storage.</summary>
    /// <param name="beerTasteGuid">The partition key (BeerTaste event GUID as string)</param>
    /// <param name="beer">The Beer to convert</param>
    /// <returns>A TableEntity ready for Azure Table Storage</returns>
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
        storage.BeersTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
        |> Seq.map entityToBeer
        |> Seq.toList
        |> List.sortBy _.Id

    let deleteBeersForBeerTaste (beersTable: TableClient) (beerTasteGuid: string) : Task =
        task {
            let deleteTasks =
                beersTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
                |> Seq.map (fun e -> beersTable.DeleteEntityAsync(e.PartitionKey, e.RowKey) :> Task)
                |> Seq.toArray

            do! Task.WhenAll(deleteTasks)
        }

    let addBeers (beersTable: TableClient) (beerTasteGuid: string) (beers: Beer list) : Task =
        task {
            let entities = beers |> List.map (beerToEntity beerTasteGuid)

            // Azure Table Storage supports up to 100 entities per batch transaction
            let batches = entities |> List.chunkBySize 100

            for batch in batches do
                let actions =
                    batch
                    |> List.map (fun entity -> TableTransactionAction(TableTransactionActionType.Add, entity))

                let! _ = beersTable.SubmitTransactionAsync(actions)
                ()
        }
