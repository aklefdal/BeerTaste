#load "BeerTaste.Common.fsx"

#r "nuget: FSharp.Stats, 0.4.0"

open System
open System.Collections.Generic
open System.IO
open OfficeOpenXml
open BeerTaste.Common
open FSharp.Stats
open FSharp.Stats.Correlation

type Scoring =
    { BeerId: int
      TasterName: string
      Score: float }

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

                yield
                    { BeerId = beerId
                      TasterName = tasterName
                      Score = score }
    }
    |> List.ofSeq
    |> Scores

let standardDeviation (values: float array) : float =
    let avg = Array.average values

    let variance =
        values
        |> Array.averageBy (fun v -> (v - avg) ** 2.0)

    Math.Sqrt(variance)

let tastingName = "ØJ Ølsmaking 2024"
let fileName = tastingName + ".xlsx"
let beers = fileName |> readBeers
let tasters = fileName |> readTasters

let scores = readScores fileName beers tasters

beers
|> List.map (fun b ->
    b.Name,
    b.Id
    |> scores.GetScoresForBeer
    |> standardDeviation)
|> List.sortByDescending snd

let resultsFileName = tastingName + " Results.md"

// Write results to markdown file, overwrite if it exists
let resultsFile = resultsFileName |> File.CreateText

resultsFile.WriteLine($"# {tastingName} Results")
resultsFile.WriteLine()

let beerAverages (beers: Beer list) =
    beers
    |> List.map (fun b -> $"{b.Producer} - {b.Name}", scores.GetScoresForBeer b.Id |> Array.average)
    |> List.sortByDescending snd

let beerStandardDeviations (beers: Beer list) =
    beers
    |> List.map (fun b ->
        $"{b.Producer} - {b.Name}",
        scores.GetScoresForBeer b.Id
        |> Seq.stDevPopulation)
    |> List.sortByDescending snd

let correlationToAverages (beers: Beer list) (tasters: Taster list) =
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

let correlationBetweenTasters (tasters: Taster list) =
    let tasterPairs = combineAllTaster tasters

    tasterPairs
    |> List.map (fun (tasterName1, tasterName2) ->
        let scores1 = tasterName1 |> scores.GetScoresForTaster
        let scores2 = tasterName2 |> scores.GetScoresForTaster

        let correl = Seq.pearson scores1 scores2

        (tasterName1, tasterName2), correl)
    |> List.sortBy snd

let correlationToAbv (beers: Beer list) (tasters: Taster list) =
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

let correlationToAbvPrice (beers: Beer list) (tasters: Taster list) =
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

let writeBeerAveragesResults (file: StreamWriter) (beers: Beer list) =

    let beerAverages = beers |> beerAverages

    resultsFile.WriteLine("## Best beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Beer | Average Score |")
    resultsFile.WriteLine("|------|------|--------------:|")


    beerAverages
    |> List.iteri (fun i (name, avg) -> resultsFile.WriteLine($"| {i + 1} | {name} | {avg:F2} |"))

    resultsFile.WriteLine()

let writeBeerStdDevResults (file: StreamWriter) (beers: Beer list) =

    let beerStandardDeviations = beers |> beerStandardDeviations

    resultsFile.WriteLine("## Most controversial beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Beer | Standard Deviation |")
    resultsFile.WriteLine("|------|------|-------------------:|")

    beerStandardDeviations
    |> List.iteri (fun i (name, correl) -> resultsFile.WriteLine($"| {i + 1} | {name} | {correl:F2} |"))

    resultsFile.WriteLine()

let writeTasterCorrelToAverageResults (file: StreamWriter) (beers: Beer list) (tasters: Taster list) =

    let tasterCorrelToAverage = correlationToAverages beers tasters

    resultsFile.WriteLine("## Most deviant tasters")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster | Correlation to Average |")
    resultsFile.WriteLine("|------|--------|-----------------------:|")


    tasterCorrelToAverage
    |> List.iteri (fun i (name, stddev) -> resultsFile.WriteLine($"| {i + 1} | {name} | {stddev:F2} |"))

    resultsFile.WriteLine()

let writeCorrelBetweenTasters (file: StreamWriter) (tasters: Taster list) =

    let correlationBetweenTasters = correlationBetweenTasters tasters

    resultsFile.WriteLine("## Most similar tasters")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster 1 | Taster 2 | Correlation |")
    resultsFile.WriteLine("|------|----------|----------|------------:|")


    correlationBetweenTasters
    |> List.iteri (fun i ((name1, name2), correl) ->
        resultsFile.WriteLine($"| {i + 1} | {name1} | {name2} | {correl:F2} |"))

    resultsFile.WriteLine()

let writeCorrelToAbv (file: StreamWriter) (beers: Beer list) (tasters: Taster list) =

    let tasterCorrelToAbv = correlationToAbv beers tasters

    resultsFile.WriteLine("## Most fond of strong beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster | Correlation to ABV |")
    resultsFile.WriteLine("|------|--------|-----------------------:|")


    tasterCorrelToAbv
    |> List.iteri (fun i (name, stddev) -> resultsFile.WriteLine($"| {i + 1} | {name} | {stddev:F2} |"))

    resultsFile.WriteLine()

let writeCorrelToAbvPrice (file: StreamWriter) (beers: Beer list) (tasters: Taster list) =

    let tasterCorrelToAbvPrice = correlationToAbvPrice beers tasters

    resultsFile.WriteLine("## Most fond of cheap alcohol")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster | Correlation to ABV |")
    resultsFile.WriteLine("|------|--------|-----------------------:|")


    tasterCorrelToAbvPrice
    |> List.iteri (fun i (name, stddev) -> resultsFile.WriteLine($"| {i + 1} | {name} | {stddev:F2} |"))

    resultsFile.WriteLine()

writeBeerAveragesResults resultsFile beers
writeBeerStdDevResults resultsFile beers
writeTasterCorrelToAverageResults resultsFile beers tasters
writeCorrelBetweenTasters resultsFile tasters
writeCorrelToAbv resultsFile beers tasters
writeCorrelToAbvPrice resultsFile beers tasters

resultsFile.Close()
