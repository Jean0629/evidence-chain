namespace EvidenceChain.Domain.Exceptions
{
    public class ConcurrencyConflictException(string currentStatus) : Exception("La transferencia fue modificada por otro usuario.")
    {
        public string CurrentStatus { get; } = currentStatus;
    }
}
