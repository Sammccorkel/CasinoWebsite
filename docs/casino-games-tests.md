# Casino Games Backend Testing & Automation

## Primary Components

- Dedicated xUnit test project: `Chuds2Chads.Tests`
- Unit tests for the horse racing, roulette, and slots backend services
- GitHub Actions workflow at `.github/workflows/backend-tests.yml`
- Automatic test execution on every push and every pull request

## Organization

The backend game logic is tested at the service layer so that the game rules can be verified without rendering the Blazor UI.

- `HorseRaceServiceTests` checks core race invariants:
  - horse count clamping
  - payout calculation from winning odds
  - frame bounds and monotonic progress
  - race completion within the simulation limit
- `RouletteServiceTests` checks deterministic rule handling:
  - straight bets
  - colour bets
  - odd/even
  - high/low
  - dozens
  - zero handling
  - wheel metadata helpers
- `SlotsServiceTests` checks deterministic payout logic:
  - jackpot detection
  - three-of-a-kind payouts
  - two-cherry special case
  - losing spins

## Running Tests in GitHub Actions

The workflows are stored in `.github/workflows/`. GitHub automatically discovers and runs these on every `push` and `pull_request` to the specified branches (main, develop).

### Workflow Steps (backend-tests.yml):

1. Check out the repository code.
2. Set up .NET 10 SDK.
3. Restore NuGet packages with `dotnet restore Chuds2Chads.sln`.
4. Build the solution in Release mode with `dotnet build Chuds2Chads.sln --configuration Release --no-restore`.
5. Run all unit tests with `dotnet test --solution Chuds2Chads.sln --configuration Release --no-build --report-xunit-trx --results-directory TestResults`.
6. Upload test results as artifacts for inspection.

### Viewing Results:
- Go to the repository on GitHub.
- Navigate to the "Actions" tab.
- Select the workflow run.
- Check the "Run unit tests" step for output.
- Download artifacts if needed for detailed reports.

Ensure .NET 10 SDK is installed and the solution builds without errors before running tests.


