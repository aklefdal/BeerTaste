namespace BeerTaste.Common

open System
open Azure.Data.Tables
open Azure

type Score = {
    BeerId: int
    TasterName: string
    ScoreValue: int
} with
    member _.RowKey = $"{_.BeerId}|{_.TasterName}"

type ScoreEntity() =
    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable<DateTimeOffset>() with get, set
        member val ETag = ETag() with get, set

    member val BeerId = 0 with get, set
    member val TasterName = "" with get, set
    member val ScoreValue = 0 with get, set

    new(partitionKey: string, rowKey: string, score: Score) as this =
        ScoreEntity()

        then
            (this :> ITableEntity).PartitionKey <- partitionKey
            (this :> ITableEntity).RowKey <- rowKey
            this.BeerId <- score.BeerId
            this.TasterName <- score.TasterName
            this.ScoreValue <- score.ScoreValue

module ScoresStorage =
    let deleteScoresForBeerTaste (scoresTable: TableClient) (partitionKey: string) : unit =
        try
            let query =
                scoresTable.Query<ScoreEntity>(filter = $"PartitionKey eq '{partitionKey}'")

            for entity in query do
                scoresTable.DeleteEntity(entity) |> ignore
        with _ ->
            ()

    let addScores (scoresTable: TableClient) (partitionKey: string) (scores: Score list) : unit =
        scores
        |> List.iter (fun score ->
            let rowKey = score.RowKey
            let entity = ScoreEntity(partitionKey, rowKey, score)
            scoresTable.AddEntity(entity) |> ignore)
