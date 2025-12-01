namespace BeerTaste.Common

open System.Threading.Tasks
open Azure.Data.Tables

/// <summary>
/// Represents a taster's score for a specific beer.
/// ScoreValue is optional to handle missing or incomplete ratings.
/// </summary>
type Score = {
    /// The ID of the beer being rated
    BeerId: int
    /// Name of the taster providing the score
    TasterName: string
    /// Optional integer score (typically 1-10). None indicates missing/incomplete data.
    ScoreValue: int option
} with
    /// Composite row key for Azure Table Storage: "BeerId|TasterName"
    member this.RowKey = $"{this.BeerId}|{this.TasterName}"

/// <summary>
/// Azure Table Storage operations for score data.
/// Handles conversion between Score domain type and TableEntity, CRUD operations,
/// and validation functions (hasScores, isComplete).
/// </summary>
module Scores =
    /// <summary>Converts an Azure TableEntity to a Score domain object.</summary>
    let entityToScore (entity: TableEntity) : Score = {
        BeerId =
            entity.GetInt32("BeerId")
            |> Option.ofNullable
            |> Option.get
        TasterName = entity.GetString("TasterName")
        ScoreValue = entity.GetInt32("ScoreValue") |> Option.ofNullable
    }

    let scoreToEntity (beerTasteGuid: string) (score: Score) : TableEntity =
        let entity = TableEntity(beerTasteGuid, score.RowKey)
        entity.Add("BeerId", score.BeerId)
        entity.Add("TasterName", score.TasterName)
        entity.Add("ScoreValue", score.ScoreValue |> Option.toNullable)
        entity

    let deleteScoresForBeerTasteAsync (scoresTable: TableClient) (beerTasteGuid: string) : Task =
        task {
            let deleteTasks =
                scoresTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
                |> Seq.map (fun e -> scoresTable.DeleteEntityAsync(e.PartitionKey, e.RowKey) :> Task)
                |> Seq.toArray

            do! Task.WhenAll(deleteTasks)
        }

    let addScoresAsync (scoresTable: TableClient) (beerTasteGuid: string) (scores: Score list) : Task =
        task {
            let entities = scores |> List.map (scoreToEntity beerTasteGuid)

            // Azure Table Storage supports up to 100 entities per batch transaction
            let batches = entities |> List.chunkBySize 100

            for batch in batches do
                let actions =
                    batch
                    |> List.map (fun entity -> TableTransactionAction(TableTransactionActionType.Add, entity))

                let! _ = scoresTable.SubmitTransactionAsync(actions)
                ()
        }

    let fetchScores (storage: BeerTasteTableStorage) (beerTasteGuid: string) : Score list =
        try
            storage.ScoresTableClient.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.map entityToScore
            |> Seq.toList
        with _ -> []

    let hasScores (scores: Score list) : bool =
        scores
        |> List.filter (fun s -> s.ScoreValue |> Option.isSome)
        |> List.length
        |> (>) 0

    let isComplete (scores: Score list) : bool =
        scores
        |> List.filter (fun s -> s.ScoreValue |> Option.isNone)
        |> List.length
        |> (=) 0
