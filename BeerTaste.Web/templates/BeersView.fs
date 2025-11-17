module BeerTaste.Web.Templates.BeersView

open Oxpecker.ViewEngine
open BeerTaste.Common
open BeerTaste.Web.Templates.Layout

let view (beerTasteGuid: string) (beers: Beer list) =
    layout "Beers" beerTasteGuid [
        h1 () { raw "Beers" }

        p () { a (href = $"/{beerTasteGuid}/results") { raw "Back to Results" } }

        table () {
            thead () {
                tr () {
                    th () { raw "Id" }
                    th () { raw "Name" }
                    th () { raw "Type" }
                    th () { raw "Origin" }
                    th () { raw "Producer" }
                    th (class' = "value") { raw "ABV" }
                    th (class' = "value") { raw "Volume" }
                    th (class' = "value") { raw "Price" }
                    th () { raw "Packaging" }
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
