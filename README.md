# BeerTaste 🍺

A comprehensive F# data analysis system for organizing and analyzing beer tasting events. BeerTaste processes beer ratings from multiple tasters and generates statistical insights including best-rated beers, most controversial beers, taster similarity analysis, and preference correlations.

## Features

- **Event Management**: Create and manage beer tasting events with Excel-based data entry
- **Statistical Analysis**: Generate comprehensive insights from tasting data
  - Best-rated beers by average score
  - Most controversial beers by standard deviation
  - Taster similarity analysis using Pearson correlations
  - Preference correlations with ABV and price metrics
  - Age-correlated beer preferences (old man beers)
  - Deviant tasters (least similar to group average)
- **Email Notifications**: Send results to tasters automatically via SendGrid when scoring is complete (optional)
- **Internationalization**: Complete English and Norwegian translations with language selector
- **Multiple Interfaces**:
  - Console application for event setup and data management
  - Web application for results presentation with 7 statistical pages + 4 data views
  - F# scripts for custom analysis
- **Azure Integration**: Store event data in Azure Table Storage
- **Excel I/O**: Read and write beer catalogs, taster lists, and scoring matrices

## Quick Start

### Prerequisites

- .NET 10.0 SDK or later
- Azure Storage Account (for persistence)
- SendGrid account (optional - for email notifications)
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

Shared .NET 10.0 class library containing:
- Domain models (Beer, Taster, Score, BeerTaste event)
- Azure Table Storage operations (4 tables: beertaste, beers, tasters, scores)
- Email notifications via SendGrid
- Statistical analysis functions (correlations, averages, standard deviations, age analysis)
- No UI dependencies - pure business logic

### BeerTaste.Console

Console application for event management:
- Excel I/O for beers, tasters, and scores
- Interactive prompts using Spectre.Console
- Event creation and validation workflows
- Delegates data persistence to BeerTaste.Common

### BeerTaste.Web

ASP.NET Core web application with Oxpecker framework:
- **Localization**: Complete English/Norwegian translations with language selector
- **Results presentation**: 7 statistical analysis pages
  - Best Beers (★), Most Controversial (⚡), Most Deviant Tasters (😈)
  - Most Similar Tasters (❤), Strong Beer Preference (😵)
  - Cheap Alcohol Preference (💰), Old Man Beers (👴)
- **Data views**: 4 pages for viewing event data
  - Beers listing, Tasters listing, Scores table, Event details
- Black and white responsive theme with Noto Color Emoji font
- Real-time data fetching from Azure Table Storage
- Language detection from cookies and Accept-Language header
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
| **Common** | Core .NET, Azure.Data.Tables, FSharp.Stats, SendGrid | EPPlus, Spectre.Console, ASP.NET Core |
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
- Computed properties: PricePerLiter, PricePerAbv

### Tasters (Excel "Tasters" worksheet)

- Name (required)
- Email (optional - used for result notifications)
- BirthYear (optional - used for age correlation analysis)

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
6. **Age Correlation**: Correlation between beer ratings and taster age (old man beers)

## Configuration

### User Secrets

Store sensitive configuration using .NET user secrets:

```bash
cd BeerTaste.Console

# Required: Azure Table Storage connection string
dotnet user-secrets set "BeerTaste:TableStorageConnectionString" "<connection-string>"

# Optional: Custom folder path for Excel files (defaults to ./BeerTastes)
dotnet user-secrets set "BeerTaste:FilesFolder" "C:\path\to\data\folder"

# Optional: Results base URL (defaults to https://beertaste.azurewebsites.net)
dotnet user-secrets set "BeerTaste:ResultsBaseUrl" "https://your-app.azurewebsites.net"

# Optional: SendGrid email configuration (for sending results to tasters)
dotnet user-secrets set "BeerTaste:SendGridApiKey" "<your-sendgrid-api-key>"
dotnet user-secrets set "BeerTaste:SendGridFromEmail" "<your-verified-sender@example.com>"
dotnet user-secrets set "BeerTaste:SendGridFromName" "BeerTaste Results"
```

For the web application:

```bash
cd BeerTaste.Web

# Required: Azure Table Storage connection string
dotnet user-secrets set "BeerTaste:TableStorageConnectionString" "<connection-string>"

# Optional: Firebase authentication (for user login)
dotnet user-secrets set "BeerTaste:Firebase:ApiKey" "<your-firebase-api-key>"
dotnet user-secrets set "BeerTaste:Firebase:AuthDomain" "<your-project-id>.firebaseapp.com"
dotnet user-secrets set "BeerTaste:Firebase:ProjectId" "<your-project-id>"
```

### Environment Variables

Alternatively, use environment variables:

```bash
# PowerShell
$env:BeerTaste__TableStorageConnectionString = "<connection-string>"
$env:BeerTaste__FilesFolder = "C:\path\to\data\folder"
$env:BeerTaste__ResultsBaseUrl = "https://your-app.azurewebsites.net"
$env:BeerTaste__SendGridApiKey = "<your-sendgrid-api-key>"
$env:BeerTaste__SendGridFromEmail = "<your-verified-sender@example.com>"
$env:BeerTaste__SendGridFromName = "BeerTaste Results"
$env:BeerTaste__Firebase__ApiKey = "<your-firebase-api-key>"
$env:BeerTaste__Firebase__AuthDomain = "<your-project-id>.firebaseapp.com"
$env:BeerTaste__Firebase__ProjectId = "<your-project-id>"

# Bash
export BeerTaste__TableStorageConnectionString="<connection-string>"
export BeerTaste__FilesFolder="/path/to/data/folder"
export BeerTaste__ResultsBaseUrl="https://your-app.azurewebsites.net"
export BeerTaste__SendGridApiKey="<your-sendgrid-api-key>"
export BeerTaste__SendGridFromEmail="<your-verified-sender@example.com>"
export BeerTaste__SendGridFromName="BeerTaste Results"
export BeerTaste__Firebase__ApiKey="<your-firebase-api-key>"
export BeerTaste__Firebase__AuthDomain="<your-project-id>.firebaseapp.com"
export BeerTaste__Firebase__ProjectId="<your-project-id>"
```

