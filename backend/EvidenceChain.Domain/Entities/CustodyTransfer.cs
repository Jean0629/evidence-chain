using EvidenceChain.Domain.Exceptions;

namespace EvidenceChain.Domain.Entities
{
    public enum TransferStatus { Pending, Accepted, Rejected, Expired }

    public class CustodyTransfer
    {
        public Guid Id { get; set; }
        public Guid EvidenceId { get; set; }
        public Evidence Evidence { get; set; } = default!;
        public Guid FromCustodianId {  get; set; }
        public Guid ToCustodianId {  get; set; }
        public Custodian FromCustodian { get; set; } = default!;
        public Custodian ToCustodian { get; set; } = default!;
        public TransferStatus Status {  get; set; } = TransferStatus.Pending;
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = default!;

        public void Accept()
        {
            if (Status != TransferStatus.Pending)
                throw new InvalidTransitionException(Status.ToString(), "aceptar");

            Status = TransferStatus.Accepted;
            ResolvedAtUtc = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (Status != TransferStatus.Pending)
                throw new InvalidTransitionException(Status.ToString(), "rechazar");

            Status = TransferStatus.Rejected;
            ResolvedAtUtc = DateTime.UtcNow;
        }
    }
}
