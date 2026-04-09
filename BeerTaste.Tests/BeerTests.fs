module BeerTaste.Tests.BeerTests

open Xunit
open BeerTaste.Common
open BeerTaste.Tests.TestHelpers

module PricePerLiterTests =
    [<Fact>]
    let ``PricePerLiter divides price by volume`` () =
        let beer = makeBeer 1 "Lager" "BrewCo" 4.5 30.0
        // Volume defaults to 0.5 in makeBeer
        Assert.Equal(60.0, beer.PricePerLiter, 6)

    [<Fact>]
    let ``PricePerLiter scales correctly with different volume`` () =
        let beer = {
            makeBeer 1 "Lager" "BrewCo" 4.5 30.0 with
                Volume = 1.0
        }

        Assert.Equal(30.0, beer.PricePerLiter, 6)

    [<Fact>]
    let ``PricePerLiter scales correctly with higher price`` () =
        let beer = {
            makeBeer 1 "Stout" "DarkCo" 6.0 75.0 with
                Volume = 0.5
        }

        Assert.Equal(150.0, beer.PricePerLiter, 6)

module PricePerAbvTests =
    [<Fact>]
    let ``PricePerAbv equals PricePerLiter divided by ABV fraction`` () =
        // makeBeer creates beer with Volume=0.5
        // PricePerLiter = 30.0/0.5 = 60.0
        // PricePerAbv = 60.0 / (4.5/100) = 60.0 / 0.045 ≈ 1333.33
        let beer = makeBeer 1 "Lager" "BrewCo" 4.5 30.0
        let expected = beer.PricePerLiter / (beer.ABV / 100.0)
        Assert.Equal(expected, beer.PricePerAbv, 6)

    [<Fact>]
    let ``PricePerAbv is higher for lower-ABV beer at same price`` () =
        let weakBeer = makeBeer 1 "Session" "BrewCo" 3.5 30.0
        let strongBeer = makeBeer 2 "IPA" "BrewCo" 7.0 30.0
        // Same price, same volume → stronger beer has lower price-per-abv (better value per unit alcohol)
        Assert.True(strongBeer.PricePerAbv < weakBeer.PricePerAbv)

    [<Fact>]
    let ``PricePerAbv is lower for same-ABV beer at lower price`` () =
        let cheapBeer = makeBeer 1 "Basic" "BrewCo" 5.0 20.0
        let expensiveBeer = makeBeer 2 "Craft" "ArtCo" 5.0 50.0
        Assert.True(cheapBeer.PricePerAbv < expensiveBeer.PricePerAbv)
