namespace BeerTaste.Common

open System
open Azure.Data.Tables
open Azure

type Score = {
    BeerId: int
    TasterName: string
    ScoreValue: float
} with
    member this.RowKey = $"{this.BeerId}|{this.TasterName}"

type ScoreEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val BeerId = 0 with get, set
    member val TasterName = "" with get, set
    member val ScoreValue = 0.0 with get, set

    new(beerTasteGuid: string, score: Score) as this =
        ScoreEntity()

        then
            (this :> ITableEntity).PartitionKey <- beerTasteGuid
            (this :> ITableEntity).RowKey <- score.RowKey
            this.BeerId <- score.BeerId
            this.TasterName <- score.TasterName
            this.ScoreValue <- score.ScoreValue

module ScoresStorage =
    let deleteScoresForBeerTaste (scoresTable: TableClient) (beerTasteGuid: string) : unit =
        try
            scoresTable.Query<ScoreEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.iter (scoresTable.DeleteEntity >> ignore)
        with _ ->
            ()

    let addScores (scoresTable: TableClient) (beerTasteGuid: string) (scores: Score list) : unit =
        scores
        |> List.iter (fun score ->
            let entity = ScoreEntity(beerTasteGuid, score)
            scoresTable.AddEntity(entity) |> ignore)
