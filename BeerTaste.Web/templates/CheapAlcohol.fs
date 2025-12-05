module BeerTaste.Web.Templates.CheapAlcohol

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation
open BeerTaste.Web.Localization

let view
    (beerTasteGuid: string)
    (language: Language)
    (firebaseConfig: FirebaseConfig option)
    (results: TasterResult list)
    =
    let t = getTranslations language

    layout t.MostFondOfCheapAlcohol beerTasteGuid language firebaseConfig [
        h1 () { raw t.MostFondOfCheapAlcohol }

        renderNavigation beerTasteGuid t ResultPage.CheapAlcohol

        table () {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Taster }
                    th (class' = "value") { raw t.CorrelationToPricePerABV }
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
