using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Domain.Entities
{
    public class Evidence
    {
        public Guid Id { get; set; }
        public String Code { get; set; } = default!;
        public String Description { get; set; } = default!;
        public Guid CurrentCustodianId { get; set; }
        public Custodian CurrentCustodian { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }

        public List<CustodyEvent> Events { get; set; } = new();
    }
}
