# GitHub Copilot Instructions for BeerTaste

This document provides guidance to GitHub Copilot when working with the BeerTaste repository.

## Project Context

BeerTaste is an F# data analysis system for beer tasting events. It uses:
- **F# with .NET 9.0** - Primary language and runtime
- **Azure Table Storage** - Data persistence
- **EPPlus** - Excel file operations
- **Spectre.Console** - CLI interactions
- **Oxpecker** - F# web framework
- **FSharp.Stats** - Statistical analysis

## Architecture Overview

### Three-Layer Architecture

1. **BeerTaste.Common** (Shared Library)
   - Domain models (Beer, Taster, Score, BeerTaste)
   - Azure Table Storage operations
   - Statistical analysis functions
   - NO UI dependencies

2. **BeerTaste.Console** (Console Application)
   - Excel I/O using EPPlus (EXCLUSIVE to Console)
   - User interaction using Spectre.Console (EXCLUSIVE to Console)
   - Delegates all Azure operations to Common

3. **BeerTaste.Web** (Web Application)
   - Results presentation using Oxpecker framework
   - ASP.NET Core hosting (EXCLUSIVE to Web)
   - Delegates all Azure operations to Common

### Critical Dependency Rules

**When adding code or features, ALWAYS follow these rules:**

| What | Goes Where | Why |
|------|-----------|-----|
| Excel operations (EPPlus) | BeerTaste.Console ONLY | Separation of concerns |
| CLI interactions (Spectre.Console) | BeerTaste.Console ONLY | Separation of concerns |
| Web hosting (ASP.NET Core/Oxpecker) | BeerTaste.Web ONLY | Separation of concerns |
| Azure Table Storage operations | BeerTaste.Common ONLY | Single source of truth |
| Domain models | BeerTaste.Common | Shared across projects |
| Statistical analysis | BeerTaste.Common | Shared across projects |

## F# Code Patterns

### 1. Option Types (Preferred over null)

```fsharp
// GOOD - Use Option<'T>
let findBeer (beerId: int) : Beer option =
    beers |> List.tryFind (fun b -> b.Id = beerId)

// BAD - Don't use null
let findBeer (beerId: int) : Beer =
    beers |> List.find (fun b -> b.Id = beerId)  // throws if not found
```

### 2. Computation Expressions

```fsharp
// Use option { } for clean workflows
let processEvent shortName =
    option {
        let! setup = getConsoleSetup shortName
        let! beerTaste = setupBeerTaste setup
        let! beers = verifyBeers setup beerTaste
        let! tasters = verifyTasters setup beerTaste
        return! verifyScores setup beerTaste beers tasters
    }
```

### 3. Pattern Matching

```fsharp
// Use pattern matching for control flow
match state with
| DoesNotExist -> createSchema()
| ExistsWithoutScores -> promptForScores()
| ExistsWithScores -> validateScores()
| ExistsAndComplete -> showResults()
```

### 4. Piping and Composition

```fsharp
// Use |> operator for data transformation
beers
|> List.filter (fun b -> b.ABV > 5.0)
|> List.sortBy _.Name
|> List.map _.Id

// Use _.Property shorthand for property access in lambdas
tasters |> List.sortBy _.BirthYear
```

### 5. Record Types for Data

```fsharp
// Immutable records for domain models
type Beer = {
    Id: int
    Name: string
    BeerType: string
    ABV: float
    Price: float option  // Use Option for nullable fields
}
```

### 6. Discriminated Unions for State

```fsharp
// Model state with DUs
type ScoresSchemaState =
    | DoesNotExist
    | ExistsWithoutScores
    | ExistsWithScores
    | ExistsAndComplete
```

## Naming Conventions

- **PascalCase**: Types, Modules, Properties (`Beer`, `BeerEntity`, `TableStorage`)
- **camelCase**: Functions, Parameters, Local Bindings (`readBeers`, `shortName`, `excelFilePath`)
- **Descriptive**: Function names describe actions (`verifyBeers`, `setupBeerTaste`, `deleteBeersForBeerTaste`)
- **Domain Language**: Use business terms (BeerTaste, Tasters, Scores, not GenericEvent, Users, Data)

## Module Organization

