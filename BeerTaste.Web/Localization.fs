module BeerTaste.Web.Localization

open System
open Microsoft.AspNetCore.Http

// Language discriminated union
type Language =
    | English
    | Norwegian

// Record type for all translatable strings
type Translations = {
    // Navigation
    Home: string
    Beers: string
    Tasters: string
    Scores: string
    Results: string
    BackToResults: string

    // Page titles
    BeerTastingResults: string
    AvailableResults: string
    BestBeers: string
    MostControversialBeers: string
    MostDeviantTasters: string
    MostSimilarTasters: string
    MostFondOfStrongBeers: string
    MostFondOfCheapAlcohol: string

    // Table headers - Common
    Rank: string
    Name: string
    Beer: string
    Taster: string

    // Table headers - Beers
    Id: string
    Type: string
    Origin: string
    Producer: string
    ABV: string
    Volume: string
    Price: string
    Packaging: string

    // Table headers - Tasters
    Email: string
    BirthYear: string

    // Table headers - Results
    AverageScore: string
    StandardDeviation: string
    CorrelationToAverage: string
    Taster1: string
    Taster2: string
    Correlation: string
    CorrelationToABV: string
    CorrelationToPricePerABV: string

    // Labels and actions
    Description: string
    Date: string
    Highlight: string
    None: string
    LanguageLabel: string

    // Error messages
    PageNotFound: string
    Error: string
}

// English translations
let englishTranslations: Translations = {
    // Navigation
    Home = "Home"
    Beers = "Beers"
    Tasters = "Tasters"
    Scores = "Scores"
    Results = "Results"
    BackToResults = "Back to Results"

    // Page titles
    BeerTastingResults = "Beer Tasting Results"
    AvailableResults = "Available Results"
    BestBeers = "Best Beers"
    MostControversialBeers = "Most Controversial Beers"
    MostDeviantTasters = "Most Deviant Tasters"
    MostSimilarTasters = "Most Similar Tasters"
    MostFondOfStrongBeers = "Most Fond of Strong Beers"
    MostFondOfCheapAlcohol = "Most Fond of Cheap Alcohol"

    // Table headers - Common
    Rank = "Rank"
    Name = "Name"
    Beer = "Beer"
    Taster = "Taster"

    // Table headers - Beers
    Id = "Id"
    Type = "Type"
    Origin = "Origin"
    Producer = "Producer"
    ABV = "ABV"
    Volume = "Volume"
    Price = "Price"
    Packaging = "Packaging"

    // Table headers - Tasters
    Email = "Email"
    BirthYear = "Birth Year"

    // Table headers - Results
    AverageScore = "Average Score"
    StandardDeviation = "Standard Deviation"
    CorrelationToAverage = "Correlation to Average"
    Taster1 = "Taster 1"
    Taster2 = "Taster 2"
    Correlation = "Correlation"
    CorrelationToABV = "Correlation to ABV"
    CorrelationToPricePerABV = "Correlation to Price per ABV"

    // Labels and actions
    Description = "Description"
    Date = "Date"
    Highlight = "Highlight"
    None = "None"
    LanguageLabel = "Language"

    // Error messages
    PageNotFound = "Page not found!"
    Error = "Error"
}

// Norwegian translations
let norwegianTranslations: Translations = {
    // Navigation
    Home = "Hjem"
    Beers = "Øl"
    Tasters = "Smakere"
    Scores = "Poeng"
    Results = "Resultater"
    BackToResults = "Tilbake til resultater"

    // Page titles
    BeerTastingResults = "Ølsmaking resultater"
    AvailableResults = "Tilgjengelige resultater"
    BestBeers = "Beste øl"
    MostControversialBeers = "Mest kontroversielle øl"
    MostDeviantTasters = "Mest avvikende smakere"
    MostSimilarTasters = "Mest like smakere"
    MostFondOfStrongBeers = "Mest glad i sterk øl"
    MostFondOfCheapAlcohol = "Mest glad i billig alkohol"

    // Table headers - Common
    Rank = "Rang"
    Name = "Navn"
    Beer = "Øl"
    Taster = "Smaker"

    // Table headers - Beers
    Id = "Id"
    Type = "Type"
    Origin = "Opprinnelse"
    Producer = "Produsent"
    ABV = "Alkohol %"
    Volume = "Volum"
    Price = "Pris"
    Packaging = "Emballasje"

    // Table headers - Tasters
    Email = "E-post"
    BirthYear = "Fødselsår"

    // Table headers - Results
    AverageScore = "Gjennomsnittscore"
    StandardDeviation = "Standardavvik"
    CorrelationToAverage = "Korrelasjon til gjennomsnitt"
    Taster1 = "Smaker 1"
    Taster2 = "Smaker 2"
    Correlation = "Korrelasjon"
    CorrelationToABV = "Korrelasjon til alkoholprosent"
    CorrelationToPricePerABV = "Korrelasjon til pris per alkoholprosent"

    // Labels and actions
    Description = "Beskrivelse"
    Date = "Dato"
    Highlight = "Fremhev"
    None = "Ingen"
    LanguageLabel = "Språk"

    // Error messages
    PageNotFound = "Siden ble ikke funnet!"
    Error = "Feil"
}

// Get translations for a given language
let getTranslations (language: Language) : Translations =
    match language with
    | English -> englishTranslations
    | Norwegian -> norwegianTranslations

// Language code to Language type conversion
let languageFromCode (code: string) : Language =
    match code.ToLower() with
    | "no"
    | "nb"
    | "nn" -> Norwegian
    | _ -> English

// Language to code conversion
let languageToCode (language: Language) : string =
    match language with
    | English -> "en"
    | Norwegian -> "no"

// Cookie name for language preference
let languageCookieName = "beertaste-language"

// Get language from cookie
let getLanguageFromCookie (ctx: HttpContext) : Language option =
    match ctx.Request.Cookies.TryGetValue(languageCookieName) with
    | true, value ->
        value
        |> Option.ofObj
        |> Option.map languageFromCode
    | _ -> None

// Get language from Accept-Language header
let getLanguageFromHeader (ctx: HttpContext) : Language option =
    match ctx.Request.Headers.TryGetValue("Accept-Language") with
    | true, values ->
        let acceptLanguage = values.ToString().ToLowerInvariant()

        let languages = acceptLanguage.Split([| ','; ';' |], StringSplitOptions.RemoveEmptyEntries)

        if
            languages
            |> Array.exists (fun lang ->
                let trimmed = lang.Trim()

                trimmed.StartsWith("no")
                || trimmed.StartsWith("nb")
                || trimmed.StartsWith("nn"))
        then
            Some Norwegian
        else
            None
    | false, _ -> None

// Get the current language for the request
let getLanguage (ctx: HttpContext) : Language =
    match getLanguageFromCookie ctx with
    | Some lang -> lang
    | None ->
        match getLanguageFromHeader ctx with
        | Some lang -> lang
        | None -> English // Default to English
