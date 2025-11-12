module BeerTaste.Console.Tasters

open Spectre.Console
open System
open Azure.Data.Tables
open Azure
open OfficeOpenXml

// Taster type definition
type Taster = {
    Name: string
    Email: string
    BirthYear: int
}

// Taster entity type for Azure Table Storage
type TasterEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val Name = "" with get, set
    member val Email = "" with get, set
    member val BirthYear = 0 with get, set

    new(partitionKey: string, rowKey: string, taster: Taster) as this =
        TasterEntity()
        then
            (this :> ITableEntity).PartitionKey <- partitionKey
            (this :> ITableEntity).RowKey <- rowKey
            this.Name <- taster.Name
            this.Email <- taster.Email
            this.BirthYear <- taster.BirthYear

// Delete all tasters for a given partition key
let deleteTastersForPartitionKey (tastersTable: TableClient) (partitionKey: string) : unit =
    try
        let query = tastersTable.Query<TasterEntity>(filter = $"PartitionKey eq '{partitionKey}'")
        for entity in query do
            tastersTable.DeleteEntity(entity) |> ignore
    with
    | _ -> ()

let addTasters (tastersTable: TableClient) (partitionKey: string) (tasters: Taster list) : unit =
    tasters
    |> List.iter (fun taster ->
        let rowKey = taster.Name
        let entity = TasterEntity(partitionKey, rowKey, taster)
        tastersTable.AddEntity(entity) |> ignore)


let rowToTaster (worksheet: ExcelWorksheet) (row: int) : Taster = {
    Name = worksheet.Cells[row, 1].Text
    Email = worksheet.Cells[row, 2].Text
    BirthYear = worksheet.Cells[row, 3].Text |> int
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

