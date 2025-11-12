module BeerTaste.Console.Beers

open System
open Azure.Data.Tables
open Azure
open OfficeOpenXml

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
}

type BeerEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Name = "" with get, set
    member val BeerType = "" with get, set
    member val Origin = "" with get, set
    member val Producer = "" with get, set
    member val ABV = 0.0 with get, set
    member val Volume = 0.0 with get, set
    member val Price = 0.0 with get, set
    member val Packaging = "" with get, set

    new(partitionKey: string, rowKey: string, beer: Beer) as this =
        BeerEntity()
        then
            (this :> ITableEntity).PartitionKey <- partitionKey
            (this :> ITableEntity).RowKey <- rowKey
            this.Name <- beer.Name
            this.BeerType <- beer.BeerType
            this.Origin <- beer.Origin
            this.Producer <- beer.Producer
            this.ABV <- beer.ABV
            this.Volume <- beer.Volume
            this.Price <- beer.Price
            this.Packaging <- beer.Packaging

let deleteBeersForBeerTaste (beersTable: TableClient) (partitionKey: string) : unit =
    try
        let query = beersTable.Query<BeerEntity>(filter = $"PartitionKey eq '{partitionKey}'")
        for entity in query do
            beersTable.DeleteEntity(entity) |> ignore
    with
    | _ -> ()

let addBeers (beersTable: TableClient) (partitionKey: string) (beers: Beer list) : unit =
    beers
    |> List.iter (fun beer ->
        let rowKey = beer.Id.ToString()
        let entity = BeerEntity(partitionKey, rowKey, beer)
        beersTable.AddEntity(entity) |> ignore)

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
    targetPackage.Workbook.Worksheets.Add(schemaName, templateWorksheet) |> ignore

    let worksheet = targetPackage.Workbook.Worksheets[schemaName]
    let height = worksheet.Row(3).Height

    worksheet.InsertRow(3, beers.Length - 1, 3)

    for i in 3 .. (beers.Length + 1) do
        worksheet.Row(i).Height <- height

    beers
    |> List.iteri (fun i beer ->
        worksheet.Cells[i + 3, 1].Value <- beer.Id
        worksheet.Cells[i + 3, 2].Value <- beer.Producer
        worksheet.Cells[i + 3, 3].Value <- beer.Name
        worksheet.Cells[i + 3, 4].Value <- beer.BeerType
        worksheet.Cells[i + 3, 5].Value <- beer.Origin
        worksheet.Cells[i + 3, 6].Value <- beer.ABV)

    targetPackage.Save()

