using EvidenceChain.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Application.Evidence
{
    public enum SortOrder { Ascending, Descending }
    public interface IEvidenceQueries
    {
        Task<EvidencePageDto> GetPageAsync(string? search, Guid? custodianId, string? cursor, SortOrder order = SortOrder.Descending, int pageSize = 20);
    }
}
