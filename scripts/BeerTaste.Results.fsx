#load "BeerTaste.Common.fsx"

#r "nuget: FSharp.Stats, 0.4.0"

open System
open OfficeOpenXml
open BeerTaste.Common
open FSharp.Stats
open FSharp.Stats.Correlation

type Scoring = {
    BeerId: int
    TasterName: string
    Score: float
}

type Scores(scorings: Scoring list) =
    let numberOfBeers =
        scorings
        |> List.map (fun s -> s.BeerId)
        |> List.distinct
        |> List.length

    member _.GetScoresForTaster(tasterName: string) : float array =
        scorings
        |> List.filter (fun s -> s.TasterName = tasterName)
        |> List.sortBy (fun s -> s.BeerId)
        |> List.map (fun s -> s.Score)
        |> List.toArray

    member _.GetScoresForBeer(beerId: int) : float array =
        scorings
        |> List.filter (fun s -> s.BeerId = beerId)
        |> List.map (fun s -> s.Score)
        |> List.toArray

let readScores (fileName: string) (beers: Beer list) (tasters: Taster list) =
    seq {
        use package = new ExcelPackage(fileName)
        let worksheet = package.Workbook.Worksheets.["ScoreSchema"]

        for i in 2 .. worksheet.Dimension.End.Row do
            for j in 4 .. worksheet.Dimension.End.Column do
                let tasterName = worksheet.Cells.[1, j].Text
                let beerId = worksheet.Cells.[i, 1].Text |> int
                let score = worksheet.Cells.[i, j].Text |> norwegianToFloat

                yield {
                    BeerId = beerId
                    TasterName = tasterName
                    Score = score
                }
    }
    |> List.ofSeq
    |> Scores

let standardDeviation (values: float array) : float =
    let avg = Array.average values

    let variance =
        values
        |> Array.averageBy (fun v -> (v - avg) ** 2.0)

    Math.Sqrt(variance)

let beerAverages (beers: Beer list) (scores: Scores) =
    beers
    |> List.map (fun b -> $"{b.Producer} - {b.Name}", scores.GetScoresForBeer b.Id |> Array.average)
    |> List.sortByDescending snd

let beerStandardDeviations (beers: Beer list) (scores: Scores) =
    beers
    |> List.map (fun b ->
        $"{b.Producer} - {b.Name}",
        scores.GetScoresForBeer b.Id
        |> Seq.stDevPopulation)
    |> List.sortByDescending snd

let correlationToAverages (beers: Beer list) (tasters: Taster list) (scores: Scores) =
    let beerAverages =
        beers
        |> List.map (fun b -> b.Id, scores.GetScoresForBeer b.Id |> Array.average)
        |> List.sortBy fst
        |> List.map snd

    tasters
    |> List.map (fun t ->
        let correl =
            t.Name
            |> scores.GetScoresForTaster
            |> Seq.pearson beerAverages

        t.Name, correl)
    |> List.sortBy snd

let combineAllTaster (tasters: Taster list) =
    seq {
        for taster in tasters do
            for taster2 in tasters do
                if taster.Name < taster2.Name then
                    yield taster.Name, taster2.Name
                elif taster.Name > taster2.Name then
                    yield taster2.Name, taster.Name
                else
                    ()
    }
    |> Seq.distinct
    |> Seq.toList

let correlationBetweenTasters (tasters: Taster list) (scores: Scores) =
    let tasterPairs = combineAllTaster tasters

    tasterPairs
    |> List.map (fun (tasterName1, tasterName2) ->
        let scores1 = tasterName1 |> scores.GetScoresForTaster
        let scores2 = tasterName2 |> scores.GetScoresForTaster

        let correl = Seq.pearson scores1 scores2

        (tasterName1, tasterName2), correl)
    |> List.sortByDescending snd

let correlationToAbv (beers: Beer list) (tasters: Taster list) (scores: Scores) =
    let beerAbv =
        beers
        |> List.map (fun b -> b.Id, b.ABV)
        |> List.sortBy fst
        |> List.map snd

    tasters
    |> List.map (fun t ->
        let correl =
            t.Name
            |> scores.GetScoresForTaster
            |> Seq.pearson beerAbv

        t.Name, correl)
    |> List.sortByDescending snd

let correlationToAbvPrice (beers: Beer list) (tasters: Taster list) (scores: Scores) =
    let beerAbvPrice =
        beers
        |> List.map (fun b -> b.Id, b.PricePerAbv)
        |> List.sortBy fst
        |> List.map snd

    tasters
    |> List.map (fun t ->
        let correl =
            t.Name
            |> scores.GetScoresForTaster
            |> Seq.pearson beerAbvPrice

        t.Name, correl)
    |> List.sortByDescending snd
