namespace BeerTaste.Common

open Azure.Data.Tables

type Score = {
    BeerId: int
    TasterName: string
    ScoreValue: int option
} with
    member this.RowKey = $"{this.BeerId}|{this.TasterName}"

module Scores =
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

    let deleteScoresForBeerTaste (scoresTable: TableClient) (beerTasteGuid: string) : unit =
        try
            scoresTable.Query<TableEntity>(filter = $"PartitionKey eq '{beerTasteGuid}'")
            |> Seq.iter (scoresTable.DeleteEntity >> ignore)
        with _ ->
            ()

    let addScores (scoresTable: TableClient) (beerTasteGuid: string) (scores: Score list) : unit =
        scores
        |> List.map (scoreToEntity beerTasteGuid)
        |> List.iter (scoresTable.AddEntity >> ignore)

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
