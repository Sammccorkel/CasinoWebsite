# Multiplayer, Friends, and Leaderboard Testing & Automation

## Primary Components

- Dedicated xUnit test project: `Chuds2Chads.Tests`
- Service-level multiplayer, friends, wallet, and leaderboard tests
- GitHub Actions workflow at `.github/workflows/backend-tests.yml`
- Manual backup workflow at `.github/workflows/tests.yml`
- Automatic test execution on every push and every pull request to `main` and `develop`

## Organization

The multiplayer and leaderboard systems are tested at the service layer so that room visibility, friend relationships, wallet-backed ranking, and ranking updates can be verified without relying on Blazor page rendering.

- `LeaderboardServiceTests` checks leaderboard behavior:
  - players are ranked by wallet balance in descending order
  - rank numbers are reassigned when balances change
  - users without wallets are still included with `0` chips
- `MultiplayerServiceTests` checks multiplayer and friend workflows:
  - friend requests can be sent and accepted
  - accepted friendships appear on dashboard data for both users
  - friend-visible live tables are visible to friends and hidden from non-friends
  - host-created tables can be deleted and properly closed
- Existing backend service tests still validate:
  - horse racing rules
  - roulette rules
  - slots payouts
  - wallet transaction and balance behavior

## Test Infrastructure

The multiplayer and leaderboard tests use an in-memory SQLite harness so the tests execute against the real EF Core model and the real application services.

- `Chuds2Chads.Tests/TestInfrastructure/SqliteTestHarness.cs`
  - creates a shared in-memory SQLite database
  - registers `AppDbContext`, `WalletService`, `LeaderboardService`, and `MultiplayerService`
  - seeds test users and wallets for realistic ranking and room scenarios

## Running Tests in GitHub Actions

The workflows are stored in `.github/workflows/`.

- `backend-tests.yml`
  - runs automatically on every `push` and `pull_request` to `main` and `develop`
  - can also be run manually with `workflow_dispatch`
- `tests.yml`
  - remains available as a manual backup test run

### Workflow Steps (backend-tests.yml)

1. Check out the repository code.
2. Set up .NET 10 SDK.
3. Restore NuGet packages with `dotnet restore Chuds2Chads.sln`.
4. Build the solution in Release mode with `dotnet build Chuds2Chads.sln --configuration Release --no-restore`.
5. Run the test project with `dotnet test --project Chuds2Chads.Tests/Chuds2Chads.Tests.csproj --configuration Release --no-build --report-xunit-trx --results-directory TestResults`.
6. Upload test results as artifacts for inspection.

### Viewing Results

- Go to the repository on GitHub.
- Navigate to the `Actions` tab.
- Select the workflow run.
- Review the build and test steps.
- Download the `test-results` artifact if detailed results are needed.

## Running Tests Locally

From the repository root:

```powershell
dotnet restore Chuds2Chads.sln
dotnet build Chuds2Chads.Tests/Chuds2Chads.Tests.csproj --configuration Release
dotnet test --project Chuds2Chads.Tests/Chuds2Chads.Tests.csproj --configuration Release --no-build
```

If the web app is currently running locally, stop it before a Debug build because the Blazor static web assets cache can keep a build artifact locked while `dotnet run` is active.
