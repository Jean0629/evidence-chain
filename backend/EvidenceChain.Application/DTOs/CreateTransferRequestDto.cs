namespace EvidenceChain.Application.DTOs
{
    public record CreateTransferRequestDto(Guid EvidenceId, Guid ToCustodianId);
}
