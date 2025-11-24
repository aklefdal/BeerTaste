module BeerTaste.Web.Templates.TastersView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout

let maskEmail (email: string) =
    match email.Split '@' with
    | [| local; domain |] ->
        let maskedLocal =
            if local.Length <= 2 then
                local + "**"
            else
                local.Substring(0, 2) + "**"

        let keepDomain =
            if domain.Length <= 5 then
                domain
            else
                domain.Substring(domain.Length - 5)

        $"%s{maskedLocal}@**%s{keepDomain}"

    | _ -> email


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

                        td () {
                            raw (
                                taster.Email
                                |> Option.map maskEmail
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
