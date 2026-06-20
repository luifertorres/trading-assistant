using CandlestickData.Application.Contracts;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Queries;

public record GetCandlesQuery(
    IReadOnlyList<string> Symbols,
    TimeFrame TimeFrame,
    DateTime From,
    DateTime To) : IRequest<CandlesResponse>;
