open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Caching.Memory
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

/// Cache Azure Table Storage data per beerTasteGuid.
/// Tasting event data is immutable during an event, so caching avoids redundant
/// Azure round-trips when a user navigates between result pages.
type DataCache(storage: BeerTasteTableStorage, cache: IMemoryCache) =
    let ttl = TimeSpan.FromMinutes(10.0)

    // Use DateTimeOffset absolute expiration so entries expire 10 min after creation,
    // not after last access (which the TimeSpan overload treats as sliding expiration).
    let getOrCreate (key: string) (fetch: unit -> 'T) : 'T =
        match cache.TryGetValue(key) with
        | true, (:? 'T as value) -> value
        | _ ->
            let result = fetch ()
            cache.Set(key, result, DateTimeOffset.UtcNow.Add(ttl))

    member _.FetchBeers(beerTasteGuid: string) =
        getOrCreate $"beers:{beerTasteGuid}" (fun () -> Beers.fetchBeers storage beerTasteGuid)

    member _.FetchTasters(beerTasteGuid: string) =
        getOrCreate $"tasters:{beerTasteGuid}" (fun () -> Tasters.fetchTasters storage beerTasteGuid)

    member _.FetchScores(beerTasteGuid: string) =
        getOrCreate $"scores:{beerTasteGuid}" (fun () -> Scores.fetchScores storage beerTasteGuid)

    member _.FetchBeerTaste(beerTasteGuid: string) =
        getOrCreate $"beertaste:{beerTasteGuid}" (fun () -> BeerTasteStorage.fetchBeerTaste storage beerTasteGuid)

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

let bestBeers (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.beerAverages beers scores

        BestBeers.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let controversial (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.beerStandardDeviations beers scores

        Controversial.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let deviant (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAverages beers tasters scores

        Deviant.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let similar (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationBetweenTasters tasters scores

        Similar.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let strongBeers (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAbv beers tasters scores

        StrongBeers.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let cheapAlcohol (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAbvPrice beers tasters scores

        CheapAlcohol.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let oldManBeers (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAge beers tasters scores

        OldManBeers.view beerTasteGuid language firebaseConfig results
        |> htmlView
        <| ctx

let beersView (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid

        BeersView.view beerTasteGuid language firebaseConfig beers
        |> htmlView
        <| ctx

let tastersView (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let tasters = dc.FetchTasters beerTasteGuid

        TastersView.view beerTasteGuid language firebaseConfig tasters
        |> htmlView
        <| ctx

let scoresView (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid

        ScoresView.view beerTasteGuid language firebaseConfig beers tasters scores
        |> htmlView
        <| ctx

let beerTasteView (firebaseConfig: FirebaseConfig option) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let dc = ctx.RequestServices.GetRequiredService<DataCache>()
        let language = getLanguage ctx

        match dc.FetchBeerTaste beerTasteGuid with
        | Some beerTaste ->
            BeerTasteView.view beerTaste language firebaseConfig
            |> htmlView
            <| ctx
        | None -> "BeerTaste not found" |> notFound <| ctx


let endpoints firebaseConfig = [
    GET [
        route "/" <| homepage firebaseConfig
        routef "/{%s}/results" (resultsIndex firebaseConfig)
        routef "/{%s}/results/bestbeers" (bestBeers firebaseConfig)
        routef "/{%s}/results/controversial" (controversial firebaseConfig)
        routef "/{%s}/results/deviant" (deviant firebaseConfig)
        routef "/{%s}/results/strongbeers" (strongBeers firebaseConfig)
        routef "/{%s}/results/similar" (similar firebaseConfig)
        routef "/{%s}/results/cheapalcohol" (cheapAlcohol firebaseConfig)
        routef "/{%s}/results/oldmanbeers" (oldManBeers firebaseConfig)
        routef "/{%s}/beers" (beersView firebaseConfig)
        routef "/{%s}/tasters" (tastersView firebaseConfig)
        routef "/{%s}/scores" (scoresView firebaseConfig)
        routef "/{%s}" (beerTasteView firebaseConfig)
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

let configureApp (appBuilder: WebApplication) firebaseConfig =
    appBuilder.Use(errorHandler).UseStaticFiles().UseRouting().UseOxpecker(endpoints firebaseConfig)
    |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let config = builder.Configuration.AddUserSecrets<SecretsAnchor>().AddEnvironmentVariables().Build()

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
        builder.Services
            .AddRouting()
            .AddOxpecker()
            .AddMemoryCache()
            .AddSingleton<BeerTasteTableStorage>(fun _ -> BeerTasteTableStorage(connStr))
            .AddSingleton<DataCache>()
        |> ignore

        let app = builder.Build()
        let firebaseConfig = getFirebaseConfig config
        configureApp app firebaseConfig
        app.Run()
        0
