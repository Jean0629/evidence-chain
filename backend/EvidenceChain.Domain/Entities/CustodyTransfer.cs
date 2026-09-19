namespace EvidenceChain.Domain.Entities
{
    public enum TransferStatus { Pending, Accepted, Rejected, Expired }

    public class CustodyTransfer
    {
        public Guid Id { get; set; }
        public Guid EvidenceId { get; set; }
        public Guid FromCustodianId {  get; set; }
        public Guid ToCustodianId {  get; set; }
        public Custodian FromCustodian { get; set; } = default!;
        public Custodian ToCustodian { get; set; } = default!;
        public TransferStatus Status {  get; set; } = TransferStatus.Pending;
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ResolvedAtutc { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
