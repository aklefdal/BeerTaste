namespace BeerTaste.Common

open System
open SendGrid
open SendGrid.Helpers.Mail

type EmailConfiguration = {
    SendGridApiKey: string
    FromEmail: string
    FromName: string
}

type EmailMessage = {
    To: string
    ToName: string option
    Subject: string
    Body: string
}

module Email =
    let sendEmail (config: EmailConfiguration) (message: EmailMessage) : Async<Result<unit, string>> =
        async {
            try
                let client = new SendGridClient(config.SendGridApiKey)

                let from = new EmailAddress(config.FromEmail, config.FromName)
                let toName = message.ToName |> Option.defaultValue message.To
                let toAddress = new EmailAddress(message.To, toName)

                // SendGrid API: plainTextContent, htmlContent (we only use plain text)
                let msg = MailHelper.CreateSingleEmail(from, toAddress, message.Subject, message.Body, null)

                let! response = client.SendEmailAsync(msg) |> Async.AwaitTask

                if response.IsSuccessStatusCode then
                    return Ok()
                else
                    let! body =
                        response.Body.ReadAsStringAsync()
                        |> Async.AwaitTask

                    return Error $"Failed to send email to {message.To}: {response.StatusCode} - {body}"
            with ex ->
                return Error $"Failed to send email to {message.To}: {ex.Message}"
        }

    let sendEmails
        (config: EmailConfiguration)
        (messages: EmailMessage list)
        : Async<(EmailMessage * Result<unit, string>) list> =
        async {
            let! results =
                messages
                |> List.map (fun message ->
                    async {
                        let! result = sendEmail config message
                        return (message, result)
                    })
                |> Async.Parallel

            return results |> Array.toList
        }

    let createBeerTasteResultsEmail (tasterName: string) (beerTasteName: string) (resultsUrl: string) : EmailMessage = {
        To = "" // Will be set by caller
        ToName = Some tasterName
        Subject = $"Beer Tasting Results: {beerTasteName}"
        Body =
            $"""Hi {tasterName},

The results for the beer tasting event "{beerTasteName}" are now available!

You can view the results at:
{resultsUrl}

Thank you for participating!

Best regards,
BeerTaste System
"""
    }
