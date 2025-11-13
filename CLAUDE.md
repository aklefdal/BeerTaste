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
├── BeerTaste.Common/             # Shared F# class library
│   ├── Library.fs               # Shared code between Console and Web (currently placeholder)
│   └── BeerTaste.Common.fsproj  # .NET class library project
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
# Build the shared library (from root or BeerTaste.Common directory)
dotnet build BeerTaste.Common/BeerTaste.Common.fsproj
# Or: cd BeerTaste.Common && dotnet build

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

# Build all projects at once (from root)
dotnet build

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

### Shared Library (BeerTaste.Common)

A .NET 9.0 class library for code shared between Console and Web applications:

- **Purpose:** Domain models, Azure Table Storage operations, and shared business logic
- **Current State:** Placeholder with basic example code (`Say.hello`)
- **Architecture:** Will contain modules split from Console project:
  - **Storage.fs** - Azure Table Storage client setup and initialization
  - **Beers.fs** - Beer domain types (`Beer`, `BeerEntity`) and Azure storage operations
  - **Tasters.fs** - Taster domain types (`Taster`, `TasterEntity`) and Azure storage operations
  - **BeerTaste.fs** - BeerTaste event types (`BeerTasteEntity`) and Azure CRUD operations
  - **Scores.fs** - Score-related domain logic (if applicable)
- **Responsibilities:**
  - All Azure Table Storage entity types and operations
  - Domain model definitions
  - Data access layer for both Console and Web
- **Documentation:** Generates XML documentation file for IntelliSense support

### Console Application (BeerTaste.Console)

The console application focuses on **Excel I/O and user interaction**, delegating all Azure operations to BeerTaste.Common:

**Module Compilation Order** (bottom-up dependency chain):

1. **Configuration.fs** - Configuration and setup layer
   - Loads user secrets and environment variables
   - Sets up folder structure and copies Excel template
   - Returns `ConsoleSetup` record with all necessary context
   - Function: `getConsoleSetup` returns `Option<ConsoleSetup>`
   - References BeerTaste.Common.Storage for table clients

2. **Beers.fs** - Beer Excel I/O module (Console-specific)
   - Excel reading: `readBeers`, `rowToBeer`, `norwegianToFloat` helper
   - TastersSchema worksheet creation from BeerTaste.xlsx template
   - Uses EPPlus for all Excel operations
   - Delegates to BeerTaste.Common.Beers for Azure storage operations
   - **Split:** Domain types and Azure ops moved to Common

3. **Tasters.fs** - Taster Excel I/O module (Console-specific)
   - Excel reading: `readTasters`, `rowToTaster`
   - Uses EPPlus for all Excel operations
   - Delegates to BeerTaste.Common.Tasters for Azure storage operations
   - **Split:** Domain types and Azure ops moved to Common

4. **Scores.fs** - ScoreSchema Excel management module
   - `ScoresSchemaState` discriminated union: `DoesNotExist | ExistsWithoutScores | ExistsWithScores`
   - State detection: `getScoresSchemaState`, `hasScores`
   - ScoreSchema creation: `deleteAndCreateScoreSchema`
   - Combines beers and tasters into scoring matrix using EPPlus

5. **Workflow.fs** - Orchestration layer
   - User prompts: `promptForDescription`, `promptForDate`, `promptDoneEditingBeers`, `promptDoneEditingTasters`
   - Workflow functions: `setupBeerTaste`, `verifyBeers`, `verifyTasters`, `createScoreSchema`
   - Coordinates between Console modules and Common modules
   - Uses Spectre.Console for all user interaction
   - Handles user interaction and business logic flow

6. **Program.fs** - Application entry point
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
- Will reference BeerTaste.Common for shared types

**Project Dependencies:**

```
BeerTaste.Common (shared library)
    ↓
    ├─→ BeerTaste.Console (references Common in future)
    └─→ BeerTaste.Web (references Common in future)
```

**Data Flow:**

Console → Azure Tables + Excel Files → Scripts (analysis) → Reports/Slides → Web (future presentation)

## Code Conventions

### Project Dependency Guidelines

**IMPORTANT:** Strict separation of concerns across projects to maintain clean architecture:

**BeerTaste.Common (Shared Library)**
- ✅ Can reference: Core .NET libraries (System.*, FSharp.Core), Azure.Data.Tables
- ❌ Cannot reference: EPPlus, Spectre.Console, System.Console, ASP.NET Core web libraries
- **Purpose:** Domain models, Azure Table Storage operations, shared business logic
- **Principle:** No UI dependencies - only data access and domain logic

**BeerTaste.Console (Console Application)**
- ✅ Can reference: BeerTaste.Common, EPPlus, Spectre.Console, System.Console
- ❌ Cannot reference: ASP.NET Core, Oxpecker, or any web hosting libraries
- **Exclusive rights:** Only project that can use EPPlus for Excel operations
- **Exclusive rights:** Only project that can use Spectre.Console for CLI interactions
- **Principle:** Console UI and Excel I/O only - delegates Azure operations to Common

**BeerTaste.Web (Web Application)**
- ✅ Can reference: BeerTaste.Common, ASP.NET Core, Oxpecker
- ❌ Cannot reference: EPPlus, Spectre.Console, System.Console
- **Exclusive rights:** Only project that can use ASP.NET Core and web hosting libraries
- **Principle:** Web UI only - delegates Azure operations to Common

