# Chuds2Chads Casino
Software Engineering II Group Project

## Team Membership
- Lucas Litzenberger
- Caleb Blevins
- Samuel McCorkel
- Matthew Williams
- Reo Day

## Repository Navigation
- Main application: [Chuds2Chads](./Chuds2Chads)
- Automated tests: [Chuds2Chads.Tests](./Chuds2Chads.Tests)
- Sprint reports: [SprintReports](./SprintReports)
- Supporting test and multiplayer docs: [docs](./docs)
- GitHub Actions workflows: [.github/workflows](./.github/workflows)

## Product Vision
Chuds2Chads Casino is a web-based casino platform designed to provide users with an engaging multiplayer gaming experience using virtual currency. The platform allows users to create accounts, log in securely, participate in casino games, earn and spend in-game currency, and customize their avatars through unlockable cosmetic items. The long-term vision is to create an expandable system that supports multiple casino games, persistent player progression, social and multiplayer interaction, and a clean architecture that can continue growing over time.

## Project Goals
The major goals of this project are:
- Build a functional casino-style web application using Blazor Server.
- Support secure account creation and login with ASP.NET Core Identity.
- Store persistent player data using SQLite and Entity Framework Core.
- Allow players to participate in casino games using virtual currency.
- Support multiplayer-ready game room architecture and friend-based access.
- Track player balances and transactions over time.
- Support progression systems such as avatar customization, cosmetics, and leaderboards.
- Follow Scrum methodology across multiple sprints.
- Maintain organized backlog management, testing, version control, and documentation throughout development.

## Release Plan
### Release 1 - Foundation
- Project shell and framework setup
- Core Blazor application structure
- Repository and architectural setup

### Release 2 - Accounts and Persistence
- User authentication and login system
- Database integration with ASP.NET Core Identity
- SQLite configuration and migrations

### Release 3 - Wallet and Dashboard
- Wallet and virtual currency tracking
- Core database tables for users, transactions, rooms, and sessions
- Dashboard and account support

### Release 4 - Gameplay and Multiplayer Expansion
- Roulette, Slots, and Horse Race gameplay
- Blackjack and Poker gameplay support
- Multiplayer-ready room and session support
- Friends system and live-table behavior
- Leaderboard integration
- Avatar customization and cosmetic progression
- Automated tests and GitHub Actions support


## Trello / Planner Board
Sprint reports from Sprint 4 through Sprint 10 are structured to support the required Trello / Planner export evidence. Where board exports are not currently embedded in the repo, the sprint reports clearly state that the original sprint notes did not contain the export file and that it should be attached separately when available.

## Source Code
All project source code is maintained in this repository.

### Main application areas
- UI components and pages: [Chuds2Chads/Components](./Chuds2Chads/Components)
- API/controller logic: [Chuds2Chads/Controllers](./Chuds2Chads/Controllers)
- Data context, user model, and entities: [Chuds2Chads/Data](./Chuds2Chads/Data)
- Gameplay logic: [Chuds2Chads/Games](./Chuds2Chads/Games)
- Application and domain services: [Chuds2Chads/Services](./Chuds2Chads/Services)
- Database migrations: [Chuds2Chads/Migrations](./Chuds2Chads/Migrations)
- Static assets and game art: [Chuds2Chads/wwwroot](./Chuds2Chads/wwwroot)

### Key implemented systems
- Secure account registration, login, logout, and current-user access
- Wallet-backed chip balance tracking and transaction logging
- Roulette, Slots, Horse Race, Blackjack, and Poker support
- Multiplayer rooms, joinable live tables, and friend-code system
- Leaderboard ranking based on wallet balance
- Player statistics dashboard support
- Avatar customization and cosmetics support

## Coding Standards
The team followed these coding standards:
- Use consistent C# naming conventions.
- Use PascalCase for classes, methods, and public properties.
- Keep services, data models, game logic, and UI separated by concern.
- Use consistent file and folder naming.
- Keep entity models in logical data/entity folders.
- Favor readable formatting and avoid unnecessary duplicate logic.
- Add tests when changing shared gameplay or settlement logic.

