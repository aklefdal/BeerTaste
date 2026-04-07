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

    let getOrCreate (key: string) (fetch: unit -> 'T) : 'T =
        match cache.TryGetValue(key) with
        | true, (:? 'T as value) -> value
        | _ ->
            let result = fetch ()
            use entry = cache.CreateEntry(key)
            entry.AbsoluteExpirationRelativeToNow <- ttl
            entry.Value <- result
            result

    member _.FetchBeers(beerTasteGuid: string) =
        getOrCreate $"beers:{beerTasteGuid}" (fun () -> Beers.fetchBeers storage beerTasteGuid)

    member _.FetchTasters(beerTasteGuid: string) =
        getOrCreate $"tasters:{beerTasteGuid}" (fun () -> Tasters.fetchTasters storage beerTasteGuid)

    member _.FetchScores(beerTasteGuid: string) =
        getOrCreate $"scores:{beerTasteGuid}" (fun () -> Scores.fetchScores storage beerTasteGuid)

    member _.FetchBeerTaste(beerTasteGuid: string) =
        let key = $"beertaste:{beerTasteGuid}"
        match cache.TryGetValue(key) with
        | true, (:? BeerTaste as beerTaste) -> Some beerTaste
        | _ ->
             let result = BeerTasteStorage.fetchBeerTaste storage beerTasteGuid
             match result with
             | Some beerTaste ->
                 cache.Set(key, beerTaste, ttl) |> Some
             | None -> None

let notFound (s: string) : EndpointHandler = setStatusCode 404 >=> text s

let homepage: EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx

        Homepage.view language |> htmlView <| ctx

let resultsIndex (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx

        ResultsIndex.view beerTasteGuid language
        |> htmlView
        <| ctx

let bestBeers (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.beerAverages beers scores

        BestBeers.view beerTasteGuid language results
        |> htmlView
        <| ctx

let controversial (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.beerStandardDeviations beers scores

        Controversial.view beerTasteGuid language results
        |> htmlView
        <| ctx

let deviant (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAverages beers tasters scores

        Deviant.view beerTasteGuid language results
        |> htmlView
        <| ctx

let similar (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationBetweenTasters tasters scores

        Similar.view beerTasteGuid language results
        |> htmlView
        <| ctx

let strongBeers (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAbv beers tasters scores

        StrongBeers.view beerTasteGuid language results
        |> htmlView
        <| ctx

let cheapAlcohol (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAbvPrice beers tasters scores

        CheapAlcohol.view beerTasteGuid language results
        |> htmlView
        <| ctx

let oldManBeers (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid
        let results = Results.correlationToAge beers tasters scores

        OldManBeers.view beerTasteGuid language results
        |> htmlView
        <| ctx

let beersView (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid

        BeersView.view beerTasteGuid language beers
        |> htmlView
        <| ctx

let tastersView (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let tasters = dc.FetchTasters beerTasteGuid

        TastersView.view beerTasteGuid language tasters
        |> htmlView
        <| ctx

let scoresView (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx
        let beers = dc.FetchBeers beerTasteGuid
        let tasters = dc.FetchTasters beerTasteGuid
        let scores = dc.FetchScores beerTasteGuid

        ScoresView.view beerTasteGuid language beers tasters scores
        |> htmlView
        <| ctx

let beerTasteView (dc: DataCache) (beerTasteGuid: string) : EndpointHandler =
    fun ctx ->
        let language = getLanguage ctx

        match dc.FetchBeerTaste beerTasteGuid with
        | Some beerTaste ->
            BeerTasteView.view beerTaste language |> htmlView
            <| ctx
        | None -> "BeerTaste not found" |> notFound <| ctx


let endpoints (dc: DataCache) = [
    GET [
        route "/" <| homepage
        routef "/{%s}/results" resultsIndex
        routef "/{%s}/results/bestbeers" (bestBeers dc)
        routef "/{%s}/results/controversial" (controversial dc)
        routef "/{%s}/results/deviant" (deviant dc)
        routef "/{%s}/results/strongbeers" (strongBeers dc)
        routef "/{%s}/results/similar" (similar dc)
        routef "/{%s}/results/cheapalcohol" (cheapAlcohol dc)
        routef "/{%s}/results/oldmanbeers" (oldManBeers dc)
        routef "/{%s}/beers" (beersView dc)
        routef "/{%s}/tasters" (tastersView dc)
        routef "/{%s}/scores" (scoresView dc)
        routef "/{%s}" (beerTasteView dc)
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

let configureApp (appBuilder: WebApplication) (dc: DataCache) =
    appBuilder.Use(errorHandler).UseStaticFiles().UseRouting().UseOxpecker(endpoints dc)
    |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let config = builder.Configuration.AddUserSecrets<SecretsAnchor>().AddEnvironmentVariables().Build()

    builder.Services.AddRouting().AddOxpecker().AddMemoryCache()
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
        let cache = app.Services.GetRequiredService<IMemoryCache>()
        let dc = DataCache(storage, cache)
        configureApp app dc
        app.Run()
        0
