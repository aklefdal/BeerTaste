# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BeerTaste is an F# data analysis system for organizing and analyzing beer tasting events. It processes beer ratings from multiple tasters and generates statistical insights including best-rated beers, most controversial beers, taster similarity analysis, and preference correlations with ABV and price.

## Tech Stack

- F# with .NET 9.0
- F# Script files (.fsx) for analysis logic with inline NuGet references
- EPPlus 8.2.1 for Excel I/O (licensed for non-commercial personal use)
- FSharp.Stats 0.4.0 for statistical analysis
- Azure.Data.Tables 12.11.0 for Azure Table Storage integration
- Oxpecker 1.5.0 for web presentation (F# web framework)
- Spectre.Console 0.53.0 for CLI interactions
- Fantomas 7.0.3 for code formatting

## Repository Structure

```
beertaste/
├── BeerTaste.Console/            # Compiled F# console program (modular architecture)
│   ├── Storage.fs               # Azure Table Storage client setup
│   ├── Configuration.fs         # Configuration loading and folder setup
│   ├── Beers.fs                 # Beer domain: types, Excel I/O, TastersSchema, Azure storage
│   ├── Tasters.fs               # Taster domain: types, Excel I/O, Azure storage
│   ├── BeerTaste.fs             # BeerTaste event management and Azure operations
│   ├── Scores.fs                # ScoreSchema state detection and creation
│   ├── Workflow.fs              # Orchestration of beer/taster workflows
│   ├── Program.fs               # Application entry point
│   ├── BeerTaste.Console.fsproj # .NET project file with compilation order
│   └── BeerTaste.xlsx           # Beer catalog template
├── BeerTaste.Web/                # F# web application (future results presentation)
│   ├── Program.fs               # ASP.NET Core web server with Oxpecker
│   ├── BeerTaste.Web.fsproj     # .NET web project file
│   └── README.md                # Web project documentation
├── scripts/                      # F# scripts, data files, and outputs
│   ├── BeerTaste.Common.fsx     # Core data models and Excel I/O
│   ├── BeerTaste.Preparations.fsx # Excel template generation
│   ├── BeerTaste.Results.fsx    # Statistical analysis
│   ├── BeerTaste.Report.fsx     # Report generation
│   ├── *.xlsx                   # Tasting event Excel files
│   ├── Present-Slides.ps1       # Slide presentation tool
│   ├── presentation.html        # HTML results viewer
│   ├── slides/                  # Generated presentation slides
│   ├── *Results.md              # Generated Markdown reports
│   └── *Results.pdf             # Generated PDF reports
├── Format.ps1                    # Format all F# code
├── Check-Format.ps1              # Check code formatting
└── CLAUDE.md                     # This file
```

## Essential Commands

### Build and Run

```powershell
# Build the console program (from root or BeerTaste.Console directory)
dotnet build BeerTaste.Console/BeerTaste.Console.fsproj
# Or: cd BeerTaste.Console && dotnet build

# Run the console program with a short name parameter
dotnet run --project BeerTaste.Console/BeerTaste.Console.fsproj -- <short-name>
# Or: cd BeerTaste.Console && dotnet run -- <short-name>

# Build the web application (from root or BeerTaste.Web directory)
dotnet build BeerTaste.Web/BeerTaste.Web.fsproj
# Or: cd BeerTaste.Web && dotnet build

# Run the web application (from root or BeerTaste.Web directory)
dotnet run --project BeerTaste.Web/BeerTaste.Web.fsproj
# Or: cd BeerTaste.Web && dotnet run
# Web server will start at http://localhost:5000 (or https://localhost:5001)

# Execute F# scripts directly (run from scripts directory)
cd scripts

# Generate Tasters Schema and Score Schema in Excel
dotnet fsi BeerTaste.Preparations.fsx

# Generate result report from tasting data
dotnet fsi BeerTaste.Report.fsx
```

### Code Formatting

```powershell
# Format all F# files (use this before commits)
.\Format.ps1

# Check formatting without modifying (for CI)
.\Check-Format.ps1
# Or: dotnet fantomas . --check
```

### Azure Configuration

```powershell
# Set Azure Table Storage connection string in user secrets (from BeerTaste.Console directory)
cd BeerTaste.Console
dotnet user-secrets set "BeerTaste:TableStorageConnectionString" "<your-connection-string>"

# Set custom folder path for Excel files and other data files (optional)
# If not set, defaults to ./BeerTastes relative to current directory
dotnet user-secrets set "BeerTaste:FilesFolder" "C:\path\to\your\folder"

# Or use environment variables
$env:BeerTaste__TableStorageConnectionString = "<your-connection-string>"
$env:BeerTaste__FilesFolder = "C:\path\to\your\folder"
```

### Presentation

```powershell
# Launch interactive slide navigator for tasting results (from root)
.\scripts\Present-Slides.ps1

# Open HTML presentation viewer
# Open scripts/presentation.html in browser
```

## Architecture

### Console Application (BeerTaste.Console)

The console application follows a **modular, domain-driven architecture** with clear separation of concerns:

**Module Compilation Order** (bottom-up dependency chain):
1. **Storage.fs** - Infrastructure layer
   - `BeerTasteTableStorage` class encapsulates Azure Table Storage setup
   - Creates and manages three table clients: `beertaste`, `beers`, `tasters`
   - Single responsibility: storage infrastructure initialization

2. **Configuration.fs** - Configuration and setup layer
   - Loads user secrets and environment variables
   - Sets up folder structure and copies Excel template
   - Returns `ConsoleSetup` record with all necessary context
   - Function: `getConsoleSetup` returns `Option<ConsoleSetup>`

3. **Beers.fs** - Beer domain module
   - `Beer` record type and `BeerEntity` for Azure storage
   - Excel reading: `readBeers`, `rowToBeer`, `norwegianToFloat` helper
   - TastersSchema worksheet creation from BeerTaste.xlsx template
   - Azure operations: `addBeers`, `deleteBeersForBeerTaste`
   - All beer-related functionality in one cohesive module

4. **Tasters.fs** - Taster domain module
   - `Taster` record type and `TasterEntity` for Azure storage
   - Excel reading: `readTasters`, `rowToTaster`
   - Azure operations: `addTasters`, `deleteTastersForPartitionKey`
   - All taster-related functionality in one cohesive module

5. **BeerTaste.fs** - BeerTaste event domain module
   - `BeerTasteEntity` for Azure storage
   - Event management: `getBeerTasteGuid`, `addBeerTaste`, `getBeerTastePartitionKey`
   - Handles BeerTaste lifecycle in Azure Table Storage

6. **Scores.fs** - ScoreSchema management module
   - `ScoresSchemaState` discriminated union: `DoesNotExist | ExistsWithoutScores | ExistsWithScores`
   - State detection: `getScoresSchemaState`, `hasScores`
   - ScoreSchema creation: `deleteAndCreateScoreSchema`
   - Combines beers and tasters into scoring matrix

7. **Workflow.fs** - Orchestration layer
   - User prompts: `promptForDescription`, `promptForDate`, `promptDoneEditingBeers`, `promptDoneEditingTasters`
   - Workflow functions: `setupBeerTaste`, `verifyBeers`, `verifyTasters`, `createScoreSchema`
   - Coordinates between domain modules with pattern matching and Option types
   - Handles user interaction and business logic flow

8. **Program.fs** - Application entry point
   - EPPlus license setup
   - Minimal orchestration using Workflow functions
   - Pattern matching on Option types for clean error handling
   - Exit codes: 0 for success, 1 for errors

### Script Architecture (scripts/)

Separate **layered F# script architecture** for analysis:

1. **BeerTaste.Common.fsx** - Domain models and Excel I/O
2. **BeerTaste.Preparations.fsx** - Excel template generation
3. **BeerTaste.Results.fsx** - Statistical analysis (Pearson correlations, rankings)
4. **BeerTaste.Report.fsx** - Markdown report generation

### Web Application (BeerTaste.Web)

- ASP.NET Core with Oxpecker framework
- Currently minimal ("Hello World")
- Future: results presentation and analysis visualization

**Data Flow:**

Console → Azure Tables + Excel Files → Scripts (analysis) → Reports/Slides → Web (future presentation)

## Code Conventions

### General Style

- **Formatting:** Stroustrup brace style, 120 character line width (enforced by Fantomas + EditorConfig)
- **Module Organization:** Each `.fs` file is a module with `module BeerTaste.Console.ModuleName` declaration
- **Compilation Order:** Dependencies must be compiled before dependents (F# compiler requirement)
- **Locale:** Norwegian decimal format (comma separator) handled by `norwegianToFloat`

### F# Functional Patterns

- **Option Types:** Prefer `Option<'T>` over null checks (e.g., `getConsoleSetup` returns `ConsoleSetup option`)
- **Pattern Matching:** Use `match` expressions for control flow and Option handling
- **Discriminated Unions:** Model state with DUs (e.g., `ScoresSchemaState = DoesNotExist | ExistsWithoutScores | ExistsWithScores`)
- **Piping:** Use `|>` operator for function composition and data transformation
- **Function Composition:** Small, focused functions with clear single responsibilities
- **Immutability:** Record types are immutable by default
- **Expression-Oriented:** Functions return values rather than using side effects where possible

### Domain Modeling

- **Records for Data:** Use record types for domain models (`Beer`, `Taster`, `ConsoleSetup`)
- **Classes for Azure Entities:** Use classes implementing `ITableEntity` for Azure Table Storage
- **Encapsulation:** `BeerTasteTableStorage` class encapsulates all table client initialization
- **Type Safety:** Strong typing throughout, minimal use of `string` or generic types

### Module Guidelines

- **Domain Cohesion:** Related types and functions grouped in same module (Beers.fs has Beer type, BeerEntity, and all beer operations)
- **Single Responsibility:** Each module has one clear purpose
- **Explicit Dependencies:** Module references are clear (e.g., `open BeerTaste.Console.Beers`)
- **Minimize Cross-Module Dependencies:** Keep modules loosely coupled

### Naming Conventions

- **PascalCase:** Types, modules, properties (`Beer`, `BeerEntity`, `TableStorage`)
- **camelCase:** Functions, parameters, local bindings (`readBeers`, `shortName`, `excelFilePath`)
- **Descriptive Names:** Function names describe what they do (`verifyBeers`, `setupBeerTaste`, `deleteTastersForPartitionKey`)
- **Domain Language:** Use business domain terms (BeerTaste, Tasters, Scores)

### Error Handling

- **Option Types:** Return `None` for expected failures, `Some value` for success
- **Try-Catch:** Use sparingly, primarily for external I/O (Excel, Azure)
- **User Messaging:** Display errors with Spectre.Console markup for colored output
- **Fail Fast:** Return early with clear error messages

### Azure Table Storage

- **Entity Classes:** Implement `ITableEntity` interface
- **PartitionKey Strategy:** BeerTaste GUID for partitioning beers and tasters
- **RowKey Strategy:** Natural identifiers (shortName for BeerTaste, Beer.Id for beers, Taster.Name for tasters)
- **Delete Before Insert:** Pattern of deleting existing entities before bulk insert

### Excel Operations

- **EPPlus License:** Always call `ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")` in Program.fs
- **Using Statements:** Use `use` keyword for ExcelPackage disposal
- **Template Pattern:** Copy from BeerTaste.xlsx template for consistency
- **Worksheet Management:** Delete existing worksheet before creating new one
- **Norwegian Locale:** Handle comma decimal separators with `norwegianToFloat`

### Scripts (.fsx files)

- **Inline Dependencies:** Use `#r "nuget: PackageName, Version"` for package references
- **Module Loading:** Use `#load "OtherScript.fsx"` for dependencies
- **REPL-Friendly:** Designed for interactive development in F# Interactive

## Key Files

### Console Application Modules (in compilation order)

- `BeerTaste.Console/Storage.fs` - Azure Table Storage initialization
- `BeerTaste.Console/Configuration.fs` - Config loading, folder setup, returns ConsoleSetup
- `BeerTaste.Console/Beers.fs` - Beer domain: types, Excel I/O, TastersSchema, Azure operations
- `BeerTaste.Console/Tasters.fs` - Taster domain: types, Excel I/O, Azure operations
- `BeerTaste.Console/BeerTaste.fs` - BeerTaste event management and GUID operations
- `BeerTaste.Console/Scores.fs` - ScoreSchema state detection and creation with user warnings
- `BeerTaste.Console/Workflow.fs` - Orchestration layer with user prompts and workflow coordination
- `BeerTaste.Console/Program.fs` - Minimal entry point with pattern matching orchestration
- `BeerTaste.Console/BeerTaste.Console.fsproj` - Project file with module compilation order
- `BeerTaste.Console/BeerTaste.xlsx` - Excel template for events

### Analysis Scripts

- `scripts/BeerTaste.Common.fsx` - Core data models and Excel parsing
- `scripts/BeerTaste.Results.fsx` - Statistical analysis functions
- `scripts/BeerTaste.Report.fsx` - Report generation
- `scripts/BeerTaste.Preparations.fsx` - Excel template generation

### Web Application

- `BeerTaste.Web/Program.fs` - Web application for results presentation
- `BeerTaste.Web/BeerTaste.Web.fsproj` - Web project configuration

### Configuration

- `.editorconfig` - F# formatting rules (crucial for consistency)
- User secrets (via `dotnet user-secrets`): `BeerTaste:TableStorageConnectionString`, `BeerTaste:FilesFolder`

## Excel Data Schema

The system expects Excel files with two worksheets:

### "Beers" worksheet (columns A-I)

- A: Id (int)
- B: Name (string)
- C: BeerType (string)
- D: Origin (string)
- E: Producer (string)
- F: ABV (Norwegian float with comma)
- G: Volume (Norwegian float with comma)
- H: Price (Norwegian float with comma)
- I: Packaging (string)

### "Tasters" worksheet (columns A-C)

- A: Name (string)
- B: Email (string)
- C: BirthYear (int)

### "TastersSchema" worksheet

Listing the same as the Beers worksheet, but more suited for printing.

### "ScoreSchema" worksheet

Where the administrator (me) adds all the scores given by the tasters. These are used to process the results, which further ends up in the markdown report file.

## Development Notes

### General

- No formal unit tests - validation is done through script execution and report inspection
- Norwegian language context throughout (field names, output text)
- User secrets ID: `beertaste-5f8f1d6d-b9a5-4e4a-b0d0-3c3c52e6c6c2`

### Console Application

- **Modular Architecture:** 8 modules with clear separation of concerns
- **Compilation Order Matters:** F# requires dependencies to be compiled first (see .fsproj ItemGroup order)
- **Domain-Driven Design:** Each domain (Beers, Tasters, BeerTaste, Scores) has its own module
- **Option-Based Flow:** Functions return `Option` types, orchestration uses pattern matching
- **Storage Encapsulation:** All Azure table initialization happens in Storage.fs
- **Configuration First:** ConsoleSetup record provides all context to workflow functions

### Adding New Features

1. Determine which domain module the feature belongs to (Beers, Tasters, Scores, etc.)
2. Add types and functions to that module
3. If workflow changes needed, update Workflow.fs
4. Keep Program.fs minimal - just orchestration
5. Maintain compilation order in .fsproj

### Scripts (.fsx files)

- Designed for REPL-style interactive development in FSI
- **Important:** Scripts reference Excel files by name only (e.g., `"ØJ Ølsmaking 2024.xlsx"`), so they should be run from the `scripts/` directory: `cd scripts && dotnet fsi BeerTaste.Report.fsx`
- Multiple Excel files in `scripts/` represent different tasting events
- All analysis workflow files organized in `scripts/`

### Web Application

- Currently a placeholder ("Hello World")
- Will be developed for results presentation
- Oxpecker framework for F#-friendly web development
