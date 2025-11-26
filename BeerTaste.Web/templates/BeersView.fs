module BeerTaste.Web.Templates.BeersView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (beers: Beer list) =
    let t = getTranslations language

    layout t.Beers beerTasteGuid language [
        h1 () { raw t.Beers }

        table () {
            thead () {
                tr () {
                    th () { raw t.Id }
                    th () { raw t.Name }
                    th () { raw t.Type }
                    th () { raw t.Origin }
                    th () { raw t.Producer }
                    th (class' = "value") { raw t.ABV }
                    th (class' = "value") { raw t.Volume }
                    th (class' = "value") { raw t.Price }
                    th () { raw t.Packaging }
                }
            }

            tbody () {
                for beer in beers do
                    tr () {
                        td () { raw (string beer.Id) }
                        td () { raw beer.Name }
                        td () { raw beer.BeerType }
                        td () { raw beer.Origin }
                        td () { raw beer.Producer }
                        td (class' = "value") { raw $"%.1f{beer.ABV}%%" }
                        td (class' = "value") { raw $"%.2f{beer.Volume} l" }
                        td (class' = "value") { raw $"%.2f{beer.Price}" }
                        td () { raw beer.Packaging }
                    }
            }
        }
    ]
