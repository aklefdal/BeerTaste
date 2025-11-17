module BeerTaste.Web.Templates.CheapAlcohol

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: TasterResult list) =
    layout "Most Fond of Cheap Alcohol" beerTasteGuid [
        h1 () { raw "Most Fond of Cheap Alcohol" }

        renderNavigation beerTasteGuid ResultPage.CheapAlcohol

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Taster" }
                    th (class' = "value") { raw "Correlation to Price per ABV" }
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
