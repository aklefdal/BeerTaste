module BeerTaste.Tests.SessionsTests

open System
open Xunit
open BeerTaste.Common
open BeerTaste.Common.Sessions

let makeSession () = {
    SessionId = Guid.NewGuid()
    UserId = Guid.NewGuid()
    AccountId = "firebase-uid-123"
    AuthScheme = "Firebase"
    Name = "Test User"
    LastActiveAt = DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero)
}

module IsExpiredTests =
    [<Fact>]
    let ``session active 1 day ago is not expired`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddDays(1.0)
        Assert.False(isExpired now session)

    [<Fact>]
    let ``session active 89 days ago is not expired`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddDays(89.0)
        Assert.False(isExpired now session)

    [<Fact>]
    let ``session active exactly 90 days ago is not expired`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddDays(90.0)
        Assert.False(isExpired now session)

    [<Fact>]
    let ``session active 91 days ago is expired`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddDays(91.0)
        Assert.True(isExpired now session)

module ShouldUpdateLastActiveTests =
    [<Fact>]
    let ``should not update if last active 30 minutes ago`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddMinutes(30.0)
        Assert.False(shouldUpdateLastActive now session)

    [<Fact>]
    let ``should not update if last active exactly 1 hour ago`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddHours(1.0)
        Assert.False(shouldUpdateLastActive now session)

    [<Fact>]
    let ``should update if last active more than 1 hour ago`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddHours(1.0).AddSeconds(1.0)
        Assert.True(shouldUpdateLastActive now session)

    [<Fact>]
    let ``should update if last active 1 day ago`` () =
        let session = makeSession ()
        let now = session.LastActiveAt.AddDays(1.0)
        Assert.True(shouldUpdateLastActive now session)

module EntityRoundTripTests =
    [<Fact>]
    let ``session survives entity round-trip`` () =
        let session = makeSession ()
        let roundTripped = session |> sessionToEntity |> entityToSession
        Assert.Equal(session.SessionId, roundTripped.SessionId)
        Assert.Equal(session.UserId, roundTripped.UserId)
        Assert.Equal(session.AccountId, roundTripped.AccountId)
        Assert.Equal(session.AuthScheme, roundTripped.AuthScheme)
        Assert.Equal(session.Name, roundTripped.Name)
        Assert.Equal(session.LastActiveAt, roundTripped.LastActiveAt)

    [<Fact>]
    let ``partition key uses first 8 chars of session id`` () =
        let session = makeSession ()
        let entity = sessionToEntity session
        let expected = session.SessionId.ToString("N").Substring(0, 8)
        Assert.Equal(expected, entity.PartitionKey)

    [<Fact>]
    let ``row key is full session id`` () =
        let session = makeSession ()
        let entity = sessionToEntity session
        Assert.Equal(session.SessionId.ToString(), entity.RowKey)
