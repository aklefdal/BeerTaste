module BeerTaste.Tests.ResultsTests

open Xunit
open BeerTaste.Common
open BeerTaste.Tests.TestHelpers

module CombineAllTastersTests =
    [<Fact>]
    let ``combineAllTasters returns empty list for empty input`` () =
        let result = Results.combineAllTasters []
        Assert.Empty(result)

    [<Fact>]
    let ``combineAllTasters returns empty list for single taster`` () =
        let tasters = [ makeTaster "Alice" None ]
        let result = Results.combineAllTasters tasters
        Assert.Empty(result)

    [<Fact>]
    let ``combineAllTasters returns one pair for two tasters`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        let result = Results.combineAllTasters tasters
        Assert.Equal(1, result.Length)
        Assert.Contains(("Alice", "Bob"), result)

    [<Fact>]
    let ``combineAllTasters returns three pairs for three tasters`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
            makeTaster "Carol" None
        ]

        let result = Results.combineAllTasters tasters
        Assert.Equal(3, result.Length)

    [<Fact>]
    let ``combineAllTasters does not generate self-pairs`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        let result = Results.combineAllTasters tasters

        for pair in result do
            Assert.NotEqual<string>(fst pair, snd pair)

    [<Fact>]
    let ``combineAllTasters does not generate duplicate pairs`` () =
        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
            makeTaster "Carol" None
        ]

        let result = Results.combineAllTasters tasters
        let unique = result |> List.distinct
        Assert.Equal(result.Length, unique.Length)

    [<Fact>]
    let ``combineAllTasters always puts alphabetically smaller name first`` () =
        let tasters = [
            makeTaster "Bob" None
            makeTaster "Alice" None
        ]

        let result = Results.combineAllTasters tasters
        Assert.Equal(1, result.Length)
        Assert.Contains(("Alice", "Bob"), result)

module BeerAveragesTests =
    [<Fact>]
    let ``beerAverages returns 0 for beer with no scores`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
        ]

        let scores = []
        let result = Results.beerAverages beers scores
        Assert.Equal(1, result.Length)
        Assert.Equal(0.0, result[0].Value)

    [<Fact>]
    let ``beerAverages computes correct average`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
        ]

        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 1 "Bob" (Some 6)
        ]

        let result = Results.beerAverages beers scores
        Assert.Equal(1, result.Length)
        Assert.Equal(7.0, result[0].Value)

    [<Fact>]
    let ``beerAverages treats missing scores as 0`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
        ]

        let scores = [
            makeScore 1 "Alice" (Some 8)
            makeScore 1 "Bob" None
        ]

        let result = Results.beerAverages beers scores
        Assert.Equal(1, result.Length)
        Assert.Equal(4.0, result[0].Value)

    [<Fact>]
    let ``beerAverages sorts by descending average`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
            makeBeer 2 "IPA" "HopCo" 6.5 55.0
        ]

        let scores = [
            makeScore 1 "Alice" (Some 5)
            makeScore 2 "Alice" (Some 9)
        ]

        let result = Results.beerAverages beers scores
        Assert.Equal(2, result.Length)
        Assert.Equal(9.0, result[0].Value)
        Assert.Equal(5.0, result[1].Value)

module BeerStandardDeviationsTests =
    [<Fact>]
    let ``beerStandardDeviations returns 0 for beer with no scores`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
        ]

        let scores = []
        let result = Results.beerStandardDeviations beers scores
        Assert.Equal(1, result.Length)
        Assert.Equal(0.0, result[0].Value)

    [<Fact>]
    let ``beerStandardDeviations is higher for more varied scores`` () =
        let beers = [
            makeBeer 1 "Consistent" "BrewCo" 4.7 35.0
            makeBeer 2 "Controversial" "HopCo" 6.5 55.0
        ]

        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 1 "Bob" (Some 7)
            makeScore 2 "Alice" (Some 2)
            makeScore 2 "Bob" (Some 10)
        ]

        let result = Results.beerStandardDeviations beers scores

        let controversialStdDev =
            result
            |> List.find (fun r -> r.Name.Contains("Controversial"))

        let consistentStdDev =
            result
            |> List.find (fun r -> r.Name.Contains("Consistent"))

        Assert.True(controversialStdDev.Value > consistentStdDev.Value)

    [<Fact>]
    let ``beerStandardDeviations includes correct average in result`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
        ]

        let scores = [
            makeScore 1 "Alice" (Some 6)
            makeScore 1 "Bob" (Some 8)
            makeScore 1 "Carol" (Some 10)
        ]

        let result = Results.beerStandardDeviations beers scores
        Assert.Equal(1, result.Length)
        Assert.Equal(8.0, result[0].Average, 6)

    [<Fact>]
    let ``beerStandardDeviations returns 0 average for beer with no scores`` () =
        let beers = [
            makeBeer 1 "Pilsner" "BrewCo" 4.7 35.0
        ]

        let result = Results.beerStandardDeviations beers []
        Assert.Equal(1, result.Length)
        Assert.Equal(0.0, result[0].Average)
