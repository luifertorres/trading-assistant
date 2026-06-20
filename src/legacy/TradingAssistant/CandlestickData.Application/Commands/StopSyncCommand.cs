using CandlestickData.Application.Contracts;
using MediatR;

namespace CandlestickData.Application.Commands;

public record StopSyncCommand : IRequest<SyncCommandResponse>;
