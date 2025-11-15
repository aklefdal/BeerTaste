module BeerTaste.Web.Templates.ResultsIndex

open Oxpecker.ViewEngine
open BeerTaste.Web.Templates.Layout

let view (beerTasteGuid: string) =
    layout "Beer Tasting Results" [
        h1() { raw "Beer Tasting Results" }

        div(class'="nav") {
            h2() { raw "Available Results" }
            a(href = $"/results/{beerTasteGuid}/bestbeers") { raw "Best Beers" }
            a(href = $"/results/{beerTasteGuid}/controversial") { raw "Most Controversial Beers" }
            a(href = $"/results/{beerTasteGuid}/deviant") { raw "Most Deviant Tasters" }
            a(href = $"/results/{beerTasteGuid}/similar") { raw "Most Similar Tasters" }
            a(href = $"/results/{beerTasteGuid}/strongbeers") { raw "Most Fond of Strong Beers" }
            a(href = $"/results/{beerTasteGuid}/cheapalcohol") { raw "Most Fond of Cheap Alcohol" }
        }
    ]
