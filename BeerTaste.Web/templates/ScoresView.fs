module BeerTaste.Web.Templates.ScoresView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (beers: Beer list) (tasters: Taster list) (scores: Score list) =
    let t = getTranslations language

    // Create a lookup for scores by (beerId, tasterName)
    let scoreLookup =
        scores
        |> List.map (fun s -> (s.BeerId, s.TasterName), s.ScoreValue)
        |> Map.ofList

    layout t.Scores beerTasteGuid language [
        h1 () { raw t.Scores }

        table () {
            thead () {
                tr () {
                    th () { raw t.Id }
                    th () { raw t.Producer }
                    th () { raw t.Name }

                    for taster in tasters do
                        th (class' = "value") { raw taster.Name }
                }
            }

            tbody () {
                for beer in beers do
                    tr () {
                        td () { raw (string beer.Id) }
                        td () { raw beer.Producer }
                        td () { raw beer.Name }

                        for taster in tasters do
                            let scoreValue = scoreLookup |> Map.tryFind (beer.Id, taster.Name)

                            td (class' = "value") {
                                raw (
                                    match scoreValue with
                                    | Some(Some v) -> string v
                                    | Some None -> "0"
                                    | None -> ""
                                )
                            }
                    }
            }
        }
    ]
