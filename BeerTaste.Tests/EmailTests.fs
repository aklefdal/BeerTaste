module BeerTaste.Tests.EmailTests

open Xunit
open BeerTaste.Common
open BeerTaste.Tests.TestHelpers

module MaskEmailTests =
    [<Fact>]
    let ``maskEmail masks standard email`` () =
        let result = Email.maskEmail "hello@example.com"
        Assert.Equal("he**@**e.com", result)

    [<Fact>]
    let ``maskEmail masks email with short local part (1 char)`` () =
        let result = Email.maskEmail "a@example.com"
        Assert.Equal("a**@**e.com", result)

    [<Fact>]
    let ``maskEmail masks email with 2-char local part`` () =
        let result = Email.maskEmail "ab@example.com"
        Assert.Equal("ab**@**e.com", result)

    [<Fact>]
    let ``maskEmail keeps full domain when domain length is 5 or less`` () =
        let result = Email.maskEmail "test@ab.no"
        Assert.Equal("te**@**ab.no", result)

    [<Fact>]
    let ``maskEmail keeps last 5 chars of domain when longer`` () =
        let result = Email.maskEmail "test@longdomain.org"
        Assert.Equal("te**@**n.org", result)

    [<Fact>]
    let ``maskEmail returns original string when no at-sign present`` () =
        let result = Email.maskEmail "notanemail"
        Assert.Equal("notanemail", result)

    [<Fact>]
    let ``maskEmail handles multiple at-signs by returning original`` () =
        let result = Email.maskEmail "a@b@c"
        Assert.Equal("a@b@c", result)

module IsAdminTests =
    [<Fact>]
    let ``isAdmin returns true for known admin email`` () =
        let msg = {
            To = "aklefdal@gmail.com"
            ToName = "Alf"
            Subject = "s"
            Body = "b"
        }

        Assert.True(Email.isAdmin msg)

    [<Fact>]
    let ``isAdmin returns true for another admin email`` () =
        let msg = {
            To = "alf.kare@lefdal.cc"
            ToName = "Alf"
            Subject = "s"
            Body = "b"
        }

        Assert.True(Email.isAdmin msg)

    [<Fact>]
    let ``isAdmin returns false for non-admin email`` () =
        let msg = {
            To = "beer@taster.com"
            ToName = "Bob"
            Subject = "s"
            Body = "b"
        }

        Assert.False(Email.isAdmin msg)

    [<Fact>]
    let ``isAdmin is case-insensitive`` () =
        let msg = {
            To = "AKLEFDAL@GMAIL.COM"
            ToName = "Alf"
            Subject = "s"
            Body = "b"
        }

        Assert.True(Email.isAdmin msg)

module CreateBeerTasteResultsEmailTests =
    let makeTasterWithEmail name email = {
        Name = name
        Email = Some email
        BirthYear = None
    }

    [<Fact>]
    let ``createBeerTasteResultsEmail returns None when taster has no email`` () =
        let taster = makeTaster "Alice" None
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://example.com" taster
        Assert.True(result.IsNone)

    [<Fact>]
    let ``createBeerTasteResultsEmail returns Some when taster has email`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://example.com" taster
        Assert.True(result.IsSome)

    [<Fact>]
    let ``createBeerTasteResultsEmail sets To field from taster email`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://example.com" taster
        Assert.Equal("alice@example.com", result.Value.To)

    [<Fact>]
    let ``createBeerTasteResultsEmail sets ToName from taster name`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://example.com" taster
        Assert.Equal("Alice", result.Value.ToName)

    [<Fact>]
    let ``createBeerTasteResultsEmail subject contains event name`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Sommersmaking 2025" "https://example.com" taster
        Assert.Contains("Sommersmaking 2025", result.Value.Subject)

    [<Fact>]
    let ``createBeerTasteResultsEmail body contains taster name`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://example.com" taster
        Assert.Contains("Alice", result.Value.Body)

    [<Fact>]
    let ``createBeerTasteResultsEmail body contains results URL`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://results.example.com/event" taster
        Assert.Contains("https://results.example.com/event", result.Value.Body)

    [<Fact>]
    let ``createBeerTasteResultsEmail body contains event name`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Sommersmaking 2025" "https://example.com" taster
        Assert.Contains("Sommersmaking 2025", result.Value.Body)

    [<Fact>]
    let ``createBeerTasteResultsEmail body has no leading whitespace on lines`` () =
        let taster = makeTasterWithEmail "Alice" "alice@example.com"
        let result = Email.createBeerTasteResultsEmail "Ølsmaking" "https://example.com" taster
        let lines = result.Value.Body.Split('\n')

        for line in lines do
            if line.Length > 0 then
                Assert.False(System.Char.IsWhiteSpace(line[0]), $"Line starts with whitespace: '{line}'")
