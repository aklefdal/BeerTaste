#load "BeerTaste.Common.fsx"
#load "BeerTaste.Results.fsx"

open System.IO
open BeerTaste.Common
open BeerTaste.Results

let writeBeerAveragesResults (resultsFile: StreamWriter) (beers: Beer list) (scores: Scores) =

    let beerAverages = beerAverages beers scores

    resultsFile.WriteLine("## Best beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Beer | Average Score |")
    resultsFile.WriteLine("|------|------|--------------:|")


    beerAverages
    |> List.iteri (fun i (name, avg) -> resultsFile.WriteLine($"| {i + 1} | {name} | {avg:F2} |"))

    resultsFile.WriteLine()

let writeBeerStdDevResults (resultsFile: StreamWriter) (beers: Beer list) (scores: Scores) =

    let beerStandardDeviations = beerStandardDeviations beers scores

    resultsFile.WriteLine("## Most controversial beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Beer | Standard Deviation |")
    resultsFile.WriteLine("|------|------|-------------------:|")

    beerStandardDeviations
    |> List.iteri (fun i (name, correl) -> resultsFile.WriteLine($"| {i + 1} | {name} | {correl:F2} |"))

    resultsFile.WriteLine()

let writeTasterCorrelToAverageResults
    (resultsFile: StreamWriter)
    (beers: Beer list)
    (tasters: Taster list)
    (scores: Scores)
    =

    let tasterCorrelToAverage = correlationToAverages beers tasters scores

    resultsFile.WriteLine("## Most deviant tasters")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster | Correlation to Average |")
    resultsFile.WriteLine("|------|--------|-----------------------:|")


    tasterCorrelToAverage
    |> List.iteri (fun i (name, stddev) -> resultsFile.WriteLine($"| {i + 1} | {name} | {stddev:F2} |"))

    resultsFile.WriteLine()

let writeCorrelBetweenTasters (resultsFile: StreamWriter) (tasters: Taster list) (scores: Scores) =

    let correlationBetweenTasters = correlationBetweenTasters tasters scores

    resultsFile.WriteLine("## Most similar tasters")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster 1 | Taster 2 | Correlation |")
    resultsFile.WriteLine("|------|----------|----------|------------:|")


    correlationBetweenTasters
    |> List.iteri (fun i ((name1, name2), correl) ->
        resultsFile.WriteLine($"| {i + 1} | {name1} | {name2} | {correl:F2} |"))

    resultsFile.WriteLine()

let writeCorrelToAbv (resultsFile: StreamWriter) (beers: Beer list) (tasters: Taster list) (scores: Scores) =

    let tasterCorrelToAbv = correlationToAbv beers tasters scores

    resultsFile.WriteLine("## Most fond of strong beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster | Correlation to ABV |")
    resultsFile.WriteLine("|------|--------|-----------------------:|")


    tasterCorrelToAbv
    |> List.iteri (fun i (name, stddev) -> resultsFile.WriteLine($"| {i + 1} | {name} | {stddev:F2} |"))

    resultsFile.WriteLine()

let writeCorrelToAbvPrice (resultsFile: StreamWriter) (beers: Beer list) (tasters: Taster list) (scores: Scores) =

    let tasterCorrelToAbvPrice = correlationToAbvPrice beers tasters scores

    resultsFile.WriteLine("## Most fond of cheap alcohol")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Taster | Correlation to price per ABV |")
    resultsFile.WriteLine("|------|--------|-----------------------------:|")


    tasterCorrelToAbvPrice
    |> List.iteri (fun i (name, stddev) -> resultsFile.WriteLine($"| {i + 1} | {name} | {stddev:F2} |"))

    resultsFile.WriteLine()

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

writeBeerAveragesResults resultsFile beers scores
writeBeerStdDevResults resultsFile beers scores
writeTasterCorrelToAverageResults resultsFile beers tasters scores
writeCorrelBetweenTasters resultsFile tasters scores
writeCorrelToAbv resultsFile beers tasters scores
writeCorrelToAbvPrice resultsFile beers tasters scores

resultsFile.Close()
