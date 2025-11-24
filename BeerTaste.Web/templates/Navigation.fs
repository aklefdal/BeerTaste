module BeerTaste.Web.Templates.Navigation

open Oxpecker.ViewEngine

type ResultPage =
    | BestBeers
    | Controversial
    | Deviant
    | Similar
    | StrongBeers
    | CheapAlcohol

let allPages = [
    BestBeers
    Controversial
    Deviant
    Similar
    StrongBeers
    CheapAlcohol
]

let pageToRoute page =
    match page with
    | BestBeers -> "bestbeers"
    | Controversial -> "controversial"
    | Deviant -> "deviant"
    | Similar -> "similar"
    | StrongBeers -> "strongbeers"
    | CheapAlcohol -> "cheapalcohol"

let pageToTitle page =
    match page with
    | BestBeers -> "Best Beers"
    | Controversial -> "Most Controversial Beers"
    | Deviant -> "Most Deviant Tasters"
    | Similar -> "Most Similar Tasters"
    | StrongBeers -> "Most Fond of Strong Beers"
    | CheapAlcohol -> "Most Fond of Cheap Alcohol"

let pageToIcon page =
    match page with
    | BestBeers -> "★"
    | Controversial -> "⚡"
    | Deviant -> "😈"
    | Similar -> "❤"
    | StrongBeers -> "😵"
    | CheapAlcohol -> "💰"

let renderNavigation (beerTasteGuid: string) (currentPage: ResultPage) =
    div (class' = "results-nav") {
        for page in allPages do
            if page = currentPage then
                span (class' = "results-nav-button current") {
                    span (class' = "icon") { raw (pageToIcon page) }
                    raw (pageToTitle page)
                }
            else
                a (class' = "results-nav-button", href = $"/{beerTasteGuid}/results/{pageToRoute page}") {
                    span (class' = "icon") { raw (pageToIcon page) }
                    raw (pageToTitle page)
                }
    }
