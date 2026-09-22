using EvidenceChain.Application.Auth;
using EvidenceChain.Application.Custodians;
using EvidenceChain.Application.DTOs;
using EvidenceChain.Infrastructure;
using EvidenceChain.Infrastructure.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EvidenceChain.Api.Controllers
{
    [Route("api/v1/custodians")]
    [ApiController]
    [Authorize]
    [ProducesResponseType(typeof(List<CustodianSummaryDto>), StatusCodes.Status200OK)]
    public class CustodiansController(ICustodianQueries custodianQueries) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await custodianQueries.GetAllAsync());
    }
}
