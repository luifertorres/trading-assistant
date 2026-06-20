using CandlestickData.Application.Contracts;
using MediatR;

namespace CandlestickData.Application.Queries;

public record GetSyncStatusQuery : IRequest<SyncStatusResponse>;
