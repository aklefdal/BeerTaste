module BeerTaste.Tests.CorrelationTests

open Xunit
open BeerTaste.Common
open BeerTaste.Tests.TestHelpers

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

module CorrelationToAbvTests =
    [<Fact>]
    let ``correlationToAbv returns empty list for empty tasters`` () =
        let beers = [ makeBeer 1 "Lager" "BrewCo" 4.5 30.0 ]
        let result = Results.correlationToAbv beers [] []
        Assert.Empty(result)

    [<Fact>]
    let ``correlationToAbv gives positive correlation when taster prefers high-ABV beers`` () =
        let beers = [
            makeBeer 1 "Lager" "BrewCo" 4.0 30.0
            makeBeer 2 "IPA" "HopCo" 6.5 50.0
            makeBeer 3 "Imperial" "StrCo" 9.0 70.0
        ]

        let tasters = [ makeTaster "Alice" None ]

        // Alice gives higher scores to higher-ABV beers → positive correlation
        let scores = [
            makeScore 1 "Alice" (Some 3)
            makeScore 2 "Alice" (Some 6)
            makeScore 3 "Alice" (Some 9)
        ]

        let result = Results.correlationToAbv beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.True(result[0].Value > 0.0)

    [<Fact>]
    let ``correlationToAbv gives negative correlation when taster prefers low-ABV beers`` () =
        let beers = [
            makeBeer 1 "Lager" "BrewCo" 4.0 30.0
            makeBeer 2 "IPA" "HopCo" 6.5 50.0
            makeBeer 3 "Imperial" "StrCo" 9.0 70.0
        ]

        let tasters = [ makeTaster "Bob" None ]

        // Bob gives lower scores to higher-ABV beers → negative correlation
        let scores = [
            makeScore 1 "Bob" (Some 9)
            makeScore 2 "Bob" (Some 6)
            makeScore 3 "Bob" (Some 3)
        ]

        let result = Results.correlationToAbv beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.True(result[0].Value < 0.0)

    [<Fact>]
    let ``correlationToAbv is sorted by descending correlation`` () =
        let beers = [
            makeBeer 1 "Lager" "BrewCo" 4.0 30.0
            makeBeer 2 "IPA" "HopCo" 6.5 50.0
            makeBeer 3 "Imperial" "StrCo" 9.0 70.0
        ]

        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        // Alice prefers strong; Bob prefers weak
        let scores = [
            makeScore 1 "Alice" (Some 3)
            makeScore 2 "Alice" (Some 6)
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Bob" (Some 9)
            makeScore 2 "Bob" (Some 6)
            makeScore 3 "Bob" (Some 3)
        ]

        let result = Results.correlationToAbv beers tasters scores
        Assert.Equal(2, result.Length)
        Assert.True(result[0].Value >= result[1].Value)

module CorrelationToAbvPriceTests =
    [<Fact>]
    let ``correlationToAbvPrice returns empty list for empty tasters`` () =
        let beers = [ makeBeer 1 "Lager" "BrewCo" 4.5 30.0 ]
        let result = Results.correlationToAbvPrice beers [] []
        Assert.Empty(result)

    [<Fact>]
    let ``correlationToAbvPrice is sorted by descending correlation`` () =
        // Beer PricePerAbv = (Price/Volume)/(ABV/100)
        // Beer1: (30/0.5)/(4.0/100) = 60/0.04 = 1500
        // Beer2: (50/0.5)/(6.5/100) = 100/0.065 ≈ 1538
        // Beer3: (70/0.5)/(9.0/100) = 140/0.09 ≈ 1556
        let beers = [
            makeBeer 1 "Lager" "BrewCo" 4.0 30.0
            makeBeer 2 "IPA" "HopCo" 6.5 50.0
            makeBeer 3 "Imperial" "StrCo" 9.0 70.0
        ]

        let tasters = [
            makeTaster "Alice" None
            makeTaster "Bob" None
        ]

        // Alice scores positively correlated with PricePerAbv; Bob negatively
        let scores = [
            makeScore 1 "Alice" (Some 3)
            makeScore 2 "Alice" (Some 6)
            makeScore 3 "Alice" (Some 9)
            makeScore 1 "Bob" (Some 9)
            makeScore 2 "Bob" (Some 6)
            makeScore 3 "Bob" (Some 3)
        ]

        let result = Results.correlationToAbvPrice beers tasters scores
        Assert.Equal(2, result.Length)
        Assert.True(result[0].Value >= result[1].Value)

module CorrelationToAgeTests =
    [<Fact>]
    let ``correlationToAge returns 0 for beers with fewer than 3 scored taster ages`` () =
        let beers = [ makeBeer 1 "Lager" "BrewCo" 4.5 30.0 ]

        let tasters = [
            makeTaster "Alice" (Some 1990)
            makeTaster "Bob" (Some 1975)
        ]

        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 1 "Bob" (Some 5)
        ]

        let result = Results.correlationToAge beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.Equal(0.0, result[0].Value)

    [<Fact>]
    let ``correlationToAge skips tasters without birth year`` () =
        let beers = [ makeBeer 1 "Lager" "BrewCo" 4.5 30.0 ]

        // Only 1 taster has a birth year, so fewer than 3 data points → 0.0
        let tasters = [
            makeTaster "Alice" (Some 1990)
            makeTaster "Bob" None
            makeTaster "Carol" None
        ]

        let scores = [
            makeScore 1 "Alice" (Some 7)
            makeScore 1 "Bob" (Some 5)
            makeScore 1 "Carol" (Some 8)
        ]

        let result = Results.correlationToAge beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.Equal(0.0, result[0].Value)

    [<Fact>]
    let ``correlationToAge computes positive correlation when older tasters prefer a beer`` () =
        let beers = [ makeBeer 1 "Lager" "BrewCo" 4.5 30.0 ]

        let tasters = [
            makeTaster "Young" (Some 2000)
            makeTaster "Middle" (Some 1980)
            makeTaster "Old" (Some 1960)
        ]

        // Older tasters give higher scores → positive correlation
        let scores = [
            makeScore 1 "Young" (Some 3)
            makeScore 1 "Middle" (Some 6)
            makeScore 1 "Old" (Some 9)
        ]

        let result = Results.correlationToAge beers tasters scores
        Assert.Equal(1, result.Length)
        Assert.True(result[0].Value > 0.0)

    [<Fact>]
    let ``correlationToAge is sorted by descending correlation`` () =
        let beers = [
            makeBeer 1 "OldManBeer" "TradCo" 4.5 30.0
            makeBeer 2 "YouthBeer" "TrendCo" 5.0 35.0
        ]

        let tasters = [
            makeTaster "Young" (Some 2000)
            makeTaster "Middle" (Some 1980)
            makeTaster "Old" (Some 1960)
        ]

        // Beer1: older tasters prefer it → high positive correlation
        // Beer2: younger tasters prefer it → negative correlation
        let scores = [
            makeScore 1 "Young" (Some 2)
            makeScore 1 "Middle" (Some 5)
            makeScore 1 "Old" (Some 9)
            makeScore 2 "Young" (Some 9)
            makeScore 2 "Middle" (Some 5)
            makeScore 2 "Old" (Some 2)
        ]

        let result = Results.correlationToAge beers tasters scores
        Assert.Equal(2, result.Length)
        Assert.True(result[0].Value >= result[1].Value)
