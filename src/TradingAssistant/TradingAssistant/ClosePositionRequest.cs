using MediatR;

namespace TradingAssistant;

public record ClosePositionRequest(string Symbol) : IRequest<bool>;


