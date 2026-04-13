module BeerTaste.Web.FirebaseAuth

#nowarn "44" // GoogleCredential.FromFile deprecated but CredentialFactory replacement not yet available

open System
open System.IO
open System.Threading.Tasks
open FirebaseAdmin
open FirebaseAdmin.Auth
open Microsoft.Extensions.Configuration

type VerifiedToken = {
    Uid: string
    Email: string option
    Name: string option
    EmailVerified: bool
}

let readProjectId (config: IConfiguration) : string =
    match
        config["BeerTaste:Firebase:ProjectId"]
        |> Option.ofObj
    with
    | Some id -> id
    | None -> failwith "Missing required configuration 'BeerTaste:Firebase:ProjectId'"

let initialize (config: IConfiguration) =
    let projectId = readProjectId config

    let serviceAccountKeyPath =
        config["BeerTaste:Firebase:ServiceAccountKeyPath"]
        |> Option.ofObj
        |> Option.filter (String.IsNullOrEmpty >> not)

    let credential =
        match serviceAccountKeyPath with
        | Some path when File.Exists(path) -> Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(path)
        | Some path -> failwith $"Firebase service account key file not found: {path}"
        | None ->
            match
                Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                |> Option.ofObj
            with
            | Some envPath when File.Exists(envPath) -> Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault()
            | Some envPath -> failwith $"GOOGLE_APPLICATION_CREDENTIALS file not found: {envPath}"
            | None ->
                failwith
                    "No Firebase credentials configured (no ServiceAccountKeyPath or GOOGLE_APPLICATION_CREDENTIALS)"

    let appOptions = AppOptions(Credential = credential)
    appOptions.ProjectId <- projectId
    FirebaseApp.Create(appOptions) |> ignore

let verifyIdToken (idToken: string) : Task<Result<VerifiedToken, string>> =
    task {
        try
            let! decoded = FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken)

            let claim key =
                match decoded.Claims.TryGetValue(key) with
                | true, value -> Some(string value)
                | false, _ -> None

            return
                Ok {
                    Uid = decoded.Uid
                    Email = claim "email"
                    Name = claim "name"
                    EmailVerified =
                        match decoded.Claims.TryGetValue("email_verified") with
                        | true, (:? bool as b) -> b
                        | true, value -> value.ToString() |> Boolean.TryParse |> snd
                        | false, _ -> false
                }
        with :? FirebaseAuthException as ex ->
            return Error $"Token verification failed: {ex.Message}"
    }
