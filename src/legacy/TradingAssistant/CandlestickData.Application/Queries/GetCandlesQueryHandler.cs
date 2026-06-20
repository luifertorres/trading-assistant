using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Queries;

public sealed class GetCandlesQueryHandler(ICandlestickRepository repository)
    : IRequestHandler<GetCandlesQuery, CandlesResponse>
{
    public async Task<CandlesResponse> Handle(GetCandlesQuery request, CancellationToken cancellationToken)
    {
        var candles = await repository.GetCandlesAsync(
            request.Symbols,
            request.TimeFrame,
            request.From,
            request.To,
            cancellationToken);

        var candleDtos = candles.Select(c => new CandlestickDto(
            c.Symbol,
            c.TimeFrame.ToShortString(),
            c.OpenTime,
            c.CloseTime,
            c.OpenPrice,
            c.HighPrice,
            c.LowPrice,
            c.ClosePrice,
            c.Volume)).ToList();

        var missingRanges = new List<MissingRange>();
        foreach (var symbol in request.Symbols)
        {
            var ranges = await repository.FindMissingRangesAsync(
                symbol, request.TimeFrame, request.From, request.To, cancellationToken);

            missingRanges.AddRange(ranges.Select(r => new MissingRange(r.From, r.To)));
        }

        var fromOpenTime = candles.Count > 0 ? candles[0].OpenTime : (DateTime?)null;
        var toOpenTime = candles.Count > 0 ? candles[^1].OpenTime : (DateTime?)null;

        return new CandlesResponse(
            candleDtos,
            missingRanges.Count == 0,
            fromOpenTime,
            toOpenTime,
            missingRanges);
    }
}
