module BeerTaste.Web.Templates.StrongBeers

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (results: TasterResult list) =
    let t = getTranslations language

    layout t.MostFondOfStrongBeers beerTasteGuid language [
        h1 () { raw t.MostFondOfStrongBeers }

        renderNavigation beerTasteGuid t ResultPage.StrongBeers

        table () {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Taster }
                    th (class' = "value") { raw t.CorrelationToABV }
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
