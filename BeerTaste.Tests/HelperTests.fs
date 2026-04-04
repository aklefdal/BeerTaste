module BeerTaste.Tests.HelperTests

open Xunit
open BeerTaste.Common

let private makeScore beerId tasterName scoreValue : Score = {
    BeerId = beerId
    TasterName = tasterName
    ScoreValue = scoreValue
}

let private makeBeer id name producer abv price : Beer = {
    Id = id
    Name = name
    BeerType = "Lager"
    Origin = "Norway"
    Producer = producer
    ABV = abv
    Volume = 0.5
    Price = price
    Packaging = "Can"
}

let private makeTaster name birthYear : Taster = {
    Name = name
    Email = None
    BirthYear = birthYear
}

module GetScoresForTasterTests =
    [<Fact>]
    let ``getScoresForTaster returns empty array when no scores exist`` () =
        let scores = []
        let result = Results.getScoresForTaster "Alice" scores
        Assert.Empty(result)

    [<Fact>]
    let ``getScoresForTaster returns only scores for the given taster`` () =
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 1 "Bob" (Some 6)
            makeScore 2 "Alice" (Some 7)
        ]

        let result = Results.getScoresForTaster "Alice" scores
        Assert.Equal(2, result.Length)

    [<Fact>]
    let ``getScoresForTaster sorts by beer ID ascending`` () =
        let scores = [
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Alice" (Some 7)
            makeScore 2 "Alice" (Some 5)
        ]

        let result = Results.getScoresForTaster "Alice" scores
        Assert.Equal<float array>([| 7.0; 5.0; 9.0 |], result)

    [<Fact>]
    let ``getScoresForTaster treats None score as 0`` () =
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 2 "Alice" None
        ]

        let result = Results.getScoresForTaster "Alice" scores
        Assert.Equal<float array>([| 8.0; 0.0 |], result)

module GetScoresForBeerTests =
    [<Fact>]
    let ``getScoresForBeer returns empty array when no scores exist`` () =
        let scores = []
        let result = Results.getScoresForBeer scores 1
        Assert.Empty(result)

    [<Fact>]
    let ``getScoresForBeer returns only scores for the given beer`` () =
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 2 "Alice" (Some 6)
            makeScore 1 "Bob" (Some 7)
        ]

        let result = Results.getScoresForBeer scores 1
        Assert.Equal(2, result.Length)

    [<Fact>]
    let ``getScoresForBeer sorts by taster name ascending`` () =
        let scores = [
            makeScore 1 "Charlie" (Some 9)
            makeScore 1 "Alice" (Some 7)
            makeScore 1 "Bob" (Some 5)
        ]

        let result = Results.getScoresForBeer scores 1
        Assert.Equal<float array>([| 7.0; 5.0; 9.0 |], result)

    [<Fact>]
    let ``getScoresForBeer treats None score as 0`` () =
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 1 "Bob" None
        ]

        let result = Results.getScoresForBeer scores 1
        Assert.Equal<float array>([| 8.0; 0.0 |], result)

module GetAverageScoreForBeerTests =
    [<Fact>]
    let ``getAverageScoreForBeer returns 0 for empty scores`` () =
        let result = Results.getAverageScoreForBeer [] 1
        Assert.Equal(0.0, result)

    [<Fact>]
    let ``getAverageScoreForBeer computes correct average`` () =
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 1 "Bob" (Some 6)
        ]

        let result = Results.getAverageScoreForBeer scores 1
        Assert.Equal(7.0, result)

    [<Fact>]
    let ``getAverageScoreForBeer treats None as 0 in average`` () =
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 1 "Bob" None
        ]

        let result = Results.getAverageScoreForBeer scores 1
        Assert.Equal(4.0, result)

module CorrelationToAveragesTests =
    [<Fact>]
    let ``correlationToAverages returns correlation 1 when taster matches average exactly`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
            makeBeer 2 "IPA" "HopCo" 6.5 55.0
            makeBeer 3 "Stout" "DarkCo" 5.0 45.0
        ]

        let tasters = [ makeTaster "Alice" None ]

        // Alice's scores are also the average (only taster)
        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 2 "Alice" (Some 5)
            makeScore 3 "Alice" (Some 9)
        ]

        let result = Results.correlationToAverages beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.Equal(1.0, result[0].Value, 6)

    [<Fact>]
    let ``correlationToAverages returns lower value for taster deviating from average`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
            makeBeer 2 "IPA" "HopCo" 6.5 55.0
            makeBeer 3 "Stout" "DarkCo" 5.0 45.0
        ]

        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        // Alice and Bob have opposite tastes
        let scores = [
            makeScore 1 "Alice" (Some 3)
            makeScore 2 "Alice" (Some 5)
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Bob" (Some 9)
            makeScore 2 "Bob" (Some 5)
            makeScore 3 "Bob" (Some 3)
        ]

        let result = Results.correlationToAverages beers tasters scores
        Assert.Equal(2, result.Length)
        // Both correlate less than 1; the average is the midpoint
        for r in result do
            Assert.True(r.Value < 1.0)

    [<Fact>]
    let ``correlationToAverages sorts by ascending value (most deviant first)`` () =
        let beers = [
            makeBeer 1 "Beer1" "BrewCo" 4.7 35.0
            makeBeer 2 "Beer2" "HopCo" 6.5 55.0
            makeBeer 3 "Beer3" "DarkCo" 5.0 45.0
        ]

        let tasters = [
            makeTaster "T1" None
            makeTaster "T2" None
            makeTaster "T3" None
        ]

        let scores = [
            makeScore 1 "T1" (Some 8)
            makeScore 2 "T1" (Some 6)
            makeScore 3 "T1" (Some 9)
            makeScore 1 "T2" (Some 7)
            makeScore 2 "T2" (Some 5)
            makeScore 3 "T2" (Some 8)
            makeScore 1 "T3" (Some 2)
            makeScore 2 "T3" (Some 9)
            makeScore 3 "T3" (Some 1)
        ]

        let result = Results.correlationToAverages beers tasters scores
        Assert.Equal(3, result.Length)
        // Sorted ascending: result[0].Value <= result[1].Value <= result[2].Value
        Assert.True(result[0].Value <= result[1].Value)
        Assert.True(result[1].Value <= result[2].Value)
