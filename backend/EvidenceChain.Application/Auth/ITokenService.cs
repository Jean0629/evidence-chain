using EvidenceChain.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Application.Auth
{
    public interface ITokenService
    {
        string GenerateToken(Custodian custodian);
    }
}
