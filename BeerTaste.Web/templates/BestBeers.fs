module BeerTaste.Web.Templates.BestBeers

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation
open BeerTaste.Web.Localization

let view
    (beerTasteGuid: string)
    (language: Language)
    (firebaseConfig: FirebaseConfig option)
    (results: BeerResult list)
    =
    let t = getTranslations language

    layout t.BestBeers beerTasteGuid language firebaseConfig [
        h1 () { raw t.BestBeers }

        renderNavigation beerTasteGuid t ResultPage.BestBeers

        table () {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Beer }
                    th (class' = "value") { raw t.AverageScore }
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
