module BeerTaste.Console.Tasters

open OfficeOpenXml
open BeerTaste.Common

let rowToTaster (worksheet: ExcelWorksheet) (row: int) : Taster =
    let birthYearText = worksheet.Cells[row, 3].Text
    let birthYear =
        match System.Int32.TryParse(birthYearText) with
        | true, year -> Some year
        | _ -> None
    {
    Name = worksheet.Cells[row, 1].Text
    Email = worksheet.Cells[row, 2].Text |> Option.ofObj
    BirthYear = birthYear
}

let readTasters (fileName: string) : Taster list =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets["Tasters"]

    if worksheet.Dimension = null then
        []
    else
        seq { 2 .. worksheet.Dimension.End.Row }
        |> Seq.map (rowToTaster worksheet)
        |> Seq.toList

