namespace BeerTaste.Common

open FSharp.Stats
open FSharp.Stats.Correlation

module Results =
    type BeerResult = { Name: string; Value: float }

    type TasterResult = { Name: string; Value: float }

    type TasterPairResult = {
        Name1: string
        Name2: string
        Value: float
    }

    // Get all scores for a specific taster, sorted by beer ID
    let getScoresForTaster (tasterName: string) (scores: Score list) : float array =
        scores
        |> List.filter (fun s -> s.TasterName = tasterName)
        |> List.sortBy _.BeerId
        |> List.map (fun s -> s.ScoreValue |> Option.defaultValue 0)
        |> List.map float
        |> List.toArray

    // Get all scores for a specific beer
    let getScoresForBeer (scores: Score list) (beerId: int) : float array =
        scores
        |> List.filter (fun s -> s.BeerId = beerId)
        |> List.sortBy _.TasterName
        |> List.map (fun s -> s.ScoreValue |> Option.defaultValue 0)
        |> List.map float
        |> List.toArray

    let getAverageScoreForBeer (scores: Score list) (beerId: int) : float =
        let beerScores = getScoresForBeer scores beerId

        if beerScores.Length > 0 then
            Array.average beerScores
        else
            0.0

    let beerAverages (beers: Beer list) (scores: Score list) : BeerResult list =
        beers
        |> List.map (fun b -> b, getAverageScoreForBeer scores b.Id)
        |> List.map (fun (b, avg) ->
            {
                Name = $"{b.Producer} - {b.Name}"
                Value = avg
            }
            : BeerResult)
        |> List.sortByDescending _.Value

    // Most controversial beers by standard deviation
    let beerStandardDeviations (beers: Beer list) (scores: Score list) : BeerResult list =
        beers
        |> List.map (fun b ->
            let beerScores = getScoresForBeer scores b.Id

            let stdDev =
                if beerScores.Length > 0 then
                    Seq.stDevPopulation beerScores
                else
                    0.0

            ({
                Name = $"{b.Producer} - {b.Name}"
                Value = stdDev
            }
            : BeerResult))
        |> List.sortByDescending _.Value

    let correlationToAverages (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let avgScoresByBeer =
            beers
            |> List.map _.Id
            |> List.sort
            |> List.map (getAverageScoreForBeer scores)

        tasters
        |> List.map (fun t ->
            let tasterScores = getScoresForTaster t.Name scores
            let correlation = Seq.pearson tasterScores avgScoresByBeer
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortBy _.Value

    // Generate all unique taster pairs
    let combineAllTasters (tasters: Taster list) : (string * string) list =
        seq {
            for taster in tasters do
                for taster2 in tasters do
                    if taster.Name < taster2.Name then
                        yield taster.Name, taster2.Name
                    elif taster.Name > taster2.Name then
                        yield taster2.Name, taster.Name
        }
        |> Seq.distinct
        |> Seq.toList

    // Correlation between tasters (most similar tasters)
    let correlationBetweenTasters (tasters: Taster list) (scores: Score list) : TasterPairResult list =
        let tasterPairs = combineAllTasters tasters

        tasterPairs
        |> List.map (fun (tasterName1, tasterName2) ->
            let scores1 = getScoresForTaster tasterName1 scores
            let scores2 = getScoresForTaster tasterName2 scores
            let correlation = Seq.pearson scores1 scores2

            {
                Name1 = tasterName1
                Name2 = tasterName2
                Value = correlation
            })
        |> List.sortByDescending _.Value

    // Correlation to ABV (fondest of strong beers)
    let correlationToAbv (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let beerAbv =
            beers
            |> List.sortBy _.Id
            |> List.map _.ABV

        tasters
        |> List.map (fun t ->
            let tasterScores = getScoresForTaster t.Name scores
            let correlation = Seq.pearson tasterScores beerAbv
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortByDescending _.Value

    // Correlation to price per ABV (fondest of inexpensive alcohol)
    let correlationToAbvPrice (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let beerAbvPrice =
            beers
            |> List.sortBy _.Id
            |> List.map _.PricePerAbv

        tasters
        |> List.map (fun t ->
            let tasterScores = getScoresForTaster t.Name scores
            let correlation = Seq.pearson tasterScores beerAbvPrice
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortByDescending _.Value
