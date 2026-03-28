module BeerTaste.Tests.ScoresTests

open Xunit
open BeerTaste.Common

let private makeScore beerId tasterName scoreValue : Score = {
    BeerId = beerId
    TasterName = tasterName
    ScoreValue = scoreValue
}

module HasScoresTests =
    [<Fact>]
    let ``hasScores returns false for empty list`` () =
        let scores = []
        Assert.False(Scores.hasScores scores)

    [<Fact>]
    let ``hasScores returns false when all scores are None`` () =
        let scores = [
            makeScore 1 "Alice" None
            makeScore 2 "Alice" None
        ]

        Assert.False(Scores.hasScores scores)

    [<Fact>]
    let ``hasScores returns true when at least one score has a value`` () =
        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 2 "Alice" None
        ]

        Assert.True(Scores.hasScores scores)

    [<Fact>]
    let ``hasScores returns true when all scores have values`` () =
        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 2 "Alice" (Some 8)
        ]

        Assert.True(Scores.hasScores scores)

module IsCompleteTests =
    [<Fact>]
    let ``isComplete returns true for empty list`` () =
        let scores = []
        Assert.True(Scores.isComplete scores)

    [<Fact>]
    let ``isComplete returns false when any score is None`` () =
        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 2 "Alice" None
        ]

        Assert.False(Scores.isComplete scores)

    [<Fact>]
    let ``isComplete returns true when all scores have values`` () =
        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 2 "Alice" (Some 8)
        ]

        Assert.True(Scores.isComplete scores)

    [<Fact>]
    let ``isComplete returns false when all scores are None`` () =
        let scores = [
            makeScore 1 "Alice" None
            makeScore 2 "Alice" None
        ]

        Assert.False(Scores.isComplete scores)
