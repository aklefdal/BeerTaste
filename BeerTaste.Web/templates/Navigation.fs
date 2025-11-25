module BeerTaste.Web.Templates.Navigation

open Oxpecker.ViewEngine
open BeerTaste.Web.Localization

type ResultPage =
    | BestBeers
    | Controversial
    | Deviant
    | Similar
    | StrongBeers
    | CheapAlcohol
    | OldManBeers

let allPages = [
    BestBeers
    Controversial
    Deviant
    Similar
    StrongBeers
    CheapAlcohol
    OldManBeers
]

let pageToRoute page =
    match page with
    | BestBeers -> "bestbeers"
    | Controversial -> "controversial"
    | Deviant -> "deviant"
    | Similar -> "similar"
    | StrongBeers -> "strongbeers"
    | CheapAlcohol -> "cheapalcohol"
    | OldManBeers -> "oldmanbeers"

let pageToTitle (t: Translations) page =
    match page with
    | BestBeers -> t.BestBeers
    | Controversial -> t.MostControversialBeers
    | Deviant -> t.MostDeviantTasters
    | Similar -> t.MostSimilarTasters
    | StrongBeers -> t.MostFondOfStrongBeers
    | CheapAlcohol -> t.MostFondOfCheapAlcohol
    | OldManBeers -> t.OldManBeers

let pageToIcon page =
    match page with
    | BestBeers -> "★"
    | Controversial -> "⚡"
    | Deviant -> "😈"
    | Similar -> "❤"
    | StrongBeers -> "😵"
    | CheapAlcohol -> "💰"
    | OldManBeers -> "👴"

let renderNavigation (beerTasteGuid: string) (t: Translations) (currentPage: ResultPage) =
    div (class' = "results-nav") {
        for page in allPages do
            if page = currentPage then
                span (class' = "results-nav-button current") {
                    span (class' = "icon") { raw (pageToIcon page) }
                    raw (pageToTitle t page)
                }
            else
                a (class' = "results-nav-button", href = $"/{beerTasteGuid}/results/{pageToRoute page}") {
                    span (class' = "icon") { raw (pageToIcon page) }
                    raw (pageToTitle t page)
                }
    }
