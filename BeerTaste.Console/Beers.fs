module BeerTaste.Console.Beers

open System
open OfficeOpenXml
open BeerTaste.Common

let norwegianToFloat (s: string) : float = s.Replace(",", ".") |> float

let rowToBeer (worksheet: ExcelWorksheet) (row: int) : Beer = {
    Id = worksheet.Cells[row, 1].Text |> int
    Name = worksheet.Cells[row, 2].Text
    BeerType = worksheet.Cells[row, 3].Text
    Origin = worksheet.Cells[row, 4].Text
    Producer = worksheet.Cells[row, 5].Text
    ABV = worksheet.Cells[row, 6].Text |> norwegianToFloat
    Volume = worksheet.Cells[row, 7].Text |> norwegianToFloat
    Price = worksheet.Cells[row, 8].Text |> norwegianToFloat
    Packaging = worksheet.Cells[row, 9].Text
}

let readBeers (fileName: string) : Beer list =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets["Beers"]

    if worksheet.Dimension = null then
        []
    else
        seq { 2 .. worksheet.Dimension.End.Row }
        |> Seq.map (rowToBeer worksheet)
        |> Seq.toList

let createTastersSchema (fileName: string) (beers: Beer list) : unit =
    // Get the template file path
    let templateFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BeerTaste.xlsx")

    use templatePackage = new ExcelPackage(templateFile)
    use targetPackage = new ExcelPackage(fileName)

    let schemaName = "TastersSchema"

    // Delete existing TastersSchema worksheet if it exists
    let existingWorksheet = targetPackage.Workbook.Worksheets[schemaName]

    if existingWorksheet <> null then
        targetPackage.Workbook.Worksheets.Delete(existingWorksheet)

    // Copy the template from BeerTaste.xlsx
    let templateWorksheet = templatePackage.Workbook.Worksheets["TastersSchema"]

    targetPackage.Workbook.Worksheets.Add(schemaName, templateWorksheet)
    |> ignore

    let worksheet = targetPackage.Workbook.Worksheets[schemaName]
    let height = worksheet.Row(4).Height

    worksheet.InsertRow(4, beers.Length - 1, 4)

    for i in 4 .. (beers.Length + 4) do
        worksheet.Row(i).Height <- height

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells[i + 4, 1].Value <- beer.Id
        worksheet.Cells[i + 4, 2].Value <- beer.Producer
        worksheet.Cells[i + 4, 3].Value <- beer.Name
        worksheet.Cells[i + 4, 4].Value <- beer.BeerType
        worksheet.Cells[i + 4, 5].Value <- beer.Origin
        worksheet.Cells[i + 4, 6].Value <- beer.ABV)

    targetPackage.Save()
