namespace Portfolio;

public static class DependentWalks
{
    public const int MarketSeed = 2023;
    public const double SharedFactorCorrelation = 0.6;
    public const double CrashProbability = 0.01;
    public const double CrashLoss = 0.025;
    public const int MeanRegimeDays = 120;
    public const double RegimeDrift = 0.001;

    public static IReadOnlyDictionary<int, IReadOnlyList<UsdtPoint>> Generate(ReturnScenario scenario)
    {
        if (scenario is ReturnScenario.NegativeGrowth or ReturnScenario.PositiveBias)
        {
            var drift = ReturnScenarioSeries.Drift(scenario);
            var result = new Dictionary<int, IReadOnlyList<UsdtPoint>>();
            foreach (var seed in ReturnScenarioSeries.Seeds(scenario))
                result[seed] = UtcMinusFiveUsdtSeries.Generate(seed, drift);

            return result;
        }

        return scenario switch
        {
            ReturnScenario.SharedFactor => GenerateSharedFactor(),
            ReturnScenario.CrashCoupled => GenerateCrashCoupled(),
            ReturnScenario.ComplementaryRegimes => GenerateComplementaryRegimes(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }

    private static Dictionary<int, IReadOnlyList<UsdtPoint>> GenerateSharedFactor()
    {
        var walks = InitializeWalks(ReturnScenarioSeries.Seeds(ReturnScenario.SharedFactor));
        var market = new Random(MarketSeed);
        var mu = UtcMinusFiveUsdtSeries.DailyDrift;
        var sqrtRho = Math.Sqrt(SharedFactorCorrelation);
        var sqrtOneMinusRho = Math.Sqrt(1 - SharedFactorCorrelation);

        AdvanceDays(walks, market, (seeds, state, marketRandom) =>
        {
            var marketFactor = (marketRandom.NextDouble() * 2.0) - 1.0;
            foreach (var seed in seeds)
            {
                var walk = state[seed];
                var idiosyncratic = (walk.Random.NextDouble() * 2.0) - 1.0;
                var combined = sqrtRho * marketFactor + sqrtOneMinusRho * idiosyncratic;
                ApplyRate(walk, mu + UtcMinusFiveUsdtSeries.DailyChangeRate * combined);
            }
        });

        return ToResult(walks);
    }

    private static Dictionary<int, IReadOnlyList<UsdtPoint>> GenerateCrashCoupled()
    {
        var walks = InitializeWalks(ReturnScenarioSeries.Seeds(ReturnScenario.CrashCoupled));
        var market = new Random(MarketSeed);
        var mu = UtcMinusFiveUsdtSeries.DailyDrift;
        var muPrime = (mu + CrashProbability * CrashLoss) / (1 - CrashProbability);

        AdvanceDays(walks, market, (seeds, state, marketRandom) =>
        {
            var crash = marketRandom.NextDouble() < CrashProbability;
            foreach (var seed in seeds)
            {
                var walk = state[seed];
                if (crash)
                {
                    ApplyRate(walk, -CrashLoss);
                    continue;
                }

                var idiosyncratic = (walk.Random.NextDouble() * 2.0) - 1.0;
                ApplyRate(walk, muPrime + UtcMinusFiveUsdtSeries.DailyChangeRate * idiosyncratic);
            }
        });

        return ToResult(walks);
    }

    private static Dictionary<int, IReadOnlyList<UsdtPoint>> GenerateComplementaryRegimes()
    {
        var walks = InitializeWalks(ReturnScenarioSeries.Seeds(ReturnScenario.ComplementaryRegimes));
        var market = new Random(MarketSeed);
        var mu = UtcMinusFiveUsdtSeries.DailyDrift;
        var flipProbability = 1.0 / MeanRegimeDays;
        var trendRegime = true;

        AdvanceDays(walks, market, (seeds, state, marketRandom) =>
        {
            if (marketRandom.NextDouble() < flipProbability)
                trendRegime = !trendRegime;

            foreach (var seed in seeds)
            {
                var walk = state[seed];
                var idiosyncratic = (walk.Random.NextDouble() * 2.0) - 1.0;
                var regimeTerm = seed switch
                {
                    UtcMinusFiveUsdtSeries.DefaultSeed => trendRegime ? RegimeDrift : -RegimeDrift,
                    UtcMinusFiveUsdtSeries.SecondSeed => trendRegime ? -RegimeDrift : RegimeDrift,
                    _ => 0.0
                };

                ApplyRate(walk, mu + regimeTerm + UtcMinusFiveUsdtSeries.DailyChangeRate * idiosyncratic);
            }
        });

        return ToResult(walks);
    }

    private sealed class WalkState
    {
        public required Random Random { get; init; }
        public required List<UsdtPoint> Points { get; init; }
        public int Usdt { get; set; }
    }

    private static Dictionary<int, WalkState> InitializeWalks(IReadOnlyList<int> seeds)
    {
        var walks = new Dictionary<int, WalkState>();
        foreach (var seed in seeds)
        {
            var random = new Random(seed);
            walks[seed] = new WalkState
            {
                Random = random,
                Points = new List<UsdtPoint>(UtcMinusFiveUsdtSeries.InclusiveDayCount),
                Usdt = random.Next(UtcMinusFiveUsdtSeries.MinUsdt, UtcMinusFiveUsdtSeries.MaxUsdt + 1)
            };
        }

        return walks;
    }

    private static void AdvanceDays(
        Dictionary<int, WalkState> walks,
        Random market,
        Action<IReadOnlyList<int>, Dictionary<int, WalkState>, Random> applyRates)
    {
        var seeds = walks.Keys.ToList();
        var time = UtcMinusFiveUsdtSeries.Start;

        while (time <= UtcMinusFiveUsdtSeries.End)
        {
            foreach (var seed in seeds)
            {
                var walk = walks[seed];
                walk.Points.Add(new UsdtPoint(time, walk.Usdt));
            }

            if (time >= UtcMinusFiveUsdtSeries.End)
                break;

            applyRates(seeds, walks, market);
            time = time.AddDays(1);
        }
    }

    private static void ApplyRate(WalkState walk, double rate) =>
        walk.Usdt = (int)Math.Round(walk.Usdt + walk.Usdt * rate);

    private static Dictionary<int, IReadOnlyList<UsdtPoint>> ToResult(Dictionary<int, WalkState> walks) =>
        walks.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<UsdtPoint>)kv.Value.Points);
}
