namespace EvidenceChain.Domain.Entities
{
    public enum CustodianRole { Investigador, Custodio, Supervisor }

    public class Custodian
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = default!;
        public CustodianRole Role {  get; set; }
    }
}
