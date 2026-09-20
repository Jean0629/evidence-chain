using EvidenceChain.Application.Evidence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EvidenceChain.Api.Controllers
{
    [Route("api/v1/evidence")]
    [ApiController]
    [Authorize]
    public class EvidenceController(IEvidenceQueries evidenceQueries, IChainVerification chainVerification) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] string? search, [FromQuery] Guid? custodianId, [FromQuery] string? cursor, [FromQuery] string? sort)
        {
            var order = string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase)
                ? SortOrder.Ascending
                : SortOrder.Descending;

            var result = await evidenceQueries.GetPageAsync(search, custodianId, cursor, order);
            return Ok(result);
        }

        [HttpGet("{id}/chain/verify")]
        public async Task<IActionResult> VerifyChain(Guid id)
        {
            var result = await chainVerification.VerifyAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await evidenceQueries.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("{id}/chain")]
        public async Task<IActionResult> GetChain(Guid id)
        {
            var result = await evidenceQueries.GetChainAsync(id);
            return Ok(result);
        }
    }
}
