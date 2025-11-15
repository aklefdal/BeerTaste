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

let getPreviousPage currentPage =
    allPages
    |> List.tryFindIndex ((=) currentPage)
    |> Option.bind (fun idx -> if idx > 0 then Some allPages.[idx - 1] else None)

let getNextPage currentPage =
    allPages
    |> List.tryFindIndex ((=) currentPage)
    |> Option.bind (fun idx ->
        if idx < allPages.Length - 1 then
            Some allPages.[idx + 1]
        else
            None)

let renderNavigation (beerTasteGuid: string) (currentPage: ResultPage) =
    div (class' = "nav") {
        a (href = $"/results/{beerTasteGuid}") { raw "Back to Results" }

        match getPreviousPage currentPage with
        | Some prevPage ->
            a (href = $"/results/{beerTasteGuid}/{pageToRoute prevPage}") { raw " ← " }
        | None -> ()

        match getNextPage currentPage with
        | Some nextPage ->
            a (href = $"/results/{beerTasteGuid}/{pageToRoute nextPage}") { raw " → " }
        | None -> ()
    }
