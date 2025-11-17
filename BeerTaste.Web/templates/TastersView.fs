module BeerTaste.Web.Templates.TastersView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout

let view (beerTasteGuid: string) (tasters: Taster list) =
    layout "Tasters" beerTasteGuid [
        h1 () { raw "Tasters" }

        p () { a (href = $"/{beerTasteGuid}/results") { raw "Back to Results" } }

        table () {
            thead () {
                tr () {
                    th () { raw "Name" }
                    th () { raw "Email" }
                    th (class' = "value") { raw "Birth Year" }
                }
            }

            tbody () {
                for taster in tasters do
                    tr () {
                        td () { raw taster.Name }
                        td () { raw (taster.Email |> Option.defaultValue "") }

                        td (class' = "value") {
                            raw (
                                taster.BirthYear
                                |> Option.map string
                                |> Option.defaultValue ""
                            )
                        }
                    }
            }
        }
    ]
