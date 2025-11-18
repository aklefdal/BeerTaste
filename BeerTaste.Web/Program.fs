open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Oxpecker
open Oxpecker.ViewEngine
open Oxpecker.ViewEngine.Aria
open BeerTaste.Common
open BeerTaste.Web.Templates

type SecretsAnchor = class end

let notFound (s: string) : EndpointHandler = setStatusCode 404 >=> text s

let resultsIndex (beerTasteGuid: string) : EndpointHandler =
    ResultsIndex.view beerTasteGuid |> htmlView

let bestBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid
    let results = Results.beerAverages beers scores
    BestBeers.view beerTasteGuid results |> htmlView

let controversial (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid
    let results = Results.beerStandardDeviations beers scores

    Controversial.view beerTasteGuid results
    |> htmlView

let deviant (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    let tasters = Tasters.fetchTasters storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid
    let results = Results.correlationToAverages beers tasters scores
    Deviant.view beerTasteGuid results |> htmlView

let similar (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let tasters = Tasters.fetchTasters storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid
    let results = Results.correlationBetweenTasters tasters scores
    Similar.view beerTasteGuid results |> htmlView

let strongBeers (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    let tasters = Tasters.fetchTasters storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid
    let results = Results.correlationToAbv beers tasters scores
    StrongBeers.view beerTasteGuid results |> htmlView

let cheapAlcohol (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    let tasters = Tasters.fetchTasters storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid
    let results = Results.correlationToAbvPrice beers tasters scores

    CheapAlcohol.view beerTasteGuid results
    |> htmlView

let beersView (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    BeersView.view beerTasteGuid beers |> htmlView

let tastersView (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let tasters = Tasters.fetchTasters storage beerTasteGuid
    TastersView.view beerTasteGuid tasters |> htmlView

let scoresView (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    let beers = Beers.fetchBeers storage beerTasteGuid
    let tasters = Tasters.fetchTasters storage beerTasteGuid
    let scores = Scores.fetchScores storage beerTasteGuid

    ScoresView.view beerTasteGuid beers tasters scores
    |> htmlView

let beerTasteView (storage: BeerTasteTableStorage) (beerTasteGuid: string) : EndpointHandler =
    match BeerTasteStorage.fetchBeerTaste storage beerTasteGuid with
    | Some beerTaste -> BeerTasteView.view beerTaste |> htmlView
    | None -> "BeerTaste not found" |> notFound


let endpoints storage = [
    GET [
        route "/"
        <| text "Beer Tasting Results - Navigate to /results/{beerTasteGuid}"
        routef "/{%s}/results" resultsIndex
        routef "/{%s}/results/bestbeers" (bestBeers storage)
        routef "/{%s}/results/controversial" (controversial storage)
        routef "/{%s}/results/deviant" (deviant storage)
        routef "/{%s}/results/strongbeers" (strongBeers storage)
        routef "/{%s}/results/similar" (similar storage)
        routef "/{%s}/results/cheapalcohol" (cheapAlcohol storage)
        routef "/{%s}/beers" (beersView storage)
        routef "/{%s}/tasters" (tastersView storage)
        routef "/{%s}/scores" (scoresView storage)
        routef "/{%s}" (beerTasteView storage)
    ]
]

let errorView errorCode (errorText: string) =
    html () {
        body (style = "width: 800px; margin: 0 auto") {
            h1 (style = "text-align: center; color: red") { raw $"Error <i>%d{errorCode}</i>" }
            p(ariaErrorMessage = "err1").on ("click", "console.log('clicked on error')") { errorText }
        }
    }

let notFoundHandler (ctx: HttpContext) =
    let logger = ctx.GetLogger()
    logger.LogWarning("Unhandled 404 error")
    ctx.SetStatusCode 404
    ctx.WriteHtmlView(errorView 404 "Page not found!")

let errorHandler (ctx: HttpContext) (next: RequestDelegate) : Task =
    task {
        try
            return! next.Invoke(ctx)
        with
        | :? ModelBindException
        | :? RouteParseException as ex ->
            let logger = ctx.GetLogger()
            logger.LogWarning(ex, "Unhandled 400 error")
            ctx.SetStatusCode StatusCodes.Status400BadRequest
            return! ctx.WriteHtmlView(errorView 400 (string ex))
        | ex ->
            let logger = ctx.GetLogger()
            logger.LogError(ex, "Unhandled 500 error")
            ctx.SetStatusCode StatusCodes.Status500InternalServerError
            return! ctx.WriteHtmlView(errorView 500 (string ex))
    }

let configureApp (appBuilder: WebApplication) storage =
    appBuilder.Use(errorHandler).UseRouting().UseOxpecker(endpoints storage)
    |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let config = builder.Configuration.AddUserSecrets<SecretsAnchor>().AddEnvironmentVariables().Build()

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

        1
    | Some connStr ->
        let storage = BeerTasteTableStorage(connStr)
        configureApp app storage
        app.Run()
        0
