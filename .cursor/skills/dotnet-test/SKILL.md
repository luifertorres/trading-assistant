# Skill: .NET Testing with xUnit

## Description

Creates and runs xUnit tests for the trading-assistant project, following established conventions.

## When to Use

Use this skill when the user asks to:
- Add tests for a class or feature
- Create a test project
- Run existing tests
- Verify a change with tests

## Instructions

### 1. Check if Test Project Exists

Look for `*.Tests.csproj` files in `src/TradingAssistant/`. If none exist, create the test project first.

### 2. Create Test Project (if needed)

```bash
cd src/TradingAssistant
dotnet new xunit -n TradingAssistant.Domain.Tests
dotnet sln add TradingAssistant.Domain.Tests
cd TradingAssistant.Domain.Tests
dotnet add reference ../TradingAssistant.Domain/TradingAssistant.Domain.csproj
dotnet add package NSubstitute
dotnet add package FluentAssertions
```

Repeat for other layers as needed:
- `TradingAssistant.Application.Tests` — references Application + Domain
- `TradingAssistant.Infrastructure.Tests` — references Infrastructure + Application + Domain
- `TradingAssistant.Host.Tests` — references Host + all layers (integration tests)

### 3. Test Project Structure

Mirror the production project structure:

```
TradingAssistant.Domain.Tests/
├── CandleTests.cs
├── StopLossPriceTests.cs
├── TakeProfitPriceTests.cs
├── CircularTimeSeriesTests.cs
└── SteppedTrailingStopTests.cs
```

### 4. Test Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
```csharp
public class StopLossPriceTests
{
    [Fact]
    public void Calculate_WithValidPriceAndPercentage_ReturnsCorrectStopLoss()
    {
        // Arrange
        // Act
        // Assert
    }

    [Theory]
    [InlineData(100, 5, 95)]
    [InlineData(200, 10, 180)]
    public void Calculate_WithVariousInputs_ReturnsExpectedValues(
        decimal price, decimal percentage, decimal expected)
    {
        // ...
    }
}
```

### 5. Testing Patterns by Layer

**Domain tests (pure, no mocks):**
```csharp
// Value objects — test equality, immutability, validation
// Entities — test behavior methods, invariant enforcement
// Extensions — test input/output for all edge cases
```

**Application tests (mock infrastructure):**
```csharp
// Use NSubstitute for IExchangeService, ICandleRepository, etc.
var exchangeService = Substitute.For<IExchangeService>();
exchangeService.GetPositionsAsync(Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(positions));
```

**Infrastructure tests (integration):**
```csharp
// Use in-memory SQLite for EF Core tests
// Use real FASTER instances with temp directories
// Mock external HTTP calls for Binance adapter tests
```

### 6. Run Tests

```bash
cd src/TradingAssistant
dotnet test                              # Run all tests
dotnet test --filter "FullyQualifiedName~Domain"  # Run domain tests only
dotnet test --verbosity normal           # Detailed output
```

### 7. Assertions with FluentAssertions

```csharp
result.Should().Be(expected);
result.Should().BeGreaterThan(0);
action.Should().Throw<ArgumentException>().WithMessage("*invalid*");
positions.Should().HaveCount(3).And.OnlyContain(p => p.IsOpen);
```

### Rules

- Domain tests must be **pure** — no mocks, no I/O, no external dependencies.
- Every public method on a domain entity or VO should have at least one test.
- Test edge cases: null inputs, empty collections, boundary values, overflow.
- Use `[Theory]` with `[InlineData]` for parameterized tests instead of duplicating test methods.
- Keep tests fast — avoid `Thread.Sleep` or real network calls.
