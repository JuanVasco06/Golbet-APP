namespace GolBet.Repositories.Exceptions;

public sealed class DuplicateRecordException(Exception innerException)
    : Exception("Ya existe un registro con estos datos.", innerException);
