namespace BeerTaste.Common

open FSharp.Stats
open FSharp.Stats.Correlation

/// <summary>
/// Statistical analysis functions for beer tasting results.
/// Provides correlations, averages, standard deviations, and rankings.
/// </summary>
module Results =
    /// Result for a single beer with a calculated value (e.g., average score, standard deviation)
    type BeerResult = { Name: string; Value: float }

    /// Result for a beer including both a calculated value and the overall average
    type BeerResultWithAverage = {
        Name: string
        Value: float
        Average: float
    }

    /// Result for a single taster with a calculated value (e.g., correlation coefficient)
    type TasterResult = { Name: string; Value: float }

    /// Result for a pair of tasters with a calculated value (e.g., similarity correlation)
    type TasterPairResult = {
        Name1: string
        Name2: string
        Value: float
    }

    // Convert a pre-filtered Score list to a float array, sorted by beer ID
    let private toFloatArrayByBeerId (scores: Score list) : float array =
        scores
        |> List.sortBy _.BeerId
        |> List.map (fun s -> s.ScoreValue |> Option.defaultValue 0 |> float)
        |> List.toArray

    // Convert a pre-filtered Score list to a float array, sorted by taster name
    let private toFloatArrayByTasterName (scores: Score list) : float array =
        scores
        |> List.sortBy _.TasterName
        |> List.map (fun s -> s.ScoreValue |> Option.defaultValue 0 |> float)
        |> List.toArray

    // Look up scores for a beer from a pre-grouped map, returning a normalized float array
    let private lookupBeerScores (scoresByBeer: Map<int, Score list>) (beerId: int) : float array =
        scoresByBeer
        |> Map.tryFind beerId
        |> Option.defaultValue []
        |> toFloatArrayByTasterName

    // Look up scores for a taster from a pre-grouped map, returning a normalized float array
    let private lookupTasterScores (scoresByTaster: Map<string, Score list>) (tasterName: string) : float array =
        scoresByTaster
        |> Map.tryFind tasterName
        |> Option.defaultValue []
        |> toFloatArrayByBeerId

    let private averageOrZero (scores: float array) : float =
        if scores.Length > 0 then Array.average scores else 0.0

    // Get all scores for a specific taster, sorted by beer ID
    let getScoresForTaster (tasterName: string) (scores: Score list) : float array =
        scores
        |> List.filter (fun s -> s.TasterName = tasterName)
        |> toFloatArrayByBeerId

    // Get all scores for a specific beer
    let getScoresForBeer (scores: Score list) (beerId: int) : float array =
        scores
        |> List.filter (fun s -> s.BeerId = beerId)
        |> toFloatArrayByTasterName

    let getAverageScoreForBeer (scores: Score list) (beerId: int) : float =
        getScoresForBeer scores beerId |> averageOrZero

    let beerAverages (beers: Beer list) (scores: Score list) : BeerResult list =
        // Pre-group scores by beer ID to avoid a repeated full-list scan per beer
        let scoresByBeer = scores |> List.groupBy _.BeerId |> Map.ofList

        beers
        |> List.map (fun b ->
            let avg =
                lookupBeerScores scoresByBeer b.Id
                |> averageOrZero

            {
                Name = $"{b.Producer} - {b.Name}"
                Value = avg
            }
            : BeerResult)
        |> List.sortByDescending _.Value

    // Most controversial beers by standard deviation
    let beerStandardDeviations (beers: Beer list) (scores: Score list) : BeerResultWithAverage list =
        // Pre-group scores by beer ID to avoid a repeated full-list scan per beer
        let scoresByBeer = scores |> List.groupBy _.BeerId |> Map.ofList

        beers
        |> List.map (fun b ->
            let beerScores = lookupBeerScores scoresByBeer b.Id

            let stdDev, avg =
                if beerScores.Length > 0 then
                    Seq.stDevPopulation beerScores, Array.average beerScores
                else
                    0.0, 0.0

            ({
                Name = $"{b.Producer} - {b.Name}"
                Value = stdDev
                Average = avg
            }
            : BeerResultWithAverage))
        |> List.sortByDescending _.Value

    let correlationToAverages (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        // Pre-group scores by beer and taster to avoid repeated full-list scans
        let scoresByBeer = scores |> List.groupBy _.BeerId |> Map.ofList
        let scoresByTaster = scores |> List.groupBy _.TasterName |> Map.ofList

        let avgScoresByBeer =
            beers
            |> List.sortBy _.Id
            |> List.map (fun b ->
                lookupBeerScores scoresByBeer b.Id
                |> averageOrZero)

        tasters
        |> List.map (fun t ->
            let tasterScores = lookupTasterScores scoresByTaster t.Name

            let correlation = Seq.pearson tasterScores avgScoresByBeer
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortBy _.Value

    // Generate all unique taster pairs, always with the alphabetically smaller name first
    let combineAllTasters (tasters: Taster list) : (string * string) list =
        let names = tasters |> List.map _.Name |> List.sort

        [
            for i in 0 .. names.Length - 2 do
                for j in i + 1 .. names.Length - 1 do
                    yield names[i], names[j]
        ]

    // Correlation between tasters (most similar tasters)
    let correlationBetweenTasters (tasters: Taster list) (scores: Score list) : TasterPairResult list =
        let tasterPairs = combineAllTasters tasters

        // Pre-compute score arrays per taster once to avoid rescanning for every pair
        let scoresByTaster =
            tasters
            |> List.map (fun t -> t.Name, getScoresForTaster t.Name scores)
            |> Map.ofList

        tasterPairs
        |> List.map (fun (tasterName1, tasterName2) ->
            let scores1 =
                scoresByTaster
                |> Map.tryFind tasterName1
                |> Option.defaultValue [||]

            let scores2 =
                scoresByTaster
                |> Map.tryFind tasterName2
                |> Option.defaultValue [||]

            let correlation = Seq.pearson scores1 scores2

            {
                Name1 = tasterName1
                Name2 = tasterName2
                Value = correlation
            })
        |> List.sortByDescending _.Value

    // Correlation to ABV (fondest of strong beers)
    let correlationToAbv (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let beerAbv = beers |> List.sortBy _.Id |> List.map _.ABV
        // Pre-group scores by taster to avoid a repeated full-list scan per taster
        let scoresByTaster = scores |> List.groupBy _.TasterName |> Map.ofList

        tasters
        |> List.map (fun t ->
            let tasterScores = lookupTasterScores scoresByTaster t.Name
            let correlation = Seq.pearson tasterScores beerAbv
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortByDescending _.Value

    // Correlation to price per ABV (fondest of inexpensive alcohol)
    let correlationToAbvPrice (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let beerAbvPrice =
            beers
            |> List.sortBy _.Id
            |> List.map _.PricePerAbv

        // Pre-group scores by taster to avoid a repeated full-list scan per taster
        let scoresByTaster = scores |> List.groupBy _.TasterName |> Map.ofList

        tasters
        |> List.map (fun t ->
            let tasterScores = lookupTasterScores scoresByTaster t.Name
            let correlation = Seq.pearson tasterScores beerAbvPrice
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortByDescending _.Value

    // Correlation to taster age (beers preferred by older tasters)
    let correlationToAge (beers: Beer list) (tasters: Taster list) (scores: Score list) : BeerResult list =
        let currentYear = System.DateTime.Now.Year

        // Create a map of taster names to their ages
        let tasterAges =
            tasters
            |> List.choose (fun t ->
                t.BirthYear
                |> Option.map (fun birthYear -> t.Name, float (currentYear - birthYear)))
            |> Map.ofList

        // Pre-group scores by beer ID to avoid an O(beers × scores) repeated scan
        let scoresByBeer = scores |> List.groupBy _.BeerId |> Map.ofList

        beers
        |> List.map (fun beer ->
            let beerScores =
                scoresByBeer
                |> Map.tryFind beer.Id
                |> Option.defaultValue []

            // Get all scores for this beer along with the taster ages
            let scoreAgesPairs =
                beerScores
                |> List.choose (fun s ->
                    match Map.tryFind s.TasterName tasterAges, s.ScoreValue with
                    | Some age, Some score -> Some(float score, age)
                    | _ -> None)

            // Require at least 3 data points for meaningful correlation
            if scoreAgesPairs.Length >= 3 then
                let beerScoresArr = scoreAgesPairs |> List.map fst |> List.toArray
                let ages = scoreAgesPairs |> List.map snd |> List.toArray
                let correlation = Seq.pearson beerScoresArr ages

                {
                    Name = $"{beer.Producer} - {beer.Name}"
                    Value = correlation
                }
                : BeerResult
            else
                // Not enough data to calculate correlation
                {
                    Name = $"{beer.Producer} - {beer.Name}"
                    Value = 0.0
                }
                : BeerResult)
        |> List.sortByDescending _.Value
