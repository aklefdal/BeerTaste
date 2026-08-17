module BeerTaste.Web.AuthMiddleware

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
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

let sessionCookieOptions (isDevelopment: bool) (now: DateTimeOffset) =
    CookieOptions(
        HttpOnly = true,
        Secure = not isDevelopment,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = now.AddDays(SessionExpiryDays)
    )

let appendSessionCookie (ctx: HttpContext) (sessionId: Guid) (now: DateTimeOffset) =
    let isDevelopment = ctx.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
    ctx.Response.Cookies.Append(SessionCookieName, sessionId.ToString(), sessionCookieOptions isDevelopment now)

let sessionAuthMiddleware (next: RequestDelegate) (ctx: HttpContext) : Task =
    task {
        let storage = ctx.RequestServices.GetRequiredService<BeerTasteTableStorage>()

        match extractSessionCookieId ctx with
        | Some sessionId ->
            match! authenticateSession storage.SessionsTableClient sessionId with
            | Some authenticated ->
                ctx.Items[CurrentUserKey] <- authenticated.User

                // Make the expiry sliding so active users are never logged out, throttled to the
                // same rate as the LastActiveAt write to avoid a Set-Cookie on every response.
                if authenticated.LastActiveUpdated then
                    appendSessionCookie ctx sessionId DateTimeOffset.UtcNow
            | None -> ()
        | None -> ()

        do! next.Invoke(ctx)
    }
