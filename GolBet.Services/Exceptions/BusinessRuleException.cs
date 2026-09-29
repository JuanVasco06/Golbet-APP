namespace GolBet.Services.Exceptions;

public sealed class BusinessRuleException(string message) : InvalidOperationException(message);
