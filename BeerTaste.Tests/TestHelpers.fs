module BeerTaste.Tests.TestHelpers

open BeerTaste.Common

let makeScore beerId tasterName scoreValue : Score = {
    BeerId = beerId
    TasterName = tasterName
    ScoreValue = scoreValue
}

let makeBeer id name producer abv price : Beer = {
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

let makeTaster name birthYear : Taster = {
    Name = name
    Email = None
    BirthYear = birthYear
}
