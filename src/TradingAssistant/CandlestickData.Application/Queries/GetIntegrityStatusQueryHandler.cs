using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Queries;

public sealed class GetIntegrityStatusQueryHandler(ISymbolIntegrityRepository repository)
    : IRequestHandler<GetIntegrityStatusQuery, IntegrityStatusResponse>
{
    public async Task<IntegrityStatusResponse> Handle(
        GetIntegrityStatusQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await repository.GetAllAsync(cancellationToken);

        var dtos = entries.Select(e => new SymbolIntegrityDto(
            e.Symbol,
            e.TimeFrame.ToShortString(),
            e.Status.ToString(),
            e.Reason.ToString(),
            e.GapFromOpenTime,
            e.GapToOpenTime,
            e.LastVerifiedOpenTime,
            e.DetectedAt)).ToList();

        return new IntegrityStatusResponse(dtos);
    }
}
