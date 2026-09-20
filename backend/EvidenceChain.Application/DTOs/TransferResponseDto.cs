namespace EvidenceChain.Application.DTOs
{
    public record TransferResponseDto(Guid Id, Guid EvidenceId, string Status, Guid FromCustodianId, Guid ToCustodianId, DateTime RequestedAtUtc, string ETag);
}
