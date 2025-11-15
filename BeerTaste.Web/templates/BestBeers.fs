module BeerTaste.Web.Templates.BestBeers

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: BeerResult list) =
    layout "Best Beers" [
        h1 () { raw "Best Beers" }

        renderNavigation beerTasteGuid ResultPage.BestBeers

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Beer" }
                    th (class' = "value") { raw "Average Score" }
                }
            }

            tbody () {
                for i, result in results |> List.indexed do
                    tr () {
                        td () { raw (string (i + 1)) }
                        td () { raw result.Name }
                        td (class' = "value") { raw $"%.2f{result.Value}" }
                    }
            }
        }
    ]