## Documentation Standards
The team maintained documentation by:
- Keeping the repository README as the central project guide.
- Tracking project activity through sprint reports.
- Keeping testing and automation notes in the [docs](./docs) folder.
- Organizing source, tests, reports, and workflows in clearly separated folders.
- Documenting setup requirements and runtime expectations for teammates.

## Development Environment (Tech Stack)
The project is built with:
- .NET 10
- ASP.NET Core
- Blazor Server with interactive server components
- ASP.NET Core Identity
- Entity Framework Core 10
- SQLite
- xUnit-based automated testing
- Git and GitHub
- GitHub Actions
- Visual Studio / VS Code

## Deployment Environment
The project currently runs in a local development environment.

### Current deployment context
- Run locally with `dotnet run`
- SQLite file-based database
- Browser-based web interface on localhost

## Version Management
Version management is handled using Git and GitHub.

### Current process
- Source code is stored in a shared GitHub repository.
- Team members work on branches, merge through coordinated updates, and integrate changes into the shared codebase.
- Commits track progress and support change review.
- GitHub Actions workflows provide automated validation at the repository level.
- Generated files, local database files, build artifacts, and scratch projects should not remain tracked in the repository.

## Test Plan, Tests Performed, and Analysis Reports
Testing focuses on validating:
- account and authentication behavior,
- wallet-backed chip settlement,
- core game result logic,
- multiplayer and leaderboard behavior,
- integration of gameplay with persistence,
- and performance smoke checks for key operations.

### Tests performed
The repository includes tests for:
- wallet services,
- roulette logic,
- slots logic,
- horse race logic,
- blackjack game logic,
- poker hand evaluation,
- blackjack settlement,
- poker settlement,
- leaderboard behavior,
- multiplayer and friends behavior,
- wallet-integrated game flows,
- and performance metrics.

### Test types
- Unit testing
- Integration testing
- Service testing
- Functional validation
- Performance / metrics smoke testing
- CI-based automated execution

### Supporting test documents
- [Casino Games Backend Testing](./docs/casino-games-tests.md)
- [Multiplayer and Leaderboard Tests](./docs/multiplayer-leaderboard-tests.md)
- [Multiplayer Testing Guide](./docs/multiplayer-testing.md)

## Test Automation and Automated Test Execution Configuration
The project includes both local and CI-based automated testing.

### Local automation
- `dotnet restore`
- `dotnet build`
- `dotnet test`

### GitHub Actions automation
- [backend-tests.yml](./.github/workflows/backend-tests.yml)
- [tests.yml](./.github/workflows/tests.yml)

### Current automation coverage
- Build verification
- Automated test discovery
- Automated test execution
- Artifact upload for test results
- Validation on push, pull request, and manual workflow dispatch where configured

## Change Management and Bug Tracking Process
Changes and bug fixes are managed through:
- GitHub repository history
- Team branch and merge workflow
- Sprint backlog items and sprint reports
- Trello / Planner backlog tracking
- Re-testing after fixes

### Process
1. Identify a bug, requirement, or feature need.
2. Add or refine the related backlog item.
3. Prioritize it for a sprint based on dependency, risk, and project goals.
4. Assign ownership.
5. Implement and validate the change.
6. Re-test and integrate into the shared codebase.

## Definition of Ready (Revised)
A backlog item is considered ready when:
- it is clearly described,
- it has a defined user or system outcome,
- dependencies are identified,
- ownership is assigned,
- acceptance criteria or validation expectations are known,
- and the work is small enough to fit within a sprint.

## Definition of Done (Revised)
A backlog item is considered done when:
- the code is implemented,
- the application builds successfully,
- the feature works as intended,
- the change does not break existing functionality,
- required tests or validation have been completed,
- documentation is updated if behavior changed,
- and the change is integrated into the shared repository.

## Architectural Design
The project uses a layered architecture organized around UI, services, data, and tests.

### Front end
- Blazor Server components and pages

### Authentication layer
- ASP.NET Core Identity
- Secure account registration and login flow
- Role-ready identity model with user persistence

### Data layer
- EF Core DbContext
- SQLite persistence
- Entity models for users, wallets, transactions, rooms, sessions, friendships, invites, and cosmetics

### Service layer
- Wallet service
- Multiplayer service
- Leaderboard service
- Avatar service
- Player statistics service
- Settlement services for Blackjack and Poker
- Dedicated game services for Roulette, Slots, and Horse Race

