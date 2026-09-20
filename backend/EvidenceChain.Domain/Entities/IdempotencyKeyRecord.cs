namespace EvidenceChain.Domain.Entities
{
    public class IdempotencyKeyRecord
    {
        public string Key { get; set; } = default!;
        public string ResponseBody { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }
    }
}