### Default Folder

If `FilesFolder` is not configured, defaults to `./BeerTastes` relative to the current directory.

### Email Configuration (Optional)

The email feature allows you to send results notifications to all tasters when scoring is complete. Configuration is optional and the application works without it.

**Email Service: SendGrid**

BeerTaste uses SendGrid for reliable email delivery. SendGrid offers:
- Free tier: 100 emails/day (plenty for most tasting events)
- Simple API with just an API key (no SMTP configuration needed)
- High deliverability rates

**SendGrid Setup:**
1. Sign up for a free account at [SendGrid](https://sendgrid.com/)
2. Verify your sender email address (or domain)
3. Create an API key with "Mail Send" permissions
4. Configure the API key and sender information in user secrets or environment variables

**Security Note:**
The application includes admin filtering - in production, only emails configured as admin addresses will receive notifications. This prevents accidental email sends to all tasters during testing.

**Usage:**
When scoring is complete, the console application will:
1. Prompt: "Do you want to send results emails to all tasters?"
2. If you select "Yes", emails are sent to all tasters with valid email addresses
3. Each email contains a personalized link to view the results
4. Email addresses are masked in log output for privacy

If email configuration is missing or incomplete, the feature is gracefully disabled and the application continues normally.

### Firebase Authentication (Optional)

The web application supports Firebase Authentication for user login. When configured, users can sign in with their Google account.

**Firebase Setup:**

1. Go to the [Firebase Console](https://console.firebase.google.com/)
2. Create a new project (or use an existing one)
3. In the Firebase Console, go to **Project Settings** → **General**
4. Scroll down to **Your apps** and click **Add app** → **Web** (</> icon)
5. Register your app and copy the configuration values:
   - `apiKey`
   - `authDomain` (typically `your-project-id.firebaseapp.com`)
   - `projectId`
6. Enable Google Sign-In:
   - Go to **Authentication** → **Sign-in method**
   - Click on **Google** and enable it
   - Add your authorized domains (localhost for development, your production domain)

**Local Development Configuration (User Secrets):**

```bash
cd BeerTaste.Web

# Firebase configuration
dotnet user-secrets set "BeerTaste:Firebase:ApiKey" "<your-firebase-api-key>"
dotnet user-secrets set "BeerTaste:Firebase:AuthDomain" "<your-project-id>.firebaseapp.com"
dotnet user-secrets set "BeerTaste:Firebase:ProjectId" "<your-project-id>"
```

**Production Configuration (Environment Variables):**

```bash
# PowerShell
$env:BeerTaste__Firebase__ApiKey = "<your-firebase-api-key>"
$env:BeerTaste__Firebase__AuthDomain = "<your-project-id>.firebaseapp.com"
$env:BeerTaste__Firebase__ProjectId = "<your-project-id>"

# Bash
export BeerTaste__Firebase__ApiKey="<your-firebase-api-key>"
export BeerTaste__Firebase__AuthDomain="<your-project-id>.firebaseapp.com"
export BeerTaste__Firebase__ProjectId="<your-project-id>"
```

**How it Works:**
- When Firebase is configured, a "Login" link appears in the navigation bar
- Clicking "Login" opens a Google sign-in popup
- After successful login, the user's name is displayed instead of the login link
- Users can click "Logout" to sign out

**Security Notes:**
- Firebase API keys are safe to expose in client-side code (they identify your project, not authenticate)
- Actual security is enforced by Firebase Security Rules and authorized domains
- The authentication is client-side only; future features can use the Firebase ID token for server-side validation

If Firebase configuration is missing or incomplete, the login feature is gracefully hidden and the application continues normally.

## Development

### Code Style

- **Formatting**: Stroustrup brace style, 120 character line width
- **Enforced by**: Fantomas + EditorConfig
- **Naming**: PascalCase for types, camelCase for functions
- **Locale**: Norwegian decimal format (comma separator) supported

### Module Organization

Each `.fs` file is a module with clear single responsibility:

**BeerTaste.Common:**
- **Storage.fs** - Azure Table Storage client setup (4 tables)
- **Beers.fs** - Beer domain types and Azure operations
- **Tasters.fs** - Taster domain types and Azure operations
- **BeerTaste.fs** - BeerTaste event types and Azure operations
- **Scores.fs** - Score domain, validation, and Azure operations
- **Email.fs** - SendGrid email notifications with admin filtering
- **Results.fs** - Statistical analysis (correlations, rankings, age analysis)

**BeerTaste.Console:**
- **Configuration.fs** - Config loading, folder setup, email configuration
- **Beers.fs** - Beer Excel I/O and TastersSchema creation
- **Tasters.fs** - Taster Excel I/O
- **Scores.fs** - ScoreSchema management and score reading
- **Workflow.fs** - Orchestration, prompts, browser opening, email sending
- **Program.fs** - Application entry point

**BeerTaste.Web:**
- **Localization.fs** - English/Norwegian translations
- **templates/*.fs** - HTML views for all pages
- **Program.fs** - Web server with routing

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
