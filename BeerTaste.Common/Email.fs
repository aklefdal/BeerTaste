namespace BeerTaste.Common

open System.Threading.Tasks
open MailKit.Net.Smtp
open MailKit.Security
open MimeKit

type EmailConfiguration = {
    SmtpServer: string
    SmtpPort: int
    SmtpUsername: string
    SmtpPassword: string
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
                use client = new SmtpClient()

                // Connect to SMTP server
                do! client.ConnectAsync(config.SmtpServer, config.SmtpPort, SecureSocketOptions.Auto)

                // Authenticate
                do! client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword)

                // Create the email message
                let mimeMessage = new MimeMessage()
                mimeMessage.From.Add(new MailboxAddress(config.FromName, config.FromEmail))
                mimeMessage.To.Add(new MailboxAddress(message.ToName, message.To))
                mimeMessage.Subject <- message.Subject

                mimeMessage.Body <- new TextPart("plain", Text = message.Body)

                // Send email
                let! _ = client.SendAsync(mimeMessage)

                // Disconnect
                do! client.DisconnectAsync(true)

                return Ok()
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
            use client = new SmtpClient()
            do! client.ConnectAsync(config.SmtpServer, config.SmtpPort, SecureSocketOptions.Auto)
            do! client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword)

            let results = System.Collections.Generic.List()

            for message in messages do
                let! result =
                    task {
                        try
                            let mimeMessage = new MimeMessage()
                            mimeMessage.From.Add(new MailboxAddress(config.FromName, config.FromEmail))
                            mimeMessage.To.Add(new MailboxAddress(message.ToName, message.To))
                            mimeMessage.Subject <- message.Subject
                            mimeMessage.Body <- new TextPart("plain", Text = message.Body)
                            let! _ = client.SendAsync(mimeMessage)
                            return Ok()
                        with ex ->
                            return Error $"Failed to send email to {message.To |> maskEmail}: {ex.Message}"
                    }

                results.Add((message, result))

            do! client.DisconnectAsync(true)
            return results |> Seq.toList
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
                    $"Hi {taster.Name},\n\n"
                    + $"The results for the beer tasting event \"{beerTasteName}\" are now available!\n\n"
                    + $"You can view the results at:\n{resultsUrl}\n\n"
                    + "Thank you for participating!\n\n"
                    + "Best regards,\nBeerTaste System"
            }
