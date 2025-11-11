# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BeerTaste is an F# data analysis system for organizing and analyzing beer tasting events. It processes beer ratings from multiple tasters and generates statistical insights including best-rated beers, most controversial beers, taster similarity analysis, and preference correlations with ABV and price.

## Tech Stack

- F# with .NET 9.0
- F# Script files (.fsx) for analysis logic with inline NuGet references
- EPPlus 8.2.1 for Excel I/O (licensed for non-commercial personal use)
- FSharp.Stats 0.4.0 for statistical analysis
- Azure.Data.Tables 12.9.1 for Azure Table Storage integration
- Fantomas 7.0.3 for code formatting

## Essential Commands

### Build and Run

```powershell
# Build the compiled program
dotnet build

# Run the compiled program (Azure connectivity test)
dotnet run

# Execute F# scripts directly
# This script to generate Tasters Schema and Score Schema, both in Excel
dotnet fsi BeerTaste.Prepations.fsx

# This script to generate result report from the given
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
# Set Azure Table Storage connection string in user secrets
dotnet user-secrets set "TableStorage:ConnectionString" "<your-connection-string>"
```

## Architecture

The project uses a **layered F# script architecture** where each layer is a self-contained .fsx file with inline NuGet references:

1. **Common functions** (`BeerTaste.Common.fsx`)
   - Domain models: `Beer`, `Taster`, scoring types
   - Excel I/O using EPPlus
   - Norwegian locale handling (`norwegianToFloat` converts comma decimals)
   - Expected Excel schema: "Beers" and "Tasters" worksheets

2. **Preparation functions** (`BeerTaste.Preparations.fsx`)
   - Generates Excel templates for new tasting events
   - Creates proper schema for data entry

3. **Result functions** (`BeerTaste.Results.fsx`)
   - Loads Common layer with `#load "BeerTaste.Common.fsx"`
   - Statistical analysis: Pearson correlations, standard deviation, rankings
   - Uses FSharp.Stats for computations

4. **Reporting function** (`BeerTaste.Report.fsx`)
   - Loads Results layer
   - Generates Markdown reports with 6 analysis sections
   - Creates individual slide files for presentation

5. **Next version** (`Program.fs` + `beertaste.fsproj`)
   - Compiled F# program for doing what is currently in the F# script
   - Will upload data to Azure Table Storage
   - Configuration management with user secrets
   - User secrets ID: `beertaste-5f8f1d6d-b9a5-4e4a-b0d0-3c3c52e6c6c2`

**Data Flow:**

Excel Files → Common (parsing) → Results (analysis) → Report (output) → Markdown/Slides

## Code Conventions

- **Formatting:** Stroustrup brace style, 120 character line width (enforced by Fantomas + EditorConfig)
- **Locale:** Norwegian decimal format (comma separator) handled by `norwegianToFloat`
- **F# Scripts:** Use inline `#r "nuget: PackageName, Version"` for dependencies
- **Functional Style:** Heavy use of piping (`|>`), composition, and immutable data structures
- **EPPlus License:** Always call `ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")` before Excel operations

## Key Files

- `BeerTaste.Common.fsx` - Core data models and Excel parsing
- `BeerTaste.Results.fsx` - Statistical analysis functions
- `BeerTaste.Report.fsx` - Report generation
- `BeerTaste.Preparations.fsx` - Excel template generation
- `Program.fs` - Beginning of next version
- `beertaste.fsproj` - .NET project configuration
- `.editorconfig` - F# formatting rules (crucial for consistency)

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

- No formal unit tests - validation is done through script execution and report inspection
- Scripts are designed for REPL-style interactive development in FSI
- Multiple Excel files represent different tasting events (catalog, annual events)
- Norwegian language context throughout (field names, output text)
