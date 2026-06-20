using CandlestickData.Application.Contracts;
using MediatR;

namespace CandlestickData.Application.Commands;

public record RestartSyncCommand : IRequest<SyncCommandResponse>;
