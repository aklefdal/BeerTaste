module BeerTaste.Web.Templates.OldManBeers

open BeerTaste.Web.Localization
open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation

let view (beerTasteGuid: string) (language: Language) (results: BeerResult list) =
    let t = getTranslations language

    layout t.OldManBeers beerTasteGuid language [
        h1 () { raw t.OldManBeers }

        renderNavigation beerTasteGuid t ResultPage.OldManBeers

        table () {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Beer }
                    th (class' = "value") { raw t.AgeCorrelation }
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
