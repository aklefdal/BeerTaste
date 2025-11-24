module BeerTaste.Console.Scores

open OfficeOpenXml
open BeerTaste.Common

type ScoresSchemaState =
    | DoesNotExist
    | ExistsWithoutScores
    | ExistsWithScores
    | ExistsAndComplete

[<Literal>]
let sheetName = "ScoreSchema"

let norwegianToFloat (s: string) : float = s.Replace(",", ".") |> float
let norwegianToInt (s: string) : int = s.Replace(",", ".") |> int

let readScoresFroWorksheet (worksheet: ExcelWorksheet) : Score list =
    if worksheet = null || worksheet.Dimension = null then
        []
    else
        let maxRow = worksheet.Dimension.End.Row
        let maxCol = worksheet.Dimension.End.Column

        if maxRow < 2 || maxCol < 4 then
            []
        else
            // Read taster names from row 1, columns 4+
            let tasterNames =
                seq { 4..maxCol }
                |> Seq.map (fun col -> worksheet.Cells[1, col].Text)
                |> Seq.toList

            // Read scores for each beer and taster
            seq {
                for row in 2..maxRow do
                    let beerId = worksheet.Cells[row, 1].Text |> int

                    for col in 4..maxCol do
                        let tasterName = tasterNames[col - 4]

                        let scoreValue =
                            match worksheet.Cells[row, col].Text with
                            | null -> None
                            | "" -> None
                            | "-" -> Some 0
                            | scoreText -> scoreText |> norwegianToInt |> Some

                        yield {
                            BeerId = beerId
                            TasterName = tasterName
                            ScoreValue = scoreValue
                        }
            }
            |> Seq.toList

let readScores (fileName: string) : Score list =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets[sheetName]
    worksheet |> readScoresFroWorksheet

let hasScores (worksheet: ExcelWorksheet) : bool =
    worksheet
    |> readScoresFroWorksheet
    |> Scores.hasScores

let isComplete (worksheet: ExcelWorksheet) : bool =
    worksheet
    |> readScoresFroWorksheet
    |> Scores.isComplete

let getScoresSchemaState (fileName: string) : ScoresSchemaState =
    use package = new ExcelPackage(fileName)
    let existingWorksheet = package.Workbook.Worksheets[sheetName]

    if existingWorksheet = null then DoesNotExist
    elif existingWorksheet |> isComplete then ExistsAndComplete
    elif existingWorksheet |> hasScores then ExistsWithScores
    else ExistsWithoutScores

let deleteAndCreateScoreSchema
    (scoresTableClient: Azure.Data.Tables.TableClient)
    (beerTasteGuid: string)
    (fileName: string)
    (beers: Beer list)
    (tasters: Taster list)
    : unit =
    // Delete scores from Azure Table Storage
    Scores.deleteScoresForBeerTaste scoresTableClient beerTasteGuid

    use package = new ExcelPackage(fileName)

    // Delete existing ScoreSchema worksheet if it exists
    let existingWorksheet = package.Workbook.Worksheets[sheetName]

    if existingWorksheet <> null then
        package.Workbook.Worksheets.Delete(existingWorksheet)

    let worksheet = package.Workbook.Worksheets.Add(sheetName)
    worksheet.Cells[1, 1].Value <- "Id"
    worksheet.Cells[1, 2].Value <- "Producer"
    worksheet.Cells[1, 3].Value <- "Name"

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells[i + 2, 1].Value <- beer.Id
        worksheet.Cells[i + 2, 2].Value <- beer.Producer
        worksheet.Cells[i + 2, 3].Value <- beer.Name)

    tasters
    |> List.iteri (fun i taster -> worksheet.Cells[1, i + 4].Value <- taster.Name)

    worksheet.Row(1).Style.Font.Bold <- true
    worksheet.Column(1).Style.Font.Bold <- true
    worksheet.Column(2).Style.Font.Bold <- true
    worksheet.Column(3).Style.Font.Bold <- true

    package.Save()
