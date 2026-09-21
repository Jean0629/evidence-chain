using EvidenceChain.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Application.Custodians
{
    public interface ICustodianQueries
    {
        Task<List<CustodianSummaryDto>> GetAllAsync();
    }
}
