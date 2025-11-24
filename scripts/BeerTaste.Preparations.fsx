#load "BeerTaste.Common.fsx"

open System
open OfficeOpenXml
open BeerTaste.Common

let createTastersSchema (fileName: string) (beers: Beer list) =
    use package = new ExcelPackage(fileName)

    let schemaName =
        "TastersSchema "
        + DateTime.Now.ToString("yyyy-MM-dd HHmmss")

    let worksheet = package.Workbook.Worksheets.Copy("TastersSchema", schemaName)
    let height = worksheet.Row(3).Height

    worksheet.InsertRow(3, beers.Length - 1, 3)
    |> ignore

    for i in 3 .. beers.Length do
        worksheet.Row(i).Height <- height

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells.[i + 3, 1].Value <- beer.Id
        worksheet.Cells.[i + 3, 2].Value <- beer.Producer
        worksheet.Cells.[i + 3, 3].Value <- beer.Name
        worksheet.Cells.[i + 3, 4].Value <- beer.BeerType
        worksheet.Cells.[i + 3, 5].Value <- beer.Origin
        worksheet.Cells.[i + 3, 6].Value <- beer.ABV)

    package.Save()

let createScoreSchema (fileName: string) (beers: Beer list) (tasters: Taster list) =
    use package = new ExcelPackage(fileName)

    let sheetName =
        "ScoreSchema "
        + DateTime.Now.ToString("yyyy-MM-dd HHmmss")

    let worksheet = package.Workbook.Worksheets.Add(sheetName)
    worksheet.Cells.[1, 1].Value <- "Id"
    worksheet.Cells.[1, 2].Value <- "Producer"
    worksheet.Cells.[1, 3].Value <- "Name"

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells.[i + 2, 1].Value <- beer.Id
        worksheet.Cells.[i + 2, 2].Value <- beer.Producer
        worksheet.Cells.[i + 2, 3].Value <- beer.Name)

    tasters
    |> List.iteri (fun i taster -> worksheet.Cells.[1, i + 4].Value <- taster.Name)

    worksheet.Row(1).Style.Font.Bold <- true
    worksheet.Column(1).Style.Font.Bold <- true
    worksheet.Column(2).Style.Font.Bold <- true
    worksheet.Column(3).Style.Font.Bold <- true

    package.Save()

let tastingName = "BeerTaste"
let fileName = tastingName + ".xlsx"
let beers = fileName |> readBeers
let tasters = fileName |> readTasters

createTastersSchema fileName beers
createScoreSchema fileName beers tasters