Each `.fs` file is a module with `module BeerTaste.ProjectName.ModuleName` declaration:

```fsharp
module BeerTaste.Console.Beers

open System
open OfficeOpenXml
open BeerTaste.Common

// Types, functions, etc.
```

### Module Compilation Order (Important!)

F# requires dependencies to be compiled before dependents. The order in `.fsproj` matters:

```xml
<ItemGroup>
    <Compile Include="Configuration.fs" />  <!-- No dependencies -->
    <Compile Include="Beers.fs" />          <!-- Uses Configuration -->
    <Compile Include="Tasters.fs" />        <!-- Uses Configuration -->
    <Compile Include="Scores.fs" />         <!-- Uses Beers, Tasters -->
    <Compile Include="Workflow.fs" />       <!-- Uses all above -->
    <Compile Include="Program.fs" />        <!-- Entry point -->
</ItemGroup>
```

## Common Patterns

### 1. Excel I/O (Console Only)

```fsharp
// Always use 'use' for ExcelPackage disposal
let readBeers (filePath: string) =
    use package = new ExcelPackage(FileInfo(filePath))
    let worksheet = package.Workbook.Worksheets["Beers"]
    
    // Read data...
    beers

// Handle Norwegian decimal format
let norwegianToFloat (value: string) =
    value.Replace(",", ".") |> float
```

### 2. Azure Table Storage (Common Only)

```fsharp
// Use TableEntity with conversion functions
let entityToScore (entity: TableEntity) : Score option =
    option {
        let! beerId = entity.GetInt32("BeerId")
        let! tasterName = entity.GetString("TasterName")
        let scoreValue = entity.GetInt32("ScoreValue")
        return {
            BeerId = beerId
            TasterName = tasterName
            ScoreValue = scoreValue
        }
    }

// Delete before bulk insert pattern
let addBeers (tableClient: TableClient) (beerTasteGuid: Guid) (beers: Beer list) =
    deleteBeersForBeerTaste tableClient beerTasteGuid
    
    beers
    |> List.map (fun beer -> beerToEntity beerTasteGuid beer)
    |> List.iter (fun entity -> tableClient.UpsertEntity(entity))
```

### 3. User Interaction (Console Only)

```fsharp
// Use Spectre.Console with markup
let promptForDescription () =
    AnsiConsole.Ask<string>("[yellow]Enter description:[/]")

let confirmAction message =
    AnsiConsole.Confirm($"[green]{message}[/]")
```

### 4. Statistical Analysis (Common)

```fsharp
// Convert int scores to float for calculations
let beerAverages (scores: Score list) =
    scores
    |> List.choose (fun s -> s.ScoreValue |> Option.map float)
    |> List.groupBy _.BeerId
    |> List.map (fun (beerId, beerScores) ->
        let avg = beerScores |> List.averageBy (fun s -> s.ScoreValue |> Option.get |> float)
        beerId, avg)
```

## Error Handling

### Use Option Types

```fsharp
// Return None for expected failures
let getConsoleSetup shortName : ConsoleSetup option =
    try
        let connectionString = config["BeerTaste:TableStorageConnectionString"]
        if String.IsNullOrEmpty(connectionString) then
            AnsiConsole.MarkupLine("[red]Error: Connection string not found[/]")
            None
        else
            Some { /* setup data */ }
    with ex ->
        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]")
        None
```

### Fail Fast

```fsharp
// Return early with clear error messages
let validateBeers beers =
    if List.isEmpty beers then
        AnsiConsole.MarkupLine("[red]Error: No beers found[/]")
        None
    else
        Some beers
```

## Configuration

### User Secrets (Console)

```fsharp
// Access via IConfiguration
let config = 
    ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build()

let connectionString = config["BeerTaste:TableStorageConnectionString"]
let filesFolder = config["BeerTaste:FilesFolder"] ?? "./BeerTastes"
```

### EPPlus License (Console Program.fs)

```fsharp
// ALWAYS set license at startup
ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")
```

## Testing Approach

No formal unit tests. Validation through:
1. Script execution (`dotnet fsi scripts/BeerTaste.Report.fsx`)
2. Console application workflows
3. Report inspection for correctness

## Locale Considerations