**Rationale:**
- **Common owns all Azure Table Storage operations** - single source of truth for data access
- **UI layers stay clean** - Console and Web only handle presentation
- Clear boundaries for where functionality belongs
- Enables independent evolution of Console and Web UIs
- Shared library can be referenced by any future project

**Quick Reference Table:**

| Dependency | BeerTaste.Common | BeerTaste.Console | BeerTaste.Web |
|------------|------------------|-------------------|---------------|
| Core .NET (System.*, FSharp.Core) | ✅ | ✅ | ✅ |
| BeerTaste.Common | N/A | ✅ | ✅ |
| Azure.Data.Tables | ✅ (owns storage) | ❌ | ❌ |
| EPPlus | ❌ | ✅ (exclusive) | ❌ |
| Spectre.Console | ❌ | ✅ (exclusive) | ❌ |
| System.Console | ❌ | ✅ (exclusive) | ❌ |
| ASP.NET Core / Oxpecker | ❌ | ❌ | ✅ (exclusive) |

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

### Shared Library (Azure Table Storage & Domain)

- `BeerTaste.Common/Storage.fs` - Azure Table Storage client setup (`BeerTasteTableStorage` class)
- `BeerTaste.Common/Beers.fs` - Beer domain types (`Beer`, `BeerEntity`) and Azure operations
- `BeerTaste.Common/Tasters.fs` - Taster domain types (`Taster`, `TasterEntity`) and Azure operations
- `BeerTaste.Common/BeerTaste.fs` - BeerTaste event types (`BeerTasteEntity`) and Azure CRUD operations
- `BeerTaste.Common/Scores.fs` - Score-related domain logic (if applicable)
- `BeerTaste.Common/BeerTaste.Common.fsproj` - Class library project configuration

### Console Application Modules (Excel I/O & UI)

- `BeerTaste.Console/Configuration.fs` - Config loading, folder setup, references Common.Storage
- `BeerTaste.Console/Beers.fs` - Excel I/O: `readBeers`, `norwegianToFloat`, TastersSchema creation
- `BeerTaste.Console/Tasters.fs` - Excel I/O: `readTasters`, `rowToTaster`
- `BeerTaste.Console/Scores.fs` - ScoreSchema state detection and creation with user warnings
- `BeerTaste.Console/Workflow.fs` - Orchestration with Spectre.Console prompts, calls Common for Azure ops
- `BeerTaste.Console/Program.fs` - Entry point with EPPlus license and pattern matching orchestration
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

### Shared Library (BeerTaste.Common)

- **Purpose:** Data access layer and domain models shared between Console and Web applications
- **Current State:** Placeholder library with example code
- **Architecture Plan:** Will contain modules split from Console:
  - **Storage.fs** - Azure Table Storage initialization and client management
  - **Beers.fs** - Beer domain types and Azure CRUD operations
  - **Tasters.fs** - Taster domain types and Azure CRUD operations
  - **BeerTaste.fs** - BeerTaste event types and Azure CRUD operations
  - **Scores.fs** - Score domain logic (if shared)
- **Project Type:** .NET class library targeting net9.0
- **XML Documentation:** Enabled for IntelliSense support in consuming projects
- **Dependencies:** Azure.Data.Tables only (no UI dependencies)
  - ✅ Azure.Data.Tables for all storage operations
  - ❌ No EPPlus, Spectre.Console, System.Console, or ASP.NET Core
  - Keep it focused on data access and domain logic

### Console Application

- **Modular Architecture:** 8 modules with clear separation of concerns
- **Compilation Order Matters:** F# requires dependencies to be compiled first (see .fsproj ItemGroup order)
- **Domain-Driven Design:** Each domain (Beers, Tasters, BeerTaste, Scores) has its own module
- **Option-Based Flow:** Functions return `Option` types, orchestration uses pattern matching
- **Storage Encapsulation:** All Azure table initialization happens in Storage.fs
- **Configuration First:** ConsoleSetup record provides all context to workflow functions

### Adding New Features

1. Determine if the feature is shared between Console and Web:
   - If shared: Add to BeerTaste.Common (check dependency guidelines!)
   - If Console-specific: Add to appropriate Console module
   - If Web-specific: Add to Web project
2. **Check dependency guidelines:**
   - Excel operations? → Must go in Console (EPPlus exclusive)
   - CLI interactions? → Must go in Console (Spectre.Console exclusive)
   - Web hosting? → Must go in Web (ASP.NET Core exclusive)
   - Pure domain logic? → Can go in Common (no external dependencies)
3. For Console features, determine which domain module it belongs to (Beers, Tasters, Scores, etc.)
4. Add types and functions to that module
5. If workflow changes needed, update Workflow.fs
6. Keep Program.fs minimal - just orchestration
7. Maintain compilation order in .fsproj

### Scripts (.fsx files)

- Designed for REPL-style interactive development in FSI
- **Important:** Scripts reference Excel files by name only (e.g., `"ØJ Ølsmaking 2024.xlsx"`), so they should be run from the `scripts/` directory: `cd scripts && dotnet fsi BeerTaste.Report.fsx`
- Multiple Excel files in `scripts/` represent different tasting events
- All analysis workflow files organized in `scripts/`

### Web Application

- Currently a placeholder ("Hello World")
- Will be developed for results presentation
- Oxpecker framework for F#-friendly web development
- Text files need to use CRLF for line endings