module BeerTaste.Web.Templates.Controversial

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: BeerResultWithAverage list) =
    layout "Most Controversial Beers" beerTasteGuid [
        h1 () { raw "Most Controversial Beers" }

        renderNavigation beerTasteGuid ResultPage.Controversial

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Beer" }
                    th (class' = "value") { raw "Standard Deviation" }
                    th (class' = "value") { raw "Average Score" }
                }
            }

            tbody () {
                for i, result in results |> List.indexed do
                    tr () {
                        td () { raw (string (i + 1)) }
                        td () { raw result.Name }
                        td (class' = "value") { raw $"%.2f{result.Value}" }
                        td (class' = "value") { raw $"%.2f{result.Average}" }
                    }
            }
        }
    ]
