namespace EvidenceChain.Application.DTOs
{
    public record EvidenceDetailDto(Guid Id, string Code, string Description, string CurrentCustodianName, DateTime CreatedAtUtc);
}
