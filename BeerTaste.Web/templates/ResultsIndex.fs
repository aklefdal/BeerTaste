module BeerTaste.Web.Templates.ResultsIndex

open Oxpecker.ViewEngine
open BeerTaste.Web.Templates.Layout

let view (beerTasteGuid: string) =
    layout "Beer Tasting Results" beerTasteGuid [
        h1 () { raw "Beer Tasting Results" }

        h2 () { raw "Available Results" }

        div (class' = "results-list") {
            a (href = $"/{beerTasteGuid}/results/bestbeers") {
                span (class' = "icon") { raw "★" }
                raw "Best Beers"
            }

            a (href = $"/{beerTasteGuid}/results/controversial") {
                span (class' = "icon") { raw "⚡" }
                raw "Most Controversial Beers"
            }

            a (href = $"/{beerTasteGuid}/results/deviant") {
                span (class' = "icon") { raw "😈" }
                raw "Most Deviant Tasters"
            }

            a (href = $"/{beerTasteGuid}/results/similar") {
                span (class' = "icon") { raw "❤" }
                raw "Most Similar Tasters"
            }

            a (href = $"/{beerTasteGuid}/results/strongbeers") {
                span (class' = "icon") { raw "😵" }
                raw "Most Fond of Strong Beers"
            }

            a (href = $"/{beerTasteGuid}/results/cheapalcohol") {
                span (class' = "icon") { raw "💰" }
                raw "Most Fond of Cheap Alcohol"
            }

            a (href = $"/{beerTasteGuid}/results/oldmanbeers") {
                span (class' = "icon") { raw "👴" }
                raw "Beers Preferred by Older Tasters"
            }
        }
    ]
