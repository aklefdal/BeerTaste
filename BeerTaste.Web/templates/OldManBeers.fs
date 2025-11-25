module BeerTaste.Web.Templates.OldManBeers

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: BeerResult list) =
    layout "Beers Preferred by Older Tasters" beerTasteGuid [
        h1 () { raw "Beers Preferred by Older Tasters" }

        renderNavigation beerTasteGuid ResultPage.OldManBeers

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Beer" }
                    th (class' = "value") { raw "Age Correlation" }
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
