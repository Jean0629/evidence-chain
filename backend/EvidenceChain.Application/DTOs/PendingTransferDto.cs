using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Application.DTOs
{
    public record MyPendingTransferDto(Guid Id, Guid EvidenceId, string EvidenceCode, string FromCustodianName, DateTime RequestedAtUtc, string ETag, bool IsIntegrityValid);
}
