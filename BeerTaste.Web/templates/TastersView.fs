module BeerTaste.Web.Templates.TastersView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (tasters: Taster list) =
    let t = getTranslations language

    layout t.Tasters beerTasteGuid language [
        h1 () { raw t.Tasters }

        table () {
            thead () {
                tr () {
                    th () { raw t.Name }
                    th () { raw t.Email }
                    th (class' = "value") { raw t.BirthYear }
                }
            }

            tbody () {
                for taster in tasters do
                    tr () {
                        td () { raw taster.Name }

                        td () {
                            raw (
                                taster.Email
                                |> Option.map Email.maskEmail
                                |> Option.defaultValue ""
                            )
                        }

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
