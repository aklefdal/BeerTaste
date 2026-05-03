module BeerTaste.Web.FormHelpers

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Oxpecker.ViewEngine

/// Renders a hidden anti-forgery token input for use in HTML forms.
/// Add to every form that POSTs to a protected endpoint.
let antiforgeryInput (ctx: HttpContext) : HtmlElement =
    let antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>()
    let tokens = antiforgery.GetAndStoreTokens(ctx)
    input (type' = "hidden", name = tokens.FormFieldName, value = tokens.RequestToken)

/// Validates the anti-forgery token for the current request.
/// Returns Ok () if the token is valid, Error message if validation fails.
let validateAntiforgery (ctx: HttpContext) : Task<Result<unit, string>> =
    task {
        let antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>()

        try
            do! antiforgery.ValidateRequestAsync(ctx)
            return Ok()
        with ex ->
            return Error ex.Message
    }
