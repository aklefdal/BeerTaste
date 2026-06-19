module BeerTaste.Tests.EmailTests

open Xunit
open BeerTaste.Common.Email

module MaskEmailTests =
    [<Fact>]
    let ``maskEmail masks local part and most of domain`` () =
        // local "test" → keep 2 chars + "**"; domain "example.com" (11 chars) → last 5 = "e.com"
        Assert.Equal("te**@**e.com", maskEmail "test@example.com")

    [<Fact>]
    let ``maskEmail keeps only first 2 chars of a long local part`` () =
        Assert.Equal("jo**@**e.com", maskEmail "john@example.com")

    [<Fact>]
    let ``maskEmail with 1-char local part appends stars`` () =
        // local "a" (length <= 2) → "a**"; domain "x.com" (5 chars, <= 5) → kept in full
        Assert.Equal("a**@**x.com", maskEmail "a@x.com")

    [<Fact>]
    let ``maskEmail with 2-char local part keeps both chars`` () =
        Assert.Equal("ab**@**x.com", maskEmail "ab@x.com")

    [<Fact>]
    let ``maskEmail with domain exactly 5 chars keeps it in full`` () =
        // "hi.to" is exactly 5 characters
        Assert.Equal("te**@**hi.to", maskEmail "test@hi.to")

    [<Fact>]
    let ``maskEmail with domain longer than 5 chars keeps only last 5`` () =
        // "longdomain.org" = 14 chars → last 5 = "n.org"
        Assert.Equal("te**@**n.org", maskEmail "test@longdomain.org")

    [<Fact>]
    let ``maskEmail returns original string when input has no at sign`` () =
        Assert.Equal("notanemail", maskEmail "notanemail")

    [<Fact>]
    let ``maskEmail returns original string when input has multiple at signs`` () =
        // split('@') yields ["foo"; "bar"; "baz.com"] which doesn't match [| local; domain |]
        Assert.Equal("foo@bar@baz.com", maskEmail "foo@bar@baz.com")
