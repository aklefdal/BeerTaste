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
├── BeerTaste.Console/            # Compiled F# console program
│   ├── Program.fs               # Main program for Azure Table Storage and Excel management
│   ├── BeerTaste.Console.fsproj # .NET project file
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

The project uses a **layered F# script architecture** where each layer is a self-contained .fsx file with inline NuGet references:

1. **Common functions** (`scripts/BeerTaste.Common.fsx`)
   - Domain models: `Beer`, `Taster`, scoring types
   - Excel I/O using EPPlus
   - Norwegian locale handling (`norwegianToFloat` converts comma decimals)
   - Expected Excel schema: "Beers" and "Tasters" worksheets

2. **Preparation functions** (`scripts/BeerTaste.Preparations.fsx`)
   - Generates Excel templates for new tasting events
   - Creates proper schema for data entry

3. **Result functions** (`scripts/BeerTaste.Results.fsx`)
   - Loads Common layer with `#load "BeerTaste.Common.fsx"`
   - Statistical analysis: Pearson correlations, standard deviation, rankings
   - Uses FSharp.Stats for computations

4. **Reporting function** (`scripts/BeerTaste.Report.fsx`)
   - Loads Results layer
   - Generates Markdown reports with 6 analysis sections
   - Creates individual slide files for presentation

5. **Console program** (`BeerTaste.Console/Program.fs` + `BeerTaste.Console/BeerTaste.Console.fsproj`)
   - Manages BeerTaste events in Azure Table Storage
   - Takes a short name as parameter and checks if it exists in Azure
   - If new: prompts for description and date, creates Azure entry
   - Creates event folder structure: `{FilesFolder}/{shortName}/`
   - Copies BeerTaste.xlsx template to event folder
   - Reads beers from Excel, creates TastersSchema worksheet (if 2+ beers)
   - Reads tasters from Excel, creates ScoreSchema worksheet (if 2+ tasters)
   - Configuration management with user secrets
   - User secrets ID: `beertaste-5f8f1d6d-b9a5-4e4a-b0d0-3c3c52e6c6c2`
   - Configurable settings: `BeerTaste:TableStorageConnectionString`, `BeerTaste:FilesFolder`

6. **Web application** (`BeerTaste.Web/Program.fs` + `BeerTaste.Web/BeerTaste.Web.fsproj`)
   - ASP.NET Core web server using Oxpecker framework
   - Currently a basic "Hello World" endpoint
   - Future: Will present tasting results and analysis via web interface
   - Runs on http://localhost:5000 (or https://localhost:5001)

**Data Flow:**

Console Program → Excel Files ({FilesFolder}/{shortName}/) → Scripts (Common → Results → Report) → Markdown/Slides (scripts/) → Web Application (future)

## Code Conventions

- **Formatting:** Stroustrup brace style, 120 character line width (enforced by Fantomas + EditorConfig)
- **Locale:** Norwegian decimal format (comma separator) handled by `norwegianToFloat`
- **F# Scripts:** Use inline `#r "nuget: PackageName, Version"` for dependencies
- **Functional Style:** Heavy use of piping (`|>`), composition, and immutable data structures
- **EPPlus License:** Always call `ExcelPackage.License.SetNonCommercialPersonal("Alf Kåre Lefdal")` before Excel operations

## Key Files

- `scripts/BeerTaste.Common.fsx` - Core data models and Excel parsing
- `scripts/BeerTaste.Results.fsx` - Statistical analysis functions
- `scripts/BeerTaste.Report.fsx` - Report generation
- `scripts/BeerTaste.Preparations.fsx` - Excel template generation
- `BeerTaste.Console/Program.fs` - Console program for event management and Excel operations
- `BeerTaste.Console/BeerTaste.Console.fsproj` - Console project configuration
- `BeerTaste.Web/Program.fs` - Web application for results presentation
- `BeerTaste.Web/BeerTaste.Web.fsproj` - Web project configuration
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
- **Important:** Scripts reference Excel files by name only (e.g., `"ØJ Ølsmaking 2024.xlsx"`), so they should be run from the `scripts/` directory: `cd scripts && dotnet fsi BeerTaste.Report.fsx`
- Multiple Excel files in `scripts/` represent different tasting events (catalog, annual events)
- All analysis workflow files (Excel, scripts, generated reports, presentations) are organized in `scripts/`
- Console program manages event lifecycle: Azure registration, folder setup, Excel template creation, schema generation
- Web application is currently a placeholder and will be developed for results presentation
- Norwegian language context throughout (field names, output text)
