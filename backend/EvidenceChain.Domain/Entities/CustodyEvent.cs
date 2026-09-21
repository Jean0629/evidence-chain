using EvidenceChain.Domain.Services;

namespace EvidenceChain.Domain.Entities
{
    public enum CustodyEventType { Created, TransferRequested, TransferAccepted, TransferRejected }

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
        public DateTime OccurredAtUtc { get; set; }
        public string PreviousHash { get; set; } = default!;
        public string Hash { get; set; } = default!;

        public static CustodyEvent Create(Guid evidenceId, CustodyEventType type, Guid actorId, DateTime occurredAtUtc, string previousHash, Guid? relatedTransferId = null)
        {
            var hash = EventHasher.ComputeHash(evidenceId, type.ToString(), actorId, occurredAtUtc, previousHash);
            return new CustodyEvent
            {
                Id = Guid.NewGuid(),
                EvidenceId = evidenceId,
                Type = type,
                ActorId = actorId,
                RelatedTransferId = relatedTransferId,
                OccurredAtUtc = occurredAtUtc,
                PreviousHash = previousHash,
                Hash = hash
            };
        }
    }
}
