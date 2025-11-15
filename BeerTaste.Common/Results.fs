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
    let getScoresForTaster (tasterName: string) (beers: Beer list) (scores: Score list) : float array =
        beers
        |> List.sortBy (fun b -> b.Id)
        |> List.map (fun b ->
            scores
            |> List.tryFind (fun s -> s.BeerId = b.Id && s.TasterName = tasterName)
            |> Option.map (fun s -> float s.ScoreValue)
            |> Option.defaultValue 0.0)
        |> List.toArray

    // Get all scores for a specific beer
    let getScoresForBeer (beerId: int) (scores: Score list) : float array =
        scores
        |> List.filter (fun s -> s.BeerId = beerId)
        |> List.map (fun s -> float s.ScoreValue)
        |> List.toArray

    // Best beers by average score
    let beerAverages (beers: Beer list) (scores: Score list) : BeerResult list =
        beers
        |> List.map (fun b ->
            let beerScores = getScoresForBeer b.Id scores

            let avg =
                if beerScores.Length > 0 then
                    Array.average beerScores
                else
                    0.0

            ({
                Name = $"{b.Producer} - {b.Name}"
                Value = avg
            }
            : BeerResult))
        |> List.sortByDescending (fun r -> r.Value)

    // Most controversial beers by standard deviation
    let beerStandardDeviations (beers: Beer list) (scores: Score list) : BeerResult list =
        beers
        |> List.map (fun b ->
            let beerScores = getScoresForBeer b.Id scores

            let stdDev =
                if beerScores.Length > 0 then
                    stDevPopulation beerScores
                else
                    0.0

            ({
                Name = $"{b.Producer} - {b.Name}"
                Value = stdDev
            }
            : BeerResult))
        |> List.sortByDescending (fun r -> r.Value)

    // Taster correlation to beer averages (most deviant tasters)
    let correlationToAverages (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let avgScoresByBeer =
            beers
            |> List.sortBy (fun b -> b.Id)
            |> List.map (fun b ->
                let beerScores = getScoresForBeer b.Id scores

                if beerScores.Length > 0 then
                    Array.average beerScores
                else
                    0.0)

        tasters
        |> List.map (fun t ->
            let tasterScores = getScoresForTaster t.Name beers scores
            let correlation = pearson tasterScores avgScoresByBeer
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortBy (fun r -> r.Value)

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
    let correlationBetweenTasters
        (tasters: Taster list)
        (beers: Beer list)
        (scores: Score list)
        : TasterPairResult list =
        let tasterPairs = combineAllTasters tasters

        tasterPairs
        |> List.map (fun (tasterName1, tasterName2) ->
            let scores1 = getScoresForTaster tasterName1 beers scores
            let scores2 = getScoresForTaster tasterName2 beers scores
            let correlation = pearson scores1 scores2

            {
                Name1 = tasterName1
                Name2 = tasterName2
                Value = correlation
            })
        |> List.sortByDescending (fun r -> r.Value)

    // Correlation to ABV (fondest of strong beers)
    let correlationToAbv (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let beerAbv =
            beers
            |> List.sortBy (fun b -> b.Id)
            |> List.map (fun b -> b.ABV)

        tasters
        |> List.map (fun t ->
            let tasterScores = getScoresForTaster t.Name beers scores
            let correlation = pearson tasterScores beerAbv
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortByDescending (fun r -> r.Value)

    // Correlation to price per ABV (fondest of inexpensive alcohol)
    let correlationToAbvPrice (beers: Beer list) (tasters: Taster list) (scores: Score list) : TasterResult list =
        let beerAbvPrice =
            beers
            |> List.sortBy (fun b -> b.Id)
            |> List.map (fun b -> b.PricePerAbv)

        tasters
        |> List.map (fun t ->
            let tasterScores = getScoresForTaster t.Name beers scores
            let correlation = pearson tasterScores beerAbvPrice
            ({ Name = t.Name; Value = correlation }: TasterResult))
        |> List.sortByDescending (fun r -> r.Value)
