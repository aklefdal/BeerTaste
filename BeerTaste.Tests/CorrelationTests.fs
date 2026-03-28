module BeerTaste.Tests.CorrelationTests

open Xunit
open BeerTaste.Common

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

let private makeScore beerId tasterName scoreValue : Score = {
    BeerId = beerId
    TasterName = tasterName
    ScoreValue = scoreValue
}

let private makeTaster name birthYear : Taster = {
    Name = name
    Email = None
    BirthYear = birthYear
}

module CorrelationBetweenTastersTests =
    [<Fact>]
    let ``correlationBetweenTasters gives correlation 1 for identical scores`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        // Alice and Bob gave identical scores to all beers
        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 2 "Alice" (Some 5)
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Bob" (Some 8)
            makeScore 2 "Bob" (Some 5)
            makeScore 3 "Bob" (Some 9)
        ]

        let result = Results.correlationBetweenTasters tasters scores
        Assert.Equal(1, result.Length)
        Assert.Equal(1.0, result[0].Value, 6)

    [<Fact>]
    let ``correlationBetweenTasters gives negative correlation for opposite scores`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        // Alice and Bob gave perfectly reversed scores
        let scores = [
            makeScore 1 "Alice" (Some 2)
            makeScore 2 "Alice" (Some 5)
            makeScore 3 "Alice" (Some 8)
            makeScore 1 "Bob" (Some 8)
            makeScore 2 "Bob" (Some 5)
            makeScore 3 "Bob" (Some 2)
        ]

        let result = Results.correlationBetweenTasters tasters scores
        Assert.Equal(1, result.Length)
        Assert.True(result[0].Value < 0.0)

    [<Fact>]
    let ``correlationBetweenTasters returns all taster pairs`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
            makeTaster "Carol" None
        ]

        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 1 "Bob" (Some 7)
            makeScore 1 "Carol" (Some 7)
        ]

        let result = Results.correlationBetweenTasters tasters scores
        Assert.Equal(3, result.Length)

    [<Fact>]
    let ``correlationBetweenTasters is sorted by descending correlation`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
            makeTaster "Carol" None
        ]

        // Alice and Bob agree; Carol is opposite of Alice
        let scores = [
            makeScore 1 "Alice" (Some 2)
            makeScore 2 "Alice" (Some 5)
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Bob" (Some 2)
            makeScore 2 "Bob" (Some 5)
            makeScore 3 "Bob" (Some 9)
            makeScore 1 "Carol" (Some 9)
            makeScore 2 "Carol" (Some 5)
            makeScore 3 "Carol" (Some 2)
        ]

        let result = Results.correlationBetweenTasters tasters scores

        // First pair should have the highest correlation
        for i in 1 .. result.Length - 1 do
            Assert.True(result[i - 1].Value >= result[i].Value)

module CorrelationToAveragesTests =
    [<Fact>]
    let ``correlationToAverages gives 1 when taster scores match averages`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
            makeBeer 2 "IPA" "HopCo" 6.5 55.0
            makeBeer 3 "Stout" "DarkCo" 7.0 65.0
        ]

        let tasters = [ makeTaster "Alice" None ]

        // Alice is the only taster, so her scores ARE the averages
        let scores = [
            makeScore 1 "Alice" (Some 6)
            makeScore 2 "Alice" (Some 8)
            makeScore 3 "Alice" (Some 4)
        ]

        let result = Results.correlationToAverages beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.Equal(1.0, result[0].Value, 6)

    [<Fact>]
    let ``correlationToAverages is sorted ascending (most deviant first)`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
            makeBeer 2 "IPA" "HopCo" 6.5 55.0
            makeBeer 3 "Stout" "DarkCo" 7.0 65.0
        ]

        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        // Bob agrees with the average; Alice is opposite
        let scores = [
            makeScore 1 "Alice" (Some 9)
            makeScore 2 "Alice" (Some 5)
            makeScore 3 "Alice" (Some 2)
            makeScore 1 "Bob" (Some 2)
            makeScore 2 "Bob" (Some 5)
            makeScore 3 "Bob" (Some 9)
        ]

        let result = Results.correlationToAverages beers tasters scores
        Assert.Equal(2, result.Length)
        // Most deviant (lowest correlation) should be first
        Assert.True(result[0].Value <= result[1].Value)

module GetScoresForBeerTests =
    [<Fact>]
    let ``getScoresForBeer returns scores sorted by taster name`` () =
        let scores = [
            makeScore 1 "Zara" (Some 9)
            makeScore 1 "Alice" (Some 5)
            makeScore 1 "Bob" (Some 7)
        ]

        let result = Results.getScoresForBeer scores 1
        // Sorted by taster name: Alice=5, Bob=7, Zara=9
        Assert.Equal(3, result.Length)
        Assert.Equal(5.0, result[0])
        Assert.Equal(7.0, result[1])
        Assert.Equal(9.0, result[2])

    [<Fact>]
    let ``getScoresForBeer treats None scores as 0`` () =
        let scores = [
            makeScore 1 "Alice" (Some 6)
            makeScore 1 "Bob" None
        ]

        let result = Results.getScoresForBeer scores 1
        Assert.Equal(2, result.Length)
        Assert.Contains(0.0, result)
        Assert.Contains(6.0, result)

    [<Fact>]
    let ``getScoresForBeer ignores scores for other beers`` () =
        let scores = [
            makeScore 1 "Alice" (Some 6)
            makeScore 2 "Alice" (Some 9)
        ]

        let result = Results.getScoresForBeer scores 1
        Assert.Equal(1, result.Length)
        Assert.Equal(6.0, result[0])

module GetScoresForTasterTests =
    [<Fact>]
    let ``getScoresForTaster returns scores sorted by beer ID`` () =
        let scores = [
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Alice" (Some 5)
            makeScore 2 "Alice" (Some 7)
        ]

        let result = Results.getScoresForTaster "Alice" scores
        // Sorted by beer ID: beer1=5, beer2=7, beer3=9
        Assert.Equal(3, result.Length)
        Assert.Equal(5.0, result[0])
        Assert.Equal(7.0, result[1])
        Assert.Equal(9.0, result[2])

    [<Fact>]
    let ``getScoresForTaster ignores scores for other tasters`` () =
        let scores = [
            makeScore 1 "Alice" (Some 6)
            makeScore 1 "Bob" (Some 9)
        ]

        let result = Results.getScoresForTaster "Alice" scores
        Assert.Equal(1, result.Length)
        Assert.Equal(6.0, result[0])
