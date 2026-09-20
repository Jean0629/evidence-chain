namespace EvidenceChain.Application.DTOs
{
    public record PendingTransferDto(Guid Id, Guid ToCustodianId, string ToCustodianName, DateTime RequestedAtUtc, string ETag);

    public record EvidenceDetailDto(Guid Id, string Code, string Description, Guid CurrentCustodianId, string CurrentCustodianName, DateTime CreatedAtUtc, bool HasAnomaly, string? AnomalySeverity, string? AnomalyReason, PendingTransferDto? PendingTransfer);
}
