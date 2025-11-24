module BeerTaste.Web.Templates.StrongBeers

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (results: TasterResult list) =
    layout "Most Fond of Strong Beers" beerTasteGuid [
        h1 () { raw "Most Fond of Strong Beers" }

        renderNavigation beerTasteGuid ResultPage.StrongBeers

        table () {
            thead () {
                tr () {
                    th () { raw "Rank" }
                    th () { raw "Taster" }
                    th (class' = "value") { raw "Correlation to ABV" }
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
