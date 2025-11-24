module BeerTaste.Web.Templates.Deviant

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (results: TasterResult list) =
    let t = getTranslations language

    layout t.MostDeviantTasters beerTasteGuid language [
        h1 () { raw t.MostDeviantTasters }

        renderNavigation beerTasteGuid t ResultPage.Deviant

        table () {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Taster }
                    th (class' = "value") { raw t.CorrelationToAverage }
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
