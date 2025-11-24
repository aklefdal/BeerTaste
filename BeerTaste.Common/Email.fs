namespace BeerTaste.Common

open System
open MailKit.Net.Smtp
open MimeKit

type EmailConfiguration = {
    SmtpHost: string
    SmtpPort: int
    SmtpUsername: string
    SmtpPassword: string
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
    let sendEmail (config: EmailConfiguration) (message: EmailMessage) : Result<unit, string> =
        try
            use client = new SmtpClient()

            // Connect to SMTP server
            client.Connect(config.SmtpHost, config.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls)

            // Authenticate
            client.Authenticate(config.SmtpUsername, config.SmtpPassword)

            // Create message
            let mimeMessage = new MimeMessage()
            mimeMessage.From.Add(new MailboxAddress(config.FromName, config.FromEmail))

            let toName = message.ToName |> Option.defaultValue message.To
            mimeMessage.To.Add(new MailboxAddress(toName, message.To))

            mimeMessage.Subject <- message.Subject

            let bodyBuilder = new BodyBuilder()
            bodyBuilder.TextBody <- message.Body
            mimeMessage.Body <- bodyBuilder.ToMessageBody()

            // Send message
            client.Send(mimeMessage) |> ignore

            // Disconnect
            client.Disconnect(true)

            Ok()
        with ex ->
            Error $"Failed to send email to {message.To}: {ex.Message}"

    let sendEmails
        (config: EmailConfiguration)
        (messages: EmailMessage list)
        : (EmailMessage * Result<unit, string>) list =
        messages
        |> List.map (fun message ->
            let result = sendEmail config message
            (message, result))

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