### Test layer
- Unit and integration tests grouped by services, games, and metrics

## Detailed Design
Detailed design includes the following major models and services:

### Key models
- `ApplicationUser` for Identity-based user management
- `Wallet` for persistent chip balances
- `Transaction` for chip history and auditing
- `GameRoom` for multiplayer lobbies
- `RoomPlayer` for room membership and state
- `GameSession` for session outcomes and tracking
- `FriendRequest` and `Friendship` for social features
- Cosmetic and avatar entities for customization

### Key UI pages and components
- Landing page
- Login and register pages
- Dashboard
- Player statistics page
- Blackjack and Poker lobby/game pages
- Roulette, Slots, and Horse Race pages
- Profile, shop, customize, and admin pages

## Database Design
The database currently supports:
- AspNetUsers
- AspNetRoles
- Wallets
- Transactions
- GameRooms
- RoomPlayers
- GameSessions
- FriendRequests
- Friendships
- GameRoomInvites
- CosmeticDefinitions
- UserCosmeticItems
- UserAvatarLoadouts

### Database design goals
- Persistent user account storage
- Secure login integration
- Currency tracking over time
- Multiplayer-ready room and session support
- Social features such as friends and invites
- Extensibility for cosmetics, avatar systems, and future feature growth

## UI/UX Design
UI/UX goals include:
- A themed casino-style interface
- Intuitive navigation from landing page to games and dashboard
- Clear login and account-management flow
- Expandable game-specific layouts
- Live-dashboard support for friends, leaderboard, and room visibility
- Support for player progression through chips, stats, and customization

## DevOps First Way, Second Way, and Third Way
The project demonstrates the three DevOps Ways in concrete ways:

### DevOps First Way
The First Way focuses on optimizing flow from development to operations so software can be delivered quickly and efficiently.
This project supports the First Way by:
- maintaining a single integrated codebase,
- separating UI, services, data, and tests for cleaner flow,
- using migrations and startup initialization to reduce setup friction,
- and organizing the repository so multiple teammates can work on different layers with less conflict.

### DevOps Second Way
The Second Way emphasizes fast and continuous feedback loops.
This project supports the Second Way by:
- using automated tests for services, gameplay logic, multiplayer, and leaderboard functionality,
- running GitHub Actions workflows to restore, build, and test automatically,
- and using dashboard, multiplayer, and statistics systems to surface live system state and user feedback quickly.

### DevOps Third Way
The Third Way promotes continuous learning, experimentation, and improvement.
This project supports the Third Way by:
- iteratively expanding test coverage,
- documenting sprint work and lessons learned across reports,
- improving architecture as integration issues were discovered,
- and using repeated refinement of multiplayer, wallet, leaderboard, and gameplay systems to learn from both failures and successes.

## Security Features
The project includes the following security-oriented features:
- ASP.NET Core Identity for user authentication and account management
- Password policy enforcement in application startup configuration
- Authentication and authorization middleware in the app pipeline
- Antiforgery support in the request pipeline
- Unique database constraints for friend codes, room codes, wallets, friendships, and invites
- Wallet-backed transaction logging for auditable chip changes
- Controlled registration flow with duplicate username, email, and friend-code protection
- Automatic database migration and setup checks during startup to reduce environment drift

## Open Issues and Current Concerns
Current known limitations or future opportunities include:
- Deployment is currently local rather than cloud-hosted.
- UI polish can still improve in some areas.
- Additional future expansion is possible for progression systems, additional game depth, and deployment hardening.

## Setup
### Install .NET 10 SDK
Windows:

```powershell
winget install Microsoft.DotNet.SDK.10 --source winget
dotnet --list-sdks
```

### Install EF Core tooling
```powershell
dotnet tool install --global dotnet-ef
dotnet ef --version
```

### Clone and run
```powershell
git clone <YOUR_REPO_URL_HERE>
cd <YOUR_REPO_FOLDER>
cd Chuds2Chads
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

Open the local URL shown in the terminal, for example:

`http://localhost:5138`

## Database Configuration
The project uses SQLite by default.

Example connection string in [Chuds2Chads/appsettings.json](./Chuds2Chads/appsettings.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=chuds2chads.db"
  }
}
```
