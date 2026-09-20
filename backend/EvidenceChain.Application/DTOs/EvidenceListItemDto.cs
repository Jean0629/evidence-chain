using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EvidenceChain.Application.DTOs
{
    public record EvidenceListItemDto(Guid Id, string Code, string Description, string CurrentCustodianName, DateTime LastEventAtUtc, bool HasAnomaly);
}
