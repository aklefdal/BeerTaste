# BeerTaste 🍺

A comprehensive F# data analysis system for organizing and analyzing beer tasting events. BeerTaste processes beer ratings from multiple tasters and generates statistical insights including best-rated beers, most controversial beers, taster similarity analysis, and preference correlations.

## Features

- **Event Management**: Create and manage beer tasting events with Excel-based data entry
- **Statistical Analysis**: Generate comprehensive insights from tasting data
  - Best-rated beers by average score
  - Most controversial beers by standard deviation
  - Taster similarity analysis using Pearson correlations
  - Preference correlations with ABV and price metrics
- **Multiple Interfaces**:
  - Console application for event setup and data management
  - Web application for results presentation
  - F# scripts for custom analysis
- **Azure Integration**: Store event data in Azure Table Storage
- **Excel I/O**: Read and write beer catalogs, taster lists, and scoring matrices

## Quick Start

### Prerequisites

- .NET 9.0 SDK or later
- Azure Storage Account (for persistence)
- Excel for data entry (optional - can use other tools to edit .xlsx files)

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/aklefdal/BeerTaste.git
   cd BeerTaste
   ```

2. Build the projects:
   ```bash
   dotnet build
   ```

3. Configure Azure Table Storage (from BeerTaste.Console directory):
   ```bash
   cd BeerTaste.Console
   dotnet user-secrets set "BeerTaste:TableStorageConnectionString" "your-connection-string"
   ```

4. Run the console application:
   ```bash
   dotnet run --project BeerTaste.Console/BeerTaste.Console.fsproj -- <event-short-name>
   ```

5. Launch the web application to view results:
   ```bash
   dotnet run --project BeerTaste.Web/BeerTaste.Web.fsproj
   ```
   Navigate to `http://localhost:5000` or `https://localhost:5001`

## Project Structure

```
BeerTaste/
├── BeerTaste.Common/          # Shared library (domain models, Azure ops, statistics)
├── BeerTaste.Console/         # Console UI for event management
├── BeerTaste.Web/             # ASP.NET Core web app for results presentation
└── scripts/                   # F# scripts for custom analysis
```

### BeerTaste.Common

Shared .NET class library containing:
- Domain models (Beer, Taster, Score, BeerTaste event)
- Azure Table Storage operations
- Statistical analysis functions
- No UI dependencies - pure business logic

### BeerTaste.Console

Console application for event management:
- Excel I/O for beers, tasters, and scores
- Interactive prompts using Spectre.Console
- Event creation and validation workflows
- Delegates data persistence to BeerTaste.Common

### BeerTaste.Web

ASP.NET Core web application with Oxpecker framework:
- Results presentation with 6 analysis pages
- Black and white responsive theme
- Real-time data fetching from Azure Table Storage
- Auto-opens when scores are complete

### Scripts

F# scripts (.fsx files) for custom analysis:
- `BeerTaste.Common.fsx` - Data models and Excel parsing
- `BeerTaste.Preparations.fsx` - Template generation
- `BeerTaste.Results.fsx` - Statistical analysis
- `BeerTaste.Report.fsx` - Markdown/PDF report generation

## Essential Commands

### Building and Running

```bash
# Build all projects
dotnet build

# Build specific project
dotnet build BeerTaste.Common/BeerTaste.Common.fsproj
dotnet build BeerTaste.Console/BeerTaste.Console.fsproj
dotnet build BeerTaste.Web/BeerTaste.Web.fsproj

# Run console app (requires event short name)
dotnet run --project BeerTaste.Console/BeerTaste.Console.fsproj -- myevent

# Run web app
dotnet run --project BeerTaste.Web/BeerTaste.Web.fsproj

# Execute F# scripts (from scripts directory)
cd scripts
dotnet fsi BeerTaste.Preparations.fsx
dotnet fsi BeerTaste.Report.fsx
```

### Code Formatting

