using CandlestickData.Application.Contracts;
using MediatR;

namespace CandlestickData.Application.Commands;

public record StartOrResumeSyncCommand : IRequest<SyncCommandResponse>;
