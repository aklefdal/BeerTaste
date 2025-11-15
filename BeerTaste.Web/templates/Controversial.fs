module BeerTaste.Web.Templates.Controversial

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout

let view (beerTasteGuid: string) (results: BeerResult list) =
    layout "Most Controversial Beers" [
        h1() { raw "Most Controversial Beers" }

        div(class'="nav") {
            a(href = $"/results/{beerTasteGuid}") { raw "Back to Results" }
        }

        table() {
            thead() {
                tr() {
                    th() { raw "Rank" }
                    th() { raw "Beer" }
                    th(class'="value") { raw "Standard Deviation" }
                }
            }
            tbody() {
                for i, result in results |> List.indexed do
                    tr() {
                        td() { raw (string (i + 1)) }
                        td() { raw result.Name }
                        td(class'="value") { raw $"%.2f{result.Value}" }
                    }
            }
        }
    ]
