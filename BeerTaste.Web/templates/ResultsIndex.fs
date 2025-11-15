module BeerTaste.Web.Templates.ResultsIndex

open Oxpecker.ViewEngine
open BeerTaste.Web.Templates.Layout

let view (beerTasteGuid: string) =
    layout "Beer Tasting Results" [
        h1 () { raw "Beer Tasting Results" }

        h2 () { raw "Available Results" }

        div (class' = "results-list") {
            a (href = $"/results/{beerTasteGuid}/bestbeers") {
                span (class' = "icon") { raw "★" }
                raw "Best Beers"
            }
            a (href = $"/results/{beerTasteGuid}/controversial") {
                span (class' = "icon") { raw "⚡" }
                raw "Most Controversial Beers"
            }
            a (href = $"/results/{beerTasteGuid}/deviant") {
                span (class' = "icon") { raw "😈" }
                raw "Most Deviant Tasters"
            }
            a (href = $"/results/{beerTasteGuid}/similar") {
                span (class' = "icon") { raw "❤" }
                raw "Most Similar Tasters"
            }
            a (href = $"/results/{beerTasteGuid}/strongbeers") {
                span (class' = "icon") { raw "😵" }
                raw "Most Fond of Strong Beers"
            }
            a (href = $"/results/{beerTasteGuid}/cheapalcohol") {
                span (class' = "icon") { raw "💰" }
                raw "Most Fond of Cheap Alcohol"
            }
        }
    ]
