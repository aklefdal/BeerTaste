namespace BeerTaste.Common

open System.Threading.Tasks
open SendGrid
open SendGrid.Helpers.Mail

type EmailConfiguration = {
    SendGridApiKey: string
    FromEmail: string
    FromName: string
}

type EmailMessage = {
    To: string
    ToName: string
    Subject: string
    Body: string
}

module Email =
    let maskEmail (email: string) =
        match email.Split '@' with
        | [| local; domain |] ->
            let maskedLocal =
                if local.Length <= 2 then
                    local + "**"
                else
                    local.Substring(0, 2) + "**"

            let keepDomain =
                if domain.Length <= 5 then
                    domain
                else
                    domain.Substring(domain.Length - 5)

            $"%s{maskedLocal}@**%s{keepDomain}"

        | _ -> email

    let sendEmail (config: EmailConfiguration) (message: EmailMessage) : Task<Result<unit, string>> =
        task {
            try
                let client = SendGridClient(config.SendGridApiKey)

                let from = EmailAddress(config.FromEmail, config.FromName)
                let toName = message.ToName
                let toAddress = EmailAddress(message.To, toName)

                // SendGrid API: plainTextContent, htmlContent (we only use plain text)
                let msg = MailHelper.CreateSingleEmail(from, toAddress, message.Subject, message.Body, null)

                let! response = client.SendEmailAsync(msg)

                if response.IsSuccessStatusCode then
                    return Ok()
                else
                    let! body = response.Body.ReadAsStringAsync()

                    return Error $"Failed to send email to {message.To}: {response.StatusCode} - {body}"
            with ex ->
                return Error $"Failed to send email to {message.To |> maskEmail}: {ex.Message}"
        }

    let isAdmin (message: EmailMessage) : bool =
        [
            "alf.kare@lefdal.cc"
            "aklefdal@gmail.com"
            "alf.kare.lefdal@aurum.no"
            "alf.lefdal@sikri.no"
        ]
        |> List.contains (message.To.ToLower())

    let sendEmails
        (config: EmailConfiguration)
        (messages: EmailMessage list)
        : Task<(EmailMessage * Result<unit, string>) list> =
        task {
            let! results =
                messages
                |> List.map (fun message ->
                    task {
                        let! result = sendEmail config message
                        return (message, result)
                    })
                |> Task.WhenAll

            return results |> Array.toList
        }

    let createBeerTasteResultsEmail
        (beerTasteName: string)
        (resultsUrl: string)
        (taster: Taster)
        : EmailMessage option =
        match taster.Email with
        | None -> None
        | Some email ->
            Some {
                To = email
                ToName = taster.Name
                Subject = $"Beer Tasting Results: {beerTasteName}"
                Body =
                    $"""Hi {taster.Name},

                    The results for the beer tasting event "{beerTasteName}" are now available!

                    You can view the results at:
                    {resultsUrl}

                    Thank you for participating!

                    Best regards,
                    BeerTaste System
                    """
            }
