namespace EvidenceChain.Domain.Entities
{
    public enum CustodyEventType { Created, TransferRequested, TransferAccpted, TransferRejected }

    public class CustodyEvent
    {
        public Guid Id { get; set; }
        public Guid EvidenceId { get; set; }
        public Evidence Evidence { get; set; } = default!;
        public CustodyEventType Type { get; set; }
        public Guid ActorId { get; set; }
        public Custodian Actor { get; set; } = default!;
        public Guid? RelatedTransferId { get; set; }
        public CustodyTransfer? RelatedTransfer { get; set; }
        public DateTime OcurredAtUtc { get; set; }
        public string PreviousHash { get; set; } = default!;
        public string Hash { get; set; } = default!;
    }
}
