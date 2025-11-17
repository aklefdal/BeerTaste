module BeerTaste.Web.Templates.BeerTasteView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout

let view (beerTaste: BeerTaste) =
    layout beerTaste.ShortName beerTaste.BeerTasteGuid [
        h1 () { raw beerTaste.ShortName }

        table () {
            tbody () {
                tr () {
                    th () { raw "Description" }
                    td () { raw beerTaste.Description }
                }

                tr () {
                    th () { raw "Date" }
                    td () { raw (beerTaste.Date.ToString("yyyy-MM-dd")) }
                }
            }
        }
    ]
