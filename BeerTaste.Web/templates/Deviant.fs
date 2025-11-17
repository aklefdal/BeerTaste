module BeerTaste.Web.Templates.Deviant

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: TasterResult list) =
    layout "Most Deviant Tasters" beerTasteGuid [
        h1 () { raw "Most Deviant Tasters" }

        renderNavigation beerTasteGuid ResultPage.Deviant

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Taster" }
                    th (class' = "value") { raw "Correlation to Average" }
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
