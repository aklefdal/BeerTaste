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

let sessionAuthMiddleware (next: RequestDelegate) (ctx: HttpContext) : Task =
    task {
        let storage = ctx.RequestServices.GetRequiredService<BeerTasteTableStorage>()

        match extractSessionCookieId ctx with
        | Some sessionId ->
            match! authenticateSession storage.SessionsTableClient sessionId with
            | Some user ->
                ctx.Items[CurrentUserKey] <- user
                // Refresh the cookie expiry so active users are never logged out.
                // The cookie is set to expire 90 days from login but is never renewed,
                // so users who are active for more than 90 days get a stale cookie.
                // Refreshing it on every authenticated request makes the expiry sliding.
                let isDevelopment = ctx.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()

                ctx.Response.Cookies.Append(
                    SessionCookieName,
                    sessionId.ToString(),
                    CookieOptions(
                        HttpOnly = true,
                        Secure = not isDevelopment,
                        SameSite = SameSiteMode.Strict,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(SessionExpiryDays)
                    )
                )
            | None -> ()
        | None -> ()

        do! next.Invoke(ctx)
    }
