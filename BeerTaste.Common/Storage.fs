namespace BeerTaste.Common

open Azure.Data.Tables

type BeerTasteTableStorage(connectionString: string) =
    let service = TableServiceClient(connectionString)
    let beerTasteTableName = "beertaste"
    let beerTasteTableClient = service.GetTableClient(beerTasteTableName)
    do beerTasteTableClient.CreateIfNotExists() |> ignore

    let beersTableName = "beers"
    let beersTableClient = service.GetTableClient(beersTableName)
    do beersTableClient.CreateIfNotExists() |> ignore

    let tastersTableName = "tasters"
    let tastersTableClient = service.GetTableClient(tastersTableName)
    do tastersTableClient.CreateIfNotExists() |> ignore

    let scoresTableName = "scores"
    let scoresTableClient = service.GetTableClient(scoresTableName)
    do scoresTableClient.CreateIfNotExists() |> ignore

    member this.BeerTasteTableClient = beerTasteTableClient
    member this.BeersTableClient = beersTableClient
    member this.TastersTableClient = tastersTableClient
    member this.ScoresTableClient = scoresTableClient
