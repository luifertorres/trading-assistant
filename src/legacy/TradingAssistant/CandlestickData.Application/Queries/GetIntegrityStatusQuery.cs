using CandlestickData.Application.Contracts;
using MediatR;

namespace CandlestickData.Application.Queries;

public record GetIntegrityStatusQuery : IRequest<IntegrityStatusResponse>;
