module BeerTaste.Web.AuthMiddleware

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open BeerTaste.Common
open BeerTaste.Common.Sessions

[<Literal>]
let AuthSchemeFirebase = "Firebase"

[<Literal>]
let CurrentUserKey = "CurrentUser"

[<Literal>]
let SessionCookieName = "session"

let private extractSessionCookieId (ctx: HttpContext) : Guid option =
    match ctx.Request.Cookies.TryGetValue(SessionCookieName) with
    | true, value when not (String.IsNullOrEmpty(value)) ->
        match Guid.TryParse(value) with
        | true, guid -> Some guid
        | false, _ -> None
    | _ -> None

let getCurrentUser (ctx: HttpContext) : User option =
    match ctx.Items.TryGetValue(CurrentUserKey) with
    | true, (:? User as user) -> Some user
    | _ -> None

let sessionAuthMiddleware (next: RequestDelegate) (ctx: HttpContext) : Task =
    task {
        let storage = ctx.RequestServices.GetRequiredService<BeerTasteTableStorage>()

        match extractSessionCookieId ctx with
        | Some sessionId ->
            match! authenticateSession storage.SessionsTableClient sessionId with
            | Some user -> ctx.Items[CurrentUserKey] <- user
            | None -> ()
        | None -> ()

        do! next.Invoke(ctx)
    }
