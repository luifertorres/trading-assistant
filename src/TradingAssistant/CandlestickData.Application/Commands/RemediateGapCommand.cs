using CandlestickData.Application.Contracts;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Commands;

public record RemediateGapCommand(
    string Symbol,
    TimeFrame TimeFrame,
    DateTime GapFrom,
    DateTime GapTo) : IRequest<SyncCommandResponse>;
