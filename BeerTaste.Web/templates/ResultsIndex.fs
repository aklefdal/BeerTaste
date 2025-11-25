module BeerTaste.Web.Templates.ResultsIndex

open Oxpecker.ViewEngine
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) =
    let t = getTranslations language

    layout t.BeerTastingResults beerTasteGuid language [
        h1 () { raw t.BeerTastingResults }

        h2 () { raw t.AvailableResults }

        div (class' = "results-list") {
            a (href = $"/{beerTasteGuid}/results/bestbeers") {
                span (class' = "icon") { raw "★" }
                raw t.BestBeers
            }

            a (href = $"/{beerTasteGuid}/results/controversial") {
                span (class' = "icon") { raw "⚡" }
                raw t.MostControversialBeers
            }

            a (href = $"/{beerTasteGuid}/results/deviant") {
                span (class' = "icon") { raw "😈" }
                raw t.MostDeviantTasters
            }

            a (href = $"/{beerTasteGuid}/results/similar") {
                span (class' = "icon") { raw "❤" }
                raw t.MostSimilarTasters
            }

            a (href = $"/{beerTasteGuid}/results/strongbeers") {
                span (class' = "icon") { raw "😵" }
                raw t.MostFondOfStrongBeers
            }

            a (href = $"/{beerTasteGuid}/results/cheapalcohol") {
                span (class' = "icon") { raw "💰" }
                raw t.MostFondOfCheapAlcohol
            }

            a (href = $"/{beerTasteGuid}/results/oldmanbeers") {
                span (class' = "icon") { raw "👴" }
                raw "Beers Preferred by Older Tasters"
            }
        }
    ]