- **Norwegian decimal format**: Comma separator (5,5 instead of 5.5)
- **Handling**: Use `norwegianToFloat` helper function
- **Score input**: Accept both `-` and empty as zero/missing

```fsharp
let norwegianToFloat (value: string) =
    value.Replace(",", ".") |> float

let parseScore (value: string) =
    match value.Trim() with
    | "" | "-" -> None
    | v -> v |> norwegianToFloat |> int |> Some
```

## When Adding New Features

1. **Determine project placement**:
   - Domain logic → BeerTaste.Common
   - Excel operations → BeerTaste.Console
   - Web UI → BeerTaste.Web

2. **Check dependencies**:
   - Don't add Excel/CLI deps to Common or Web
   - Don't add Web deps to Common or Console
   - Don't add Azure ops to Console or Web

3. **Follow compilation order**:
   - Add new .fs files in correct order in .fsproj
   - Dependencies must be compiled before dependents

4. **Use existing patterns**:
   - Option types for nullable values
   - Pattern matching for control flow
   - Piping for transformations
   - Computation expressions for workflows

5. **Maintain module cohesion**:
   - Keep related types and functions together
   - Single responsibility per module
   - Clear, descriptive names

## Web Application Specifics

### Oxpecker HTML Templates

```fsharp
// Use ViewEngine for HTML generation
let layout (title: string) (content: HtmlElement) =
    html() {
        head() {
            title(title)
            style() { rawText "/* CSS */" }
        }
        body() {
            content
        }
    }
```

### Routes Pattern

```fsharp
// Routes follow /{beerTasteGuid}/results/...
endpoints [
    route $"/{beerTasteGuid}/results" resultsIndex
    route $"/{beerTasteGuid}/results/best-beers" bestBeers
    route $"/{beerTasteGuid}/results/controversial" controversial
]
```

## Scripts (.fsx Files)

### Inline NuGet References

```fsharp
#r "nuget: EPPlus, 8.2.1"
#r "nuget: FSharp.Stats, 0.4.0"

// Load other scripts
#load "BeerTaste.Common.fsx"
```

### REPL-Friendly

Scripts are designed for interactive development in F# Interactive (FSI):

```bash
cd scripts
dotnet fsi BeerTaste.Report.fsx
```

## Common Pitfalls to Avoid

❌ **Don't**: Add EPPlus references to BeerTaste.Common or BeerTaste.Web  
✅ **Do**: Keep Excel operations in BeerTaste.Console

❌ **Don't**: Add Azure Table Storage operations to Console or Web  
✅ **Do**: Use BeerTaste.Common for all Azure operations

❌ **Don't**: Use null for missing values  
✅ **Do**: Use Option<'T> types

❌ **Don't**: Ignore module compilation order  
✅ **Do**: Maintain correct order in .fsproj ItemGroup

❌ **Don't**: Use mutable state unnecessarily  
✅ **Do**: Use immutable records and functional transformations

## Quick Reference: Where Things Go

| Feature | Project | Module | Reason |
|---------|---------|--------|--------|
| New domain type | Common | Beers.fs/Tasters.fs/etc | Shared across projects |
| Excel read/write | Console | Beers.fs/Tasters.fs/Scores.fs | EPPlus exclusive to Console |
| Azure CRUD | Common | Beers.fs/Tasters.fs/etc | Single source of truth |
| User prompt | Console | Workflow.fs | Spectre.Console exclusive to Console |
| Statistical function | Common | Results.fs | Shared analysis logic |
| Web page template | Web | templates/*.fs | Oxpecker exclusive to Web |
| Configuration loading | Console | Configuration.fs | Console-specific setup |

## Code Formatting

All F# code uses:
- **Stroustrup brace style**
- **120 character line width**
- **Enforced by Fantomas + EditorConfig**

Run before committing:
```bash
./Format.ps1
```

## Summary

When working with BeerTaste:
1. **Follow the three-layer architecture** - respect dependency boundaries
2. **Use F# functional patterns** - Option types, pattern matching, piping
3. **Keep modules focused** - single responsibility, clear names
4. **Respect compilation order** - dependencies before dependents
5. **Use appropriate tools per project** - EPPlus in Console, Oxpecker in Web, Azure in Common

Happy coding with GitHub Copilot! 🍺
