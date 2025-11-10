#r "nuget: EPPlus, 8.2.1"

open OfficeOpenXml

// Open Excel file
ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")

type Beer = {
    Id: int
    Name: string
    BeerType: string
    Origin: string
    Producer: string
    ABV: float
    Volume: float
    Price: float
    Packaging: string
} with
    member this.PricePerLiter = this.Price / this.Volume
    member this.PricePerAbv = this.PricePerLiter / (this.ABV / 100.0)

type Taster = {
    Name: string
    Email: string
    BirthYear: int
}

let norwegianToFloat (s: string) : float = s.Replace(",", ".") |> float

let rowToBeer (worksheet: ExcelWorksheet) (row: int) : Beer = {
    Id = worksheet.Cells.[row, 1].Text |> int
    Name = worksheet.Cells.[row, 2].Text
    BeerType = worksheet.Cells.[row, 3].Text
    Origin = worksheet.Cells.[row, 4].Text
    Producer = worksheet.Cells.[row, 5].Text
    ABV = worksheet.Cells.[row, 6].Text |> norwegianToFloat
    Volume = worksheet.Cells.[row, 7].Text |> norwegianToFloat
    Price = worksheet.Cells.[row, 8].Text |> norwegianToFloat
    Packaging = worksheet.Cells.[row, 9].Text
}

let readBeers (fileName: string) =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets.["Beers"]

    seq { 2 .. worksheet.Dimension.End.Row }
    |> Seq.map (rowToBeer worksheet)
    |> Seq.toList

let rowToTaster (worksheet: ExcelWorksheet) (row: int) : Taster = {
    Name = worksheet.Cells.[row, 1].Text
    Email = worksheet.Cells.[row, 2].Text
    BirthYear = worksheet.Cells.[row, 3].Text |> int
}

let readTasters (fileName: string) =
    use package = new ExcelPackage(fileName)
    let worksheet = package.Workbook.Worksheets.["Tasters"]

    seq { 2 .. worksheet.Dimension.End.Row }
    |> Seq.map (rowToTaster worksheet)
    |> Seq.toList
