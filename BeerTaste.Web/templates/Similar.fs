module BeerTaste.Web.Templates.Similar

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: TasterPairResult list) =
    layout "Most Similar Tasters" [
        h1 () { raw "Most Similar Tasters" }

        renderNavigation beerTasteGuid ResultPage.Similar

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Taster 1" }
                    th () { raw "Taster 2" }
                    th (class' = "value") { raw "Correlation" }
                }
            }

            tbody () {
                for i, result in results |> List.indexed do
                    tr () {
                        td () { raw (string (i + 1)) }
                        td () { raw result.Name1 }
                        td () { raw result.Name2 }
                        td (class' = "value") { raw $"%.2f{result.Value}" }
                    }
            }
        }
    ]
