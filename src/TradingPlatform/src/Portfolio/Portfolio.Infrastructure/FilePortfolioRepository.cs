using System.Text.Json;
using Portfolio.Domain;
using TradingPlatform.Kernel;

namespace Portfolio.Infrastructure;

public sealed class FilePortfolioRepository(string directory)
{
    public async Task SaveAsync(PortfolioDefinition portfolio, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{portfolio.PortfolioId:N}.json");
        var dto = PortfolioDto.From(portfolio);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PortfolioDefinition?> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directory))
            return null;
        var file = Directory
            .EnumerateFiles(directory, "*.json")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();
        if (file is null)
            return null;
        var json = await File.ReadAllTextAsync(file.FullName, cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<PortfolioDto>(json, JsonOptions);
        return dto?.ToDomain();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed record PortfolioDto(Guid PortfolioId, string Name, List<MemberDto> Members, DateTimeOffset AsOf)
    {
        public static PortfolioDto From(PortfolioDefinition p) =>
            new(
                p.PortfolioId,
                p.Name,
                p.Members.Select(m => new MemberDto(m.VectorId.Value, m.Weight)).ToList(),
                p.AsOf);

        public PortfolioDefinition ToDomain() =>
            new(
                PortfolioId,
                Name,
                Members.Select(m => new PortfolioMember(TradingVectorId.From(m.VectorId), m.Weight)).ToList(),
                AsOf);
    }

    private sealed record MemberDto(Guid VectorId, decimal Weight);
}
