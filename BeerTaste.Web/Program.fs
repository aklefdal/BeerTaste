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
open BeerTaste.Web.Localization

type SecretsAnchor = class end

let notFound (s: string) : EndpointHandler = setStatusCode 404 >=> text s

// Read Firebase configuration from IConfiguration
let getFirebaseConfig (config: IConfiguration) : FirebaseConfig option =
    let apiKey =
        config["BeerTaste:Firebase:ApiKey"]
        |> Option.ofObj

    let authDomain =
        config["BeerTaste:Firebase:AuthDomain"]
        |> Option.ofObj

    let projectId =
        config["BeerTaste:Firebase:ProjectId"]
        |> Option.ofObj

    match apiKey, authDomain, projectId with
    | Some ak, Some ad, Some pid ->
        Some {
            ApiKey = ak
            AuthDomain = ad
            ProjectId = pid
        }
    | _ -> None

let homepage (firebaseConfig: FirebaseConfig option) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx

        Homepage.view language firebaseConfig |> htmlView
        <| ctx

let resultsIndex (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx

        ResultsIndex.view beerTasteGuid language firebaseConfig
        |> htmlView
        <| ctx

let bestBeers
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.beerAverages beers scores

        BestBeers.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let controversial
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.beerStandardDeviations beers scores

        Controversial.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let deviant
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.correlationToAverages beers tasters scores

        Deviant.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let similar
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.correlationBetweenTasters tasters scores

        Similar.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let strongBeers
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.correlationToAbv beers tasters scores

        StrongBeers.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let cheapAlcohol
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.correlationToAbvPrice beers tasters scores

        CheapAlcohol.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let oldManBeers
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid
        let results = Results.correlationToAge beers tasters scores

        OldManBeers.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let beersView
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid

        BeersView.view beerTasteGuid language firebaseConfig beers
        |> htmlView
        <| ctx

let tastersView
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let tasters = Tasters.fetchTasters storage beerTasteGuid

        TastersView.view beerTasteGuid language firebaseConfig tasters
        |> htmlView
        <| ctx

let scoresView
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = Beers.fetchBeers storage beerTasteGuid
        let tasters = Tasters.fetchTasters storage beerTasteGuid
        let scores = Scores.fetchScores storage beerTasteGuid

        ScoresView.view beerTasteGuid language firebaseConfig beers tasters scores
        |> htmlView
        <| ctx

let beerTasteView
    (storage: BeerTasteTableStorage)
    (firebaseConfig: FirebaseConfig option)
    (beerTasteGuid: string)
    : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx

        match BeerTasteStorage.fetchBeerTaste storage beerTasteGuid with
        | Some beerTaste ->
            BeerTasteView.view beerTaste language firebaseConfig
            |> htmlView
            <| ctx
        | None -> "BeerTaste not found" |> notFound <| ctx


let endpoints storage firebaseConfig = [
    GET [
        route "/" <| homepage firebaseConfig
        routef "/{%s}/results" (resultsIndex firebaseConfig)
        routef "/{%s}/results/bestbeers" (bestBeers storage firebaseConfig)
        routef "/{%s}/results/controversial" (controversial storage firebaseConfig)
        routef "/{%s}/results/deviant" (deviant storage firebaseConfig)
        routef "/{%s}/results/strongbeers" (strongBeers storage firebaseConfig)
        routef "/{%s}/results/similar" (similar storage firebaseConfig)
        routef "/{%s}/results/cheapalcohol" (cheapAlcohol storage firebaseConfig)
        routef "/{%s}/results/oldmanbeers" (oldManBeers storage firebaseConfig)
        routef "/{%s}/beers" (beersView storage firebaseConfig)
        routef "/{%s}/tasters" (tastersView storage firebaseConfig)
        routef "/{%s}/scores" (scoresView storage firebaseConfig)
        routef "/{%s}" (beerTasteView storage firebaseConfig)
    ]
]

let errorView errorCode (errorText: string) (language: Language) =
    let t = getTranslations language

    html () {
        body (style = "width: 800px; margin: 0 auto") {
            h1 (style = "text-align: center; color: red") { raw $"{t.Error} <i>%d{errorCode}</i>" }
            p(ariaErrorMessage = "err1").on ("click", "console.log('clicked on error')") { errorText }
        }
    }

let notFoundHandler (ctx: HttpContext) =
    let language = getLanguage ctx
    let t = getTranslations language
    let logger = ctx.GetLogger()
    logger.LogWarning("Unhandled 404 error")
    ctx.SetStatusCode 404
    ctx.WriteHtmlView(errorView 404 t.PageNotFound language)

let errorHandler (ctx: HttpContext) (next: RequestDelegate) : Task =
    task {
        try
            return! next.Invoke(ctx)
        with
        | :? ModelBindException
        | :? RouteParseException as ex ->
            let language = getLanguage ctx
            let logger = ctx.GetLogger()
            logger.LogWarning(ex, "Unhandled 400 error")
            ctx.SetStatusCode StatusCodes.Status400BadRequest
            return! ctx.WriteHtmlView(errorView 400 (string ex) language)
        | ex ->
            let language = getLanguage ctx
            let logger = ctx.GetLogger()
            logger.LogError(ex, "Unhandled 500 error")
            ctx.SetStatusCode StatusCodes.Status500InternalServerError
            return! ctx.WriteHtmlView(errorView 500 (string ex) language)
    }

let configureApp (appBuilder: WebApplication) storage firebaseConfig =
    appBuilder.Use(errorHandler).UseStaticFiles().UseRouting().UseOxpecker(endpoints storage firebaseConfig)
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
        let firebaseConfig = getFirebaseConfig config
        configureApp app storage firebaseConfig
        app.Run()
        0
