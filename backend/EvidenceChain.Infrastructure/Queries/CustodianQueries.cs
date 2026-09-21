using EvidenceChain.Application.Custodians;
using EvidenceChain.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Infrastructure.Queries
{
    public class CustodianQueries(EvidenceChainDbContext db) : ICustodianQueries
    {
        public async Task<List<CustodianSummaryDto>> GetAllAsync() =>
            await db.Custodians
                .Select(x => new CustodianSummaryDto(x.Id, x.DisplayName, x.Role.ToString(), x.LoginCode))
                .ToListAsync();
    }
}
