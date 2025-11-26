module BeerTaste.Web.Templates.Controversial

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (results: BeerResultWithAverage list) =
    let t = getTranslations language

    layout t.MostControversialBeers beerTasteGuid language [
        h1 () { raw t.MostControversialBeers }

        renderNavigation beerTasteGuid t ResultPage.Controversial

        table () {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Beer }
                    th (class' = "value") { raw t.StandardDeviation }
                    th (class' = "supportingvalue") { raw $"({t.AverageScore})" }
                }
            }

            tbody () {
                for i, result in results |> List.indexed do
                    tr () {
                        td () { raw (string (i + 1)) }
                        td () { raw result.Name }
                        td (class' = "value") { raw $"%.2f{result.Value}" }
                        td (class' = "supportingvalue") { raw $"%.2f{result.Average}" }
                    }
            }
        }
    ]
