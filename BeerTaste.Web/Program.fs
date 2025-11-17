open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Oxpecker
open Azure.Data.Tables
open BeerTaste.Common
open BeerTaste.Web.Templates

type SecretsAnchor = class end

let beerEntityToBeer (entity: BeerEntity) : Beer = {
    Id = int (entity :> ITableEntity).RowKey
    Name = entity.Name
    BeerType = entity.BeerType
    Origin = entity.Origin
    Producer = entity.Producer
    ABV = entity.ABV
    Volume = entity.Volume
    Price = entity.Price
    Packaging = entity.Packaging
}

let fetchBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Beer list =
    try
        storage.BeersTableClient.Query<BeerEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
        |> Seq.map beerEntityToBeer
        |> Seq.toList
    with _ -> []


let fetchScores (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Score list =
    try
        storage.ScoresTableClient.Query<ScoreEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
        |> Seq.map Scores.scoreEntityToScore
        |> Seq.toList
    with _ -> []

let resultsIndex (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let html = ResultsIndex.view (beerTasteGuid.ToString())
        htmlView html ctx

let bestBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let beers = fetchBeers storage beerTasteGuid
        let scores = fetchScores storage beerTasteGuid
        let results = Results.beerAverages beers scores
        let html = BestBeers.view beerTasteGuid results
        htmlView html ctx

let controversial (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let beers = fetchBeers storage beerTasteGuid
        let scores = fetchScores storage beerTasteGuid
        let results = Results.beerStandardDeviations beers scores
        let html = Controversial.view beerTasteGuid results
        htmlView html ctx

let deviant (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let beers = fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = fetchScores storage beerTasteGuid
        let results = Results.correlationToAverages beers tasters scores
        let html = Deviant.view beerTasteGuid results
        htmlView html ctx

let similar (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let beers = fetchBeers storage beerTasteGuid
        let scores = fetchScores storage beerTasteGuid
        let results = Results.correlationBetweenTasters tasters beers scores
        let html = Similar.view beerTasteGuid results
        htmlView html ctx

let strongBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let beers = fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = fetchScores storage beerTasteGuid
        let results = Results.correlationToAbv beers tasters scores
        let html = StrongBeers.view beerTasteGuid results
        htmlView html ctx

let cheapAlcohol (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let beers = fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = fetchScores storage beerTasteGuid
        let results = Results.correlationToAbvPrice beers tasters scores
        let html = CheapAlcohol.view beerTasteGuid results
        htmlView html ctx

let endpoints storage = [
    GET [
        route "/"
        <| text "Beer Tasting Results - Navigate to /results/{beerTasteGuid}"
        routef "/results/{%s}" <| resultsIndex
        routef "/results/{%s}/bestbeers"
        <| bestBeers storage
        routef "/results/{%s}/controversial"
        <| controversial storage
        routef "/results/{%s}/deviant" <| deviant storage
        routef "/results/{%s}/strongbeers"
        <| strongBeers storage
        routef "/results/{%s}/similar" <| similar storage
        routef "/results/{%s}/cheapalcohol"
        <| cheapAlcohol storage
    ]
]

let configureApp (appBuilder: WebApplication) storage =
    appBuilder.UseStaticFiles().UseRouting().UseOxpecker(endpoints storage)
    |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let config =
        builder.Configuration.AddUserSecrets<SecretsAnchor>().AddEnvironmentVariables().Build()

    builder.Services.AddRouting().AddOxpecker()
    |> ignore

    let app = builder.Build()

    match
        config["BeerTaste:TableStorageConnectionString"]
        |> Option.ofObj
    with
    | None ->
        printfn "Error: Missing connection string 'BeerTaste:TableStorageConnectionString' in configuration"

        printfn
            "Set it using: dotnet user-secrets set \"BeerTaste:TableStorageConnectionString\" \"<your-connection-string>\""

        1 // Exit with error
    | Some connStr ->
        // Initialize Azure Table Storage
        let storage = BeerTasteTableStorage(connStr)
        configureApp app storage
        app.Run()
        0
