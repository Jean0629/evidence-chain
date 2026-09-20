namespace EvidenceChain.Application.DTOs
{
    public record CustodyEventDto(Guid Id, string Type, string ActorName, DateTime OccurredAtUtc, string Hash, string PreviousHash);
}