```bash
# Format all F# files
./Format.ps1

# Check formatting (CI)
./Check-Format.ps1
```

## Architecture Principles

### Clean Architecture

- **BeerTaste.Common**: Domain logic and data access (no UI dependencies)
- **BeerTaste.Console**: Console UI (EPPlus, Spectre.Console exclusive)
- **BeerTaste.Web**: Web UI (ASP.NET Core, Oxpecker exclusive)

### Dependency Rules

| Project | Can Reference | Cannot Reference |
|---------|---------------|------------------|
| **Common** | Core .NET, Azure.Data.Tables, FSharp.Stats | EPPlus, Spectre.Console, ASP.NET Core |
| **Console** | Common, EPPlus, Spectre.Console | ASP.NET Core, Oxpecker |
| **Web** | Common, ASP.NET Core, Oxpecker | EPPlus, Spectre.Console |

### F# Functional Patterns

- **Option types** for nullable values (`Option<'T>`)
- **Computation expressions** for clean workflows (`option { }`)
- **Pattern matching** for control flow
- **Discriminated unions** for state modeling
- **Immutable records** for data
- **Function composition** with `|>` operator
- **Underscore shorthand** for property access (`_.Property`)

## Data Model

### Beer Catalog (Excel "Beers" worksheet)

- Id, Name, BeerType, Origin, Producer
- ABV (alcohol by volume), Volume, Price, Packaging

### Tasters (Excel "Tasters" worksheet)

- Name, Email, BirthYear

### Scores (Excel "ScoreSchema" worksheet)

- Matrix of beers × tasters with integer scores (1-10)
- Norwegian decimal format supported (comma separator)
- Missing scores represented as empty or `-`

## Statistical Analysis

The system computes:

1. **Beer Rankings**: Average scores and standard deviations
2. **Taster Correlations**: Pearson correlation coefficients between tasters
3. **Deviation Analysis**: Tasters most different from group average
4. **ABV Preferences**: Correlation between ratings and alcohol content
5. **Value Analysis**: Correlation between ratings and price per ABV

## Configuration

### User Secrets

Store sensitive configuration using .NET user secrets:

```bash
cd BeerTaste.Console
dotnet user-secrets set "BeerTaste:TableStorageConnectionString" "<connection-string>"
dotnet user-secrets set "BeerTaste:FilesFolder" "C:\path\to\data\folder"
```

### Environment Variables

Alternatively, use environment variables:

```bash
# PowerShell
$env:BeerTaste__TableStorageConnectionString = "<connection-string>"
$env:BeerTaste__FilesFolder = "C:\path\to\data\folder"

# Bash
export BeerTaste__TableStorageConnectionString="<connection-string>"
export BeerTaste__FilesFolder="/path/to/data/folder"
```

### Default Folder

If `FilesFolder` is not configured, defaults to `./BeerTastes` relative to the current directory.

## Development

### Code Style

- **Formatting**: Stroustrup brace style, 120 character line width
- **Enforced by**: Fantomas + EditorConfig
- **Naming**: PascalCase for types, camelCase for functions
- **Locale**: Norwegian decimal format (comma separator) supported

### Module Organization

Each `.fs` file is a module with clear single responsibility:
- **Storage.fs** - Azure client setup
- **Beers.fs** - Beer domain and operations
- **Tasters.fs** - Taster domain and operations
- **Scores.fs** - Score domain and validation
- **Results.fs** - Statistical analysis

### Testing

- No formal unit tests (validation through script execution)
- Manual testing with real tasting events
- Report inspection for correctness

## License

Personal non-commercial use. EPPlus library used under non-commercial license.

## Contributing

This is a personal project. Feel free to fork and adapt for your own use.

## Documentation

- **README.md** (this file) - Project overview and getting started guide
- **CLAUDE.md** - Detailed guidance for Claude AI assistant
- **.github/copilot-instructions.md** - GitHub Copilot-specific instructions and code patterns
