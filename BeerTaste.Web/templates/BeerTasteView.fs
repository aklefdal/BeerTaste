module BeerTaste.Web.Templates.BeerTasteView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Localization

let view (beerTaste: BeerTaste) (language: Language) (firebaseConfig: FirebaseConfig option) =
    let t = getTranslations language

    layout beerTaste.ShortName beerTaste.BeerTasteGuid language firebaseConfig [
        h1 () { raw beerTaste.ShortName }

        table () {
            tbody () {
                tr () {
                    th () { raw t.Description }
                    td () { raw beerTaste.Description }
                }

                tr () {
                    th () { raw t.Date }
                    td () { raw (beerTaste.Date.ToString("yyyy-MM-dd")) }
                }
            }
        }
    ]
