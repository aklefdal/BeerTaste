namespace BeerTaste.Common

open System
open System.Threading.Tasks
open Azure.Data.Tables

type Session = {
    SessionId: Guid
    UserId: Guid
    AccountId: string
    AuthScheme: string
    Name: string
    LastActiveAt: DateTimeOffset
}

module Sessions =
    [<Literal>]
    let SessionExpiryDays = 90.0

    [<Literal>]
    let LastActiveThresholdHours = 1.0

    let isExpired (now: DateTimeOffset) (session: Session) =
        session.LastActiveAt < now.AddDays(-SessionExpiryDays)

    let shouldUpdateLastActive (now: DateTimeOffset) (session: Session) =
        session.LastActiveAt < now.AddHours(-LastActiveThresholdHours)

    let private partitionKey (sessionId: Guid) = sessionId.ToString("N").Substring(0, 8)

    let sessionToEntity (session: Session) : TableEntity =
        let entity = TableEntity(partitionKey session.SessionId, session.SessionId.ToString())
        entity.Add("UserId", session.UserId)
        entity.Add("AccountId", session.AccountId)
        entity.Add("AuthScheme", session.AuthScheme)
        entity.Add("Name", session.Name)
        entity.Add("LastActiveAt", session.LastActiveAt)
        entity

    let entityToSession (entity: TableEntity) : Session = {
        SessionId = entity.RowKey |> Guid.Parse
        UserId = entity.GetGuid("UserId").Value
        AccountId = entity.GetString("AccountId")
        AuthScheme = entity.GetString("AuthScheme")
        Name = entity.GetString("Name")
        LastActiveAt = entity.GetDateTimeOffset("LastActiveAt").Value
    }

    let addSession (sessionsTable: TableClient) (session: Session) : Task =
        task {
            let entity = sessionToEntity session
            let! _ = sessionsTable.UpsertEntityAsync(entity)
            ()
        }

    let fetchSession (sessionsTable: TableClient) (sessionId: Guid) : Task<Session option> =
        task {
            try
                let! response = sessionsTable.GetEntityAsync<TableEntity>(partitionKey sessionId, sessionId.ToString())

                return response.Value |> entityToSession |> Some
            with :? Azure.RequestFailedException as ex when ex.Status = 404 ->
                return None
        }

    let deleteSession (sessionsTable: TableClient) (sessionId: Guid) : Task =
        task {
            try
                let! _ = sessionsTable.DeleteEntityAsync(partitionKey sessionId, sessionId.ToString())

                ()
            with :? Azure.RequestFailedException as ex when ex.Status = 404 ->
                ()
        }

    let authenticateSession (sessionsTable: TableClient) (sessionId: Guid) : Task<User option> =
        task {
            match! fetchSession sessionsTable sessionId with
            | None -> return None
            | Some session ->
                let now = DateTimeOffset.UtcNow

                if isExpired now session then
                    do! deleteSession sessionsTable sessionId
                    return None
                else
                    if shouldUpdateLastActive now session then
                        do! addSession sessionsTable { session with LastActiveAt = now }

                    return
                        Some {
                            UserId = session.UserId
                            AccountId = session.AccountId
                            AuthenticationScheme = session.AuthScheme
                            Name = session.Name
                        }
        }
