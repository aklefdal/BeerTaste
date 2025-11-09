#r "nuget: EPPlus, 8.2.1"

open System
open System.IO
open OfficeOpenXml

// Open Excel file
ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")

type Beer =
    { Id: int
      Name: string
      BeerType: string
      Origin: string
      Producer: string
      ABV: float
      Volume: float
      Price: float
      Packaging: string }

type Taster =
    { Name: string
      Email: string
      BirthYear: int }

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

let norwegianToFloat (s: string) : float = s.Replace(",", ".") |> float

let rowToBeer (worksheet: ExcelWorksheet) (row: int) : Beer =
    { Id = worksheet.Cells.[row, 1].Text |> int
      Name = worksheet.Cells.[row, 2].Text
      BeerType = worksheet.Cells.[row, 3].Text
      Origin = worksheet.Cells.[row, 4].Text
      Producer = worksheet.Cells.[row, 5].Text
      ABV = worksheet.Cells.[row, 6].Text |> norwegianToFloat
      Volume = worksheet.Cells.[row, 7].Text |> norwegianToFloat
      Price = worksheet.Cells.[row, 8].Text |> norwegianToFloat
      Packaging = worksheet.Cells.[row, 9].Text }

let readBeers (fileName: string) =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets.["Beers"]

    seq { 2 .. worksheet.Dimension.End.Row }
    |> Seq.map (rowToBeer worksheet)
    |> Seq.toList

let rowToTaster (worksheet: ExcelWorksheet) (row: int) : Taster =
    { Name = worksheet.Cells.[row, 1].Text
      Email = worksheet.Cells.[row, 2].Text
      BirthYear = worksheet.Cells.[row, 3].Text |> int }

let readTasters (fileName: string) =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets.["Tasters"]

    seq { 2 .. worksheet.Dimension.End.Row }
    |> Seq.map (rowToTaster worksheet)
    |> Seq.toList

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

createTastersSchema fileName beers
createScoreSchema fileName beers tasters

let scores = readScores fileName beers tasters
scores.GetScoresForTaster "Alf Kåre"

scores.GetScoresForBeer 1

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

let writeBeerAveragesResults (file: StreamWriter) (beers: Beer list) =

    let beerAverages = beers |> beerAverages

    resultsFile.WriteLine("## Best beers")
    resultsFile.WriteLine()
    resultsFile.WriteLine("| Rank | Beer | Average Score |")
    resultsFile.WriteLine("|------|------|--------------:|")


    beerAverages
    |> List.iteri (fun i (name, avg) -> resultsFile.WriteLine($"| {i + 1} | {name} | {avg:F2} |"))

writeBeerAveragesResults resultsFile beers

resultsFile.Close()
